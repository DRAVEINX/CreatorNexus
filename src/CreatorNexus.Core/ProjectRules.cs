namespace CreatorNexus.Core;

public static class ProjectRules
{
    public static string ValidateTitle(string? title)
    {
        var value = title?.Trim() ?? string.Empty;
        if (value.Length == 0) return "A project title is required.";
        if (value.Length > 120) return "Project titles must be 120 characters or fewer.";
        return string.Empty;
    }

    public static IReadOnlyList<CreatorProject> Search(IEnumerable<CreatorProject> projects, string? query)
    {
        var term = query?.Trim();
        var source = projects;
        if (!string.IsNullOrEmpty(term))
            source = source.Where(p => p.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || p.Channel.Contains(term, StringComparison.OrdinalIgnoreCase)
                || p.Notes.Contains(term, StringComparison.OrdinalIgnoreCase));
        return source.OrderByDescending(p => p.UpdatedAt).ToArray();
    }
}
