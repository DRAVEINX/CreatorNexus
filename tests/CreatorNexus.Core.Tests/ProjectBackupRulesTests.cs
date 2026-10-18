using CreatorNexus.Core;
using Xunit;

namespace CreatorNexus.Core.Tests;

public class ProjectBackupRulesTests
{
    [Fact]
    public void ValidateAcceptsValidProjects()
    {
        var projects = new[] { new CreatorProject { Title = "Video idea" }, new CreatorProject { Title = "Podcast" } };

        Assert.Null(ProjectBackupRules.Validate(projects));
    }

    [Fact]
    public void ValidateRejectsEmptyProjectEntry()
    {
        CreatorProject?[] projects = { new CreatorProject { Title = "Valid" }, null };

        Assert.Contains("empty project", ProjectBackupRules.Validate(projects));
    }

    [Fact]
    public void ValidateRejectsEmptyOrDuplicateIds()
    {
        var id = Guid.NewGuid();
        var duplicate = new[]
        {
            new CreatorProject { Id = id, Title = "One" },
            new CreatorProject { Id = id, Title = "Two" }
        };
        var empty = new[] { new CreatorProject { Id = Guid.Empty, Title = "One" } };

        Assert.Contains("IDs", ProjectBackupRules.Validate(duplicate));
        Assert.Contains("IDs", ProjectBackupRules.Validate(empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRejectsBlankTitles(string title)
    {
        var projects = new[] { new CreatorProject { Title = title } };

        Assert.Contains("title or status", ProjectBackupRules.Validate(projects));
    }

    [Fact]
    public void ValidateRejectsOverlongTitles()
    {
        var projects = new[] { new CreatorProject { Title = new string('x', 121) } };

        Assert.Contains("title or status", ProjectBackupRules.Validate(projects));
    }

    [Fact]
    public void ValidateRejectsUndefinedStatus()
    {
        var projects = new[] { new CreatorProject { Title = "Video", Status = (ProjectStatus)999 } };

        Assert.Contains("title or status", ProjectBackupRules.Validate(projects));
    }
}
