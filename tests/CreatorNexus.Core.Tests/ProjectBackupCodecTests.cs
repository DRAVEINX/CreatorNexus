using System.Text.Json;
using CreatorNexus.Core;
using Xunit;

namespace CreatorNexus.Core.Tests;

public class ProjectBackupCodecTests
{
    [Fact]
    public void SerializeWritesVersionManifestAndChecksum()
    {
        var json = ProjectBackupCodec.Serialize(new[] { new CreatorProject { Title = "Launch video" } });

        using var document = JsonDocument.Parse(json);
        Assert.Equal(ProjectBackupCodec.CurrentFormatVersion, document.RootElement.GetProperty("formatVersion").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("createdAtUtc").GetString()));
        Assert.Equal(64, document.RootElement.GetProperty("contentSha256").GetString()!.Length);
        Assert.Single(document.RootElement.GetProperty("projects").EnumerateArray());
    }

    [Fact]
    public void DeserializeRoundTripsVersionedBackup()
    {
        var original = new[]
        {
            new CreatorProject { Title = "Launch video", Notes = "Draft one" },
            new CreatorProject { Title = "Podcast", Status = ProjectStatus.Planning }
        };

        var restored = ProjectBackupCodec.Deserialize(ProjectBackupCodec.Serialize(original));

        Assert.Equal(original, restored);
    }

    [Fact]
    public void DeserializeRejectsChangedProjectPayload()
    {
        var json = ProjectBackupCodec.Serialize(new[] { new CreatorProject { Title = "Valid" } });
        var changed = json.Replace("Valid", "Changed", StringComparison.Ordinal);

        var error = Assert.Throws<InvalidDataException>(() => ProjectBackupCodec.Deserialize(changed));

        Assert.Contains("integrity check failed", error.Message);
    }

    [Fact]
    public void DeserializeRejectsUnsupportedFormatVersion()
    {
        var json = ProjectBackupCodec.Serialize(new[] { new CreatorProject { Title = "Valid" } })
            .Replace("\"formatVersion\": 1", "\"formatVersion\": 99", StringComparison.Ordinal);

        var error = Assert.Throws<InvalidDataException>(() => ProjectBackupCodec.Deserialize(json));

        Assert.Contains("not supported", error.Message);
    }

    [Fact]
    public void DeserializeRejectsMalformedChecksum()
    {
        var json = ProjectBackupCodec.Serialize(new[] { new CreatorProject { Title = "Valid" } });
        using var document = JsonDocument.Parse(json);
        var checksum = document.RootElement.GetProperty("contentSha256").GetString()!;
        var changed = json.Replace(checksum, "not-a-valid-checksum", StringComparison.Ordinal);

        var error = Assert.Throws<InvalidDataException>(() => ProjectBackupCodec.Deserialize(changed));

        Assert.Contains("checksum is invalid", error.Message);
    }

    [Fact]
    public void DeserializeRejectsMissingChecksum()
    {
        var json = ProjectBackupCodec.Serialize(new[] { new CreatorProject { Title = "Valid" } });
        using var document = JsonDocument.Parse(json);
        var checksum = document.RootElement.GetProperty("contentSha256").GetString()!;
        var changed = json.Replace(checksum, "", StringComparison.Ordinal);

        var error = Assert.Throws<InvalidDataException>(() => ProjectBackupCodec.Deserialize(changed));

        Assert.Contains("missing its integrity checksum", error.Message);
    }

    [Fact]
    public void DeserializeRejectsMissingProjectCollection()
    {
        const string json = """
            {
              "formatVersion": 1,
              "createdAtUtc": "2026-10-18T00:00:00+00:00",
              "contentSha256": "",
              "projects": null
            }
            """;

        var error = Assert.Throws<InvalidDataException>(() => ProjectBackupCodec.Deserialize(json));

        Assert.Contains("does not contain a project collection", error.Message);
    }

    [Fact]
    public void DeserializeAcceptsLegacyJsonArrayBackup()
    {
        var legacyJson = JsonSerializer.Serialize(new[] { new CreatorProject { Title = "Older backup" } });

        var restored = ProjectBackupCodec.Deserialize(legacyJson);

        Assert.Single(restored);
        Assert.Equal("Older backup", restored[0].Title);
    }
}
