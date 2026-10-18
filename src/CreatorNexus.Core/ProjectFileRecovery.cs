using System.Text.Json;

namespace CreatorNexus.Core;

/// <summary>Safely restores a validated local project snapshot without overwriting the recovery source.</summary>
public static class ProjectFileRecovery
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static async Task RecoverAsync(string primaryPath, string backupPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);

        var primary = Path.GetFullPath(primaryPath);
        var backup = Path.GetFullPath(backupPath);
        if (string.Equals(primary, backup, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The recovery backup must be a different file from the saved project file.", nameof(backupPath));

        if (!File.Exists(backup))
            throw new FileNotFoundException("No local CreatorNexus recovery backup was found.", backup);

        List<CreatorProject> projects;
        try
        {
            string json;
            await using (var stream = File.OpenRead(backup))
            {
                var sizeError = ProjectBackupRules.ValidateBackupSize(stream.Length);
                if (sizeError is not null)
                    throw new InvalidDataException($"{sizeError} The saved project file was not changed.");

                using var reader = new StreamReader(stream);
                json = await reader.ReadToEndAsync(cancellationToken);
            }

            projects = ProjectBackupRules.ReadAndValidateBackup(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The local recovery backup is not valid CreatorNexus JSON. The saved project file was not changed.", ex);
        }

        var directory = Path.GetDirectoryName(primary)
            ?? throw new InvalidOperationException("The saved project file has no parent directory.");
        Directory.CreateDirectory(directory);
        var temp = primary + ".recovery.tmp";
        try
        {
            await using (var stream = File.Create(temp))
            {
                await JsonSerializer.SerializeAsync(stream, projects, Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            var outputSizeError = ProjectBackupRules.ValidateBackupSize(new FileInfo(temp).Length);
            if (outputSizeError is not null)
                throw new InvalidDataException($"{outputSizeError} The saved project file was not changed.");

            File.Move(temp, primary, true);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }
}
