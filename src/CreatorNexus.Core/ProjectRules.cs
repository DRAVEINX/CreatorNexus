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
            source = source.Where(p => Contains(p.Title, term)
                || Contains(p.Channel, term)
                || Contains(p.Notes, term)
                || Contains(p.Tags, term)
                || Contains(p.Checklist, term)
                || Contains(p.Script, term)
                || Contains(p.MediaLibrary, term)
                || Contains(p.ThumbnailTitle, term)
                || Contains(p.ThumbnailSubtitle, term));
        return source.OrderByDescending(p => p.UpdatedAt).ToArray();
    }

    private static bool Contains(string? value, string term) =>
        value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;
}
