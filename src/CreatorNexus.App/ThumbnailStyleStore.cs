using System.Text.Json;

namespace CreatorNexus.App;

/// <summary>Stores reusable thumbnail styles separately from project data.</summary>
public sealed class ThumbnailStyleStore
{
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, "thumbnail-styles.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<ThumbnailStylePreset>> LoadAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!File.Exists(_filePath)) return Array.Empty<ThumbnailStylePreset>();
            await using var stream = File.OpenRead(_filePath);
            var styles = await JsonSerializer.DeserializeAsync<List<ThumbnailStylePreset>>(stream, Options)
                ?? throw new InvalidDataException("The saved thumbnail style library is empty or invalid.");
            return styles.OrderBy(style => style.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(ThumbnailStylePreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        await SaveManyAsync(new[] { preset });
    }

    /// <summary>Validates and saves a set of styles as one atomic library update.</summary>
    public async Task SaveManyAsync(IEnumerable<ThumbnailStylePreset> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);
        var incoming = presets.ToList();
        if (incoming.Count == 0)
            throw new ArgumentException("At least one thumbnail style is required.", nameof(presets));
        if (incoming.Any(preset => preset is null || !preset.IsValid()))
            throw new ArgumentException("One or more thumbnail styles contain a missing name or unsupported option.", nameof(presets));

        incoming = incoming.Select(preset => preset with { Name = preset.Name.Trim() }).ToList();
        if (incoming.GroupBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new ArgumentException("The incoming styles contain duplicate names.", nameof(presets));

        await _gate.WaitAsync();
        try
        {
            var all = new List<ThumbnailStylePreset>();
            if (File.Exists(_filePath))
            {
                await using var input = File.OpenRead(_filePath);
                all = await JsonSerializer.DeserializeAsync<List<ThumbnailStylePreset>>(input, Options)
                    ?? throw new InvalidDataException("The saved thumbnail style library is invalid. Existing data was preserved.");
            }

            foreach (var preset in incoming)
            {
                var existing = all.FindIndex(x => string.Equals(x.Name, preset.Name, StringComparison.OrdinalIgnoreCase));
                if (existing >= 0) all[existing] = preset;
                else all.Add(preset);
            }

            all = all.OrderBy(style => style.Name, StringComparer.OrdinalIgnoreCase).ToList();
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var temp = _filePath + ".tmp";
            try
            {
                await using (var output = File.Create(temp))
                {
                    await JsonSerializer.SerializeAsync(output, all, Options);
                    await output.FlushAsync();
                }
                File.Move(temp, _filePath, true);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        await _gate.WaitAsync();
        try
        {
            if (!File.Exists(_filePath)) return false;
            List<ThumbnailStylePreset> all;
            await using (var input = File.OpenRead(_filePath))
            {
                all = await JsonSerializer.DeserializeAsync<List<ThumbnailStylePreset>>(input, Options)
                    ?? throw new InvalidDataException("The saved thumbnail style library is invalid. Existing data was preserved.");
            }

            var removed = all.RemoveAll(x => string.Equals(x.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (removed == 0) return false;

            var temp = _filePath + ".tmp";
            try
            {
                await using (var output = File.Create(temp))
                {
                    await JsonSerializer.SerializeAsync(output, all, Options);
                    await output.FlushAsync();
                }
                File.Move(temp, _filePath, true);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
            return true;
        }
        finally { _gate.Release(); }
    }

}
