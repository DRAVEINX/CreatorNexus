namespace CreatorNexus.Core;

public enum ProjectStatus { Idea, Planning, InProduction, ReadyToPublish, Published }

public sealed record CreatorProject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public string Checklist { get; init; } = string.Empty;
    public string Script { get; init; } = string.Empty;
    public string MediaLibrary { get; init; } = string.Empty;
    public string ThumbnailTitle { get; init; } = string.Empty;
    public string ThumbnailSubtitle { get; init; } = string.Empty;
    public string ThumbnailTheme { get; init; } = "Purple";
    public string? ThumbnailImagePath { get; init; }
    public string ThumbnailImageFit { get; init; } = "Cover";
    public string ThumbnailTextAlignment { get; init; } = "Center";
    public string ThumbnailTitleSize { get; init; } = "Medium";
    public string ThumbnailSubtitleSize { get; init; } = "Medium";
    public string ThumbnailFontFamily { get; init; } = "Arial";
    public string ThumbnailLayout { get; init; } = "Center";
    public string ThumbnailTextColor { get; init; } = "White";
    public string ThumbnailTextOutlineColor { get; init; } = "Black";
    public string ThumbnailTextOutlineThickness { get; init; } = "Medium";
    public string ThumbnailTextPanelStyle { get; init; } = "Dark";
    public string ThumbnailTextPanelOpacity { get; init; } = "Medium";
    public string ThumbnailTextPanelPadding { get; init; } = "Normal";
    public string ThumbnailTextPanelCorners { get; init; } = "Rounded";
    public string ThumbnailTextPanelBorder { get; init; } = "None";
    public string ThumbnailTextPanelBorderColor { get; init; } = "White";
    public string ThumbnailTextPanelBorderThickness { get; init; } = "Medium";
    public string ThumbnailTextPanelBorderOpacity { get; init; } = "Strong";
    public string ThumbnailTextPlacement { get; init; } = "Center";
    public ProjectStatus Status { get; init; } = ProjectStatus.Idea;
    public bool IsArchived { get; init; }
    public bool IsFavorite { get; init; }
    public DateTimeOffset? ScheduledFor { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    public CreatorProject WithTitle(string title) =>
        this with { Title = title.Trim(), UpdatedAt = DateTimeOffset.UtcNow };

    public CreatorProject WithStatus(ProjectStatus status) =>
        this with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };

    public CreatorProject WithArchived(bool isArchived) =>
        this with { IsArchived = isArchived, UpdatedAt = DateTimeOffset.UtcNow };
}