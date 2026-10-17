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
        try { var all = (await LoadCoreAsync()).ToList(); all.Add(project); await SaveCoreAsync(all); }
        finally { _gate.Release(); }
    }
    public async Task UpdateAsync(CreatorProject project)
    {
        await _gate.WaitAsync();
        try { var all = (await LoadCoreAsync()).ToList(); var i = all.FindIndex(x => x.Id == project.Id); if (i < 0) return; all[i] = project; await SaveCoreAsync(all); }
        finally { _gate.Release(); }
    }
    private async Task<IReadOnlyList<CreatorProject>> LoadCoreAsync()
    {
        if (!File.Exists(_filePath)) return Array.Empty<CreatorProject>();
        try { await using var stream = File.OpenRead(_filePath); return await JsonSerializer.DeserializeAsync<List<CreatorProject>>(stream, Options) ?? new List<CreatorProject>(); }
        catch (JsonException) { return Array.Empty<CreatorProject>(); }
    }
    private async Task SaveCoreAsync(IReadOnlyList<CreatorProject> projects)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!); var temp = _filePath + ".tmp";
        await using (var stream = File.Create(temp)) await JsonSerializer.SerializeAsync(stream, projects, Options);
        File.Move(temp, _filePath, true);
    }
}
