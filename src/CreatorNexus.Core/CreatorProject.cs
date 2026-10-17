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