using CreatorNexus.Core;
using Xunit;

namespace CreatorNexus.Core.Tests;

public class ProjectBackupRulesTests
{
    [Fact]
    public void ValidateBackupSizeAcceptsLimit()
    {
        Assert.Null(ProjectBackupRules.ValidateBackupSize(ProjectBackupRules.MaximumBackupBytes));
    }

    [Fact]
    public void ValidateBackupSizeRejectsOversizedBackup()
    {
        Assert.Contains("size limit", ProjectBackupRules.ValidateBackupSize(ProjectBackupRules.MaximumBackupBytes + 1));
    }

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

    [Fact]
    public void ValidateRejectsOversizedCollection()
    {
        var projects = Enumerable.Range(0, ProjectBackupRules.MaximumProjects + 1)
            .Select(index => new CreatorProject { Title = $"Project {index}" })
            .ToArray();

        Assert.Contains("more than", ProjectBackupRules.Validate(projects));
    }

    [Fact]
    public void ValidateRejectsNullTextField()
    {
        var projects = new[] { new CreatorProject { Title = "Valid", Notes = null! } };

        Assert.Contains("missing text field", ProjectBackupRules.Validate(projects));
    }

    [Fact]
    public void ValidateBackupSizeRejectsNegativeLength()
    {
        Assert.Contains("negative", ProjectBackupRules.ValidateBackupSize(-1));
    }
    [Fact]
    public void ValidateRejectsNullProjectCollection()
    {
        Assert.Throws<ArgumentNullException>(() => ProjectBackupRules.Validate(null!));
    }

    [Fact]
    public void ReadAndValidateBackupReturnsOnlyValidatedProjects()
    {
        var expected = new[] { new CreatorProject { Title = "Verified project" } };
        var backup = ProjectBackupCodec.Serialize(expected);

        var restored = ProjectBackupRules.ReadAndValidateBackup(backup);

        Assert.Equal(expected, restored);
    }

    [Fact]
    public void ReadAndValidateBackupRejectsInvalidProjectCollection()
    {
        var backup = ProjectBackupCodec.Serialize(new[] { new CreatorProject { Title = "   " } });

        var error = Assert.Throws<InvalidDataException>(
            () => ProjectBackupRules.ReadAndValidateBackup(backup));

        Assert.Contains("invalid title or status", error.Message);
        Assert.Contains("Existing projects were not changed", error.Message);
    }

    [Fact]
    public void ReadAndValidateBackupRejectsDuplicateIdsBeforeReturningData()
    {
        var id = Guid.NewGuid();
        var backup = ProjectBackupCodec.Serialize(new[]
        {
            new CreatorProject { Id = id, Title = "First" },
            new CreatorProject { Id = id, Title = "Second" }
        });

        var error = Assert.Throws<InvalidDataException>(
            () => ProjectBackupRules.ReadAndValidateBackup(backup));

        Assert.Contains("duplicate project IDs", error.Message);
        Assert.Contains("Existing projects were not changed", error.Message);
    }

    [Fact]
    public async Task RecoverAsyncRestoresValidatedBackupAndPreservesBackup()
    {
        var directory = Path.Combine(Path.GetTempPath(), "CreatorNexusRecoveryTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var primary = Path.Combine(directory, "creator-projects.json");
        var backup = primary + ".bak";
        try
        {
            await File.WriteAllTextAsync(primary, "corrupt primary");
            var originalBackup = ProjectBackupCodec.Serialize(new[] { new CreatorProject { Title = "Recovered project" } });
            await File.WriteAllTextAsync(backup, originalBackup);

            await ProjectFileRecovery.RecoverAsync(primary, backup);

            var restored = ProjectBackupRules.ReadAndValidateBackup(await File.ReadAllTextAsync(primary));
            Assert.Single(restored);
            Assert.Equal("Recovered project", restored[0].Title);
            Assert.Equal(originalBackup, await File.ReadAllTextAsync(backup));
            Assert.False(File.Exists(primary + ".recovery.tmp"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task RecoverAsyncRejectsInvalidBackupWithoutChangingPrimary()
    {
        var directory = Path.Combine(Path.GetTempPath(), "CreatorNexusRecoveryTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var primary = Path.Combine(directory, "creator-projects.json");
        var backup = primary + ".bak";
        try
        {
            const string originalPrimary = "keep this primary unchanged";
            await File.WriteAllTextAsync(primary, originalPrimary);
            await File.WriteAllTextAsync(backup, "{ invalid json");

            await Assert.ThrowsAnyAsync<Exception>(() => ProjectFileRecovery.RecoverAsync(primary, backup));

            Assert.Equal(originalPrimary, await File.ReadAllTextAsync(primary));
            Assert.Equal("{ invalid json", await File.ReadAllTextAsync(backup));
            Assert.False(File.Exists(primary + ".recovery.tmp"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task RecoverAsyncRejectsMissingBackupWithoutChangingPrimary()
    {
        var directory = Path.Combine(Path.GetTempPath(), "CreatorNexusRecoveryTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var primary = Path.Combine(directory, "creator-projects.json");
        try
        {
            const string originalPrimary = "keep this primary unchanged";
            await File.WriteAllTextAsync(primary, originalPrimary);

            await Assert.ThrowsAsync<FileNotFoundException>(() => ProjectFileRecovery.RecoverAsync(primary, primary + ".bak"));

            Assert.Equal(originalPrimary, await File.ReadAllTextAsync(primary));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
