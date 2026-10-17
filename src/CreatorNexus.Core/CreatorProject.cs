namespace CreatorNexus.Core;

public enum ProjectStatus { Idea, Planning, InProduction, ReadyToPublish, Published }

public sealed record CreatorProject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; } = ProjectStatus.Idea;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public CreatorProject WithTitle(string title) => this with { Title = title.Trim(), UpdatedAt = DateTimeOffset.UtcNow };
    public CreatorProject WithStatus(ProjectStatus status) => this with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
}
