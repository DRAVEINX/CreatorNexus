using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CreatorNexus.Core;

/// <summary>
/// Encodes versioned backups with a SHA-256 integrity checksum and reads legacy JSON-array backups.
/// The checksum detects accidental or untrusted changes; it is not a digital signature.
/// </summary>
public static class ProjectBackupCodec
{
    public const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(IReadOnlyList<CreatorProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var payload = JsonSerializer.Serialize(projects, Options);
        var envelope = new BackupEnvelope
        {
            FormatVersion = CurrentFormatVersion,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ContentSha256 = ComputeSha256(payload),
            Projects = projects.ToList()
        };

        return JsonSerializer.Serialize(envelope, Options);
    }

    /// <summary>Reads a versioned backup or a legacy bare JSON array.</summary>
    public static List<CreatorProject> Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<CreatorProject>>(json, Options)
                ?? throw new InvalidDataException("The backup does not contain a project collection.");
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("The backup does not contain a supported project collection.");

        var envelope = JsonSerializer.Deserialize<BackupEnvelope>(json, Options)
            ?? throw new InvalidDataException("The backup manifest is missing.");

        if (envelope.FormatVersion != CurrentFormatVersion)
            throw new InvalidDataException(
                $"Backup format version {envelope.FormatVersion} is not supported. Supported version: {CurrentFormatVersion}.");

        if (envelope.Projects is null)
            throw new InvalidDataException("The backup manifest does not contain a project collection.");

        var payload = JsonSerializer.Serialize(envelope.Projects, Options);
        var actualHash = Convert.FromHexString(ComputeSha256(payload));
        byte[] expectedHash;
        try
        {
            if (string.IsNullOrWhiteSpace(envelope.ContentSha256))
                throw new InvalidDataException("The backup manifest is missing its integrity checksum.");

            expectedHash = Convert.FromHexString(envelope.ContentSha256);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("The backup integrity checksum is invalid.", ex);
        }

        if (expectedHash.Length != actualHash.Length
            || !CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
            throw new InvalidDataException("The backup integrity check failed. The file may be damaged or modified.");

        return envelope.Projects;
    }

    private static string ComputeSha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class BackupEnvelope
    {
        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; init; }

        [JsonPropertyName("createdAtUtc")]
        public DateTimeOffset CreatedAtUtc { get; init; }

        [JsonPropertyName("contentSha256")]
        public string ContentSha256 { get; init; } = string.Empty;

        [JsonPropertyName("projects")]
        public List<CreatorProject>? Projects { get; init; }
    }
}
