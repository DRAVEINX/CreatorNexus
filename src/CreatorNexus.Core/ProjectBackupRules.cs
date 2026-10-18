namespace CreatorNexus.Core;

/// <summary>Pure validation rules for imported project backup data.</summary>
public static class ProjectBackupRules
{
    public const int MaximumProjects = 10_000;

    /// <summary>
    /// Returns an error message when a backup project collection is invalid; otherwise returns null.
    /// </summary>
    public static string? Validate(IReadOnlyList<CreatorProject?> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        if (projects.Count > MaximumProjects)
            return $"The backup contains more than {MaximumProjects} projects.";

        if (projects.Any(project => project is null))
            return "The backup contains an empty project entry.";

        var validProjects = projects.Cast<CreatorProject>().ToList();

        if (validProjects.Any(project => project.Id == Guid.Empty)
            || validProjects.Select(project => project.Id).Distinct().Count() != validProjects.Count)
            return "The backup contains invalid or duplicate project IDs.";

        if (validProjects.Any(project => string.IsNullOrWhiteSpace(project.Title)
            || project.Title.Trim().Length > 120
            || !Enum.IsDefined(project.Status)))
            return "The backup contains a project with an invalid title or status.";

        if (validProjects.Any(HasNullTextField))
            return "The backup contains a project with a missing text field.";

        return null;
    }

    private static bool HasNullTextField(CreatorProject project) =>
        project.Channel is null
        || project.Notes is null
        || project.Tags is null
        || project.Checklist is null
        || project.Script is null
        || project.MediaLibrary is null
        || project.ThumbnailTitle is null
        || project.ThumbnailSubtitle is null
        || project.ThumbnailTheme is null
        || project.ThumbnailTextAlignment is null
        || project.ThumbnailTitleSize is null
        || project.ThumbnailSubtitleSize is null
        || project.ThumbnailFontFamily is null
        || project.ThumbnailLayout is null
        || project.ThumbnailTextColor is null
        || project.ThumbnailTextOutlineColor is null
        || project.ThumbnailTextOutlineThickness is null
        || project.ThumbnailTextPanelStyle is null
        || project.ThumbnailTextPanelOpacity is null
        || project.ThumbnailTextPanelPadding is null
        || project.ThumbnailTextPanelCorners is null
        || project.ThumbnailTextPanelBorder is null
        || project.ThumbnailTextPanelBorderColor is null
        || project.ThumbnailTextPanelBorderThickness is null
        || project.ThumbnailTextPanelBorderOpacity is null
        || project.ThumbnailTextPlacement is null;
}
