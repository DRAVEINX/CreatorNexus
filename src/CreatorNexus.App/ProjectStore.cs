using System.Text.Json;
using CreatorNexus.Core;

namespace CreatorNexus.App;

public sealed class ProjectStore
{
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, "creator-projects.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    // All store instances share one lock so concurrent callers cannot overwrite each other's updates.
    private static readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<CreatorProject>> LoadAsync()
    {
        await _gate.WaitAsync();
        try { return await LoadCoreAsync(); }
        finally { _gate.Release(); }
    }

    public async Task AddAsync(CreatorProject project)
    {
        await _gate.WaitAsync();
        try
        {
            var all = (await LoadCoreAsync()).ToList();
            all.Add(project);
            await SaveCoreAsync(all);
        }
        finally { _gate.Release(); }
    }

    public async Task UpdateAsync(CreatorProject project)
    {
        await _gate.WaitAsync();
        try
        {
            var all = (await LoadCoreAsync()).ToList();
            var i = all.FindIndex(x => x.Id == project.Id);
            if (i < 0) return;
            all[i] = project;
            await SaveCoreAsync(all);
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            var all = (await LoadCoreAsync()).Where(x => x.Id != id).ToList();
            await SaveCoreAsync(all);
        }
        finally { _gate.Release(); }
    }

    /// <summary>Exports the current project collection as a versioned portable JSON backup.</summary>
    public async Task ExportBackupAsync(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        await _gate.WaitAsync();
        try
        {
            var projects = await LoadCoreAsync();
            var fullPath = Path.GetFullPath(destinationPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = fullPath + ".tmp";
            try
            {
                await File.WriteAllTextAsync(tempPath, ProjectBackupCodec.Serialize(projects), new System.Text.UTF8Encoding(false));

                // Never create an export that the restore flow would refuse to import.
                var sizeError = ProjectBackupRules.ValidateBackupSize(new FileInfo(tempPath).Length);
                if (sizeError is not null)
                    throw new InvalidDataException(sizeError);

                File.Move(tempPath, fullPath, true);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }
        finally { _gate.Release(); }
    }

    /// <summary>Validates a backup before replacing the current project collection.</summary>
    public async Task RestoreBackupAsync(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var fullPath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The selected CreatorNexus backup was not found.", fullPath);

        await _gate.WaitAsync();
        try
        {
            List<CreatorProject> projects;
            try
            {
                string json;
                await using (var stream = File.OpenRead(fullPath))
                {
                    var sizeError = ProjectBackupRules.ValidateBackupSize(stream.Length);
                    if (sizeError is not null)
                        throw new InvalidDataException($"{sizeError} Existing projects were not changed.");

                    using var reader = new StreamReader(stream);
                    json = await reader.ReadToEndAsync();
                }

                projects = ProjectBackupRules.ReadAndValidateBackup(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("The selected backup is not valid CreatorNexus JSON. Existing projects were not changed.", ex);
            }

            // The complete parse-and-validation gate must pass before saved data is replaced.
            await SaveCoreAsync(projects);
        }
        finally { _gate.Release(); }
    }

    /// <summary>Recovers projects from the validated local .bak snapshot without replacing that recovery source.</summary>
    public async Task RecoverFromLocalBackupAsync()
    {
        await _gate.WaitAsync();
        try
        {
            // Core helper is independently testable and preserves the recovery source.
            await ProjectFileRecovery.RecoverAsync(_filePath, _filePath + ".bak");
        }
        finally { _gate.Release(); }
    }

    private async Task<IReadOnlyList<CreatorProject>> LoadCoreAsync()
    {
        if (!File.Exists(_filePath)) return Array.Empty<CreatorProject>();
        try
        {
            await using var stream = File.OpenRead(_filePath);
            var sizeError = ProjectBackupRules.ValidateBackupSize(stream.Length);
            if (sizeError is not null)
                throw new InvalidDataException(
                    $"The saved CreatorNexus project file exceeds the supported size limit: {sizeError} The original file was preserved at '{_filePath}'.");

            var projects = await JsonSerializer.DeserializeAsync<List<CreatorProject>>(stream, Options);
            if (projects is null)
                throw new InvalidDataException(
                    $"The saved CreatorNexus project file contains no project collection. The original file was preserved at '{_filePath}'.");

            var validationError = ProjectBackupRules.Validate(projects);
            if (validationError is not null)
                throw new InvalidDataException(
                    $"The saved CreatorNexus project file failed validation: {validationError} The original file was preserved at '{_filePath}'.");

            return projects;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"The saved CreatorNexus project file is invalid. The original file was preserved at '{_filePath}'.", ex);
        }
    }

    private async Task SaveCoreAsync(IReadOnlyList<CreatorProject> projects)
    {
        // Validate every write, not only imports, so malformed updates cannot replace
        // a previously valid project file or become the next recovery snapshot.
        var validationError = ProjectBackupRules.Validate(projects);
        if (validationError is not null)
            throw new InvalidDataException($"Project data was not saved: {validationError} Existing data was preserved.");

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temp = _filePath + ".tmp";
        var backup = _filePath + ".bak";
        try
        {
            await using (var stream = File.Create(temp))
            {
                await JsonSerializer.SerializeAsync(stream, projects, Options);
                await stream.FlushAsync();
            }

            // Preserve the last known file before replacing it with the new snapshot.
            if (File.Exists(_filePath))
                File.Copy(_filePath, backup, true);

            File.Move(temp, _filePath, true);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }
}
