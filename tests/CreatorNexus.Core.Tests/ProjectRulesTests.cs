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
    [Fact] public void EmptySearchReturnsAll() => Assert.Equal(2, ProjectRules.Search(new[] { new CreatorProject { Title = "One" }, new CreatorProject { Title = "Two" } }, "").Count);
}
