namespace CreatorNexus.Core;

/// <summary>Pure validation rules for imported project backup data.</summary>
public static class ProjectBackupRules
{
    /// <summary>
    /// Returns an error message when a backup project collection is invalid; otherwise returns null.
    /// </summary>
    public static string? Validate(IReadOnlyList<CreatorProject?> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

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

        return null;
    }
}
