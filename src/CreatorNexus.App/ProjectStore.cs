using System.Text.Json;
using CreatorNexus.Core;

namespace CreatorNexus.App;

public sealed class ProjectStore
{
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, "creator-projects.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);

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

    /// <summary>Exports the current project collection as a portable JSON backup.</summary>
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
                await using (var stream = File.Create(tempPath))
                {
                    await JsonSerializer.SerializeAsync(stream, projects, Options);
                    await stream.FlushAsync();
                }
                File.Move(tempPath, fullPath, true);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }
        finally { _gate.Release(); }
    }

    /// <summary>Validates a JSON backup before replacing the current project collection.</summary>
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
                await using var stream = File.OpenRead(fullPath);
                projects = await JsonSerializer.DeserializeAsync<List<CreatorProject>>(stream, Options)
                    ?? throw new InvalidDataException("The backup does not contain a project collection.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("The selected backup is not valid CreatorNexus JSON. Existing projects were not changed.", ex);
            }

            if (projects.Any(p => p.Id == Guid.Empty) || projects.Select(p => p.Id).Distinct().Count() != projects.Count)
                throw new InvalidDataException("The backup contains invalid or duplicate project IDs. Existing projects were not changed.");

            await SaveCoreAsync(projects);
        }
        finally { _gate.Release(); }
    }

    private async Task<IReadOnlyList<CreatorProject>> LoadCoreAsync()
    {
        if (!File.Exists(_filePath)) return Array.Empty<CreatorProject>();
        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<CreatorProject>>(stream, Options)
                ?? new List<CreatorProject>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"The saved CreatorNexus project file is invalid. The original file was preserved at '{_filePath}'.", ex);
        }
    }

    private async Task SaveCoreAsync(IReadOnlyList<CreatorProject> projects)
    {
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