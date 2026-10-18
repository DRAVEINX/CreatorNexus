using CreatorNexus.Core;
using Xunit;

namespace CreatorNexus.Core.Tests;
public class ProjectRulesTests
{
    [Theory][InlineData(null)][InlineData("")][InlineData("   ")]
    public void ValidateTitleRejectsBlank(string? title) => Assert.NotEmpty(ProjectRules.ValidateTitle(title));
    [Fact] public void ValidateTitleAcceptsNormalTitle() => Assert.Empty(ProjectRules.ValidateTitle("My first video"));
    [Fact] public void ValidateTitleRejectsOverlongTitle() => Assert.NotEmpty(ProjectRules.ValidateTitle(new string('x', 121)));
    [Fact] public void SearchIsCaseInsensitive()
    {
        var projects = new[] { new CreatorProject { Title = "Gaming Highlights" }, new CreatorProject { Title = "Travel vlog" } };
        var result = ProjectRules.Search(projects, "GAMING"); Assert.Single(result); Assert.Equal("Gaming Highlights", result[0].Title);
    }
    [Theory]
    [InlineData("script", "Intro hook")]
    [InlineData("checklist", "Check audio")]
    [InlineData("media", "clip.mp4")]
    [InlineData("thumbnail title", "Big reveal")]
    [InlineData("thumbnail subtitle", "Episode 12")]
    public void SearchFindsProjectsAcrossProductionContent(string field, string query)
    {
        var project = field switch
        {
            "script" => new CreatorProject { Title = "Project", Script = "Intro hook and outro" },
            "checklist" => new CreatorProject { Title = "Project", Checklist = "Check audio before export" },
            "media" => new CreatorProject { Title = "Project", MediaLibrary = "clip.mp4" },
            "thumbnail title" => new CreatorProject { Title = "Project", ThumbnailTitle = "Big reveal" },
            "thumbnail subtitle" => new CreatorProject { Title = "Project", ThumbnailSubtitle = "Episode 12" },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        var result = ProjectRules.Search(new[] { project }, query);
        Assert.Single(result);
        Assert.Equal(project.Id, result[0].Id);
    }
    [Fact] public void EmptySearchReturnsAll() => Assert.Equal(2, ProjectRules.Search(new[] { new CreatorProject { Title = "One" }, new CreatorProject { Title = "Two" } }, "").Count);
}
