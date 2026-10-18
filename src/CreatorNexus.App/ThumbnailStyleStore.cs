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
            return await JsonSerializer.DeserializeAsync<List<ThumbnailStylePreset>>(stream, Options)
                ?? throw new InvalidDataException("The saved thumbnail style library is empty or invalid.");
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(ThumbnailStylePreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        var name = preset.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A style name is required.", nameof(preset));

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

            var existing = all.FindIndex(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            var named = preset with { Name = name };
            if (existing >= 0) all[existing] = named;
            else all.Add(named);

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
