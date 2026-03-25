using System.Text.Json.Serialization;

namespace RouterQuack.IO.Gns3.Models;

/// <summary>
/// Represents a GNS3 project from the API.
/// </summary>
public class Gns3Project
{
    [JsonPropertyName("project_id")]
    public required string ProjectId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}