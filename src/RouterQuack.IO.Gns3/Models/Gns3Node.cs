using System.Text.Json.Serialization;

namespace RouterQuack.IO.Gns3.Models;

/// <summary>
/// Represents a GNS3 node (router, switch, etc.) from the API.
/// </summary>
public class Gns3Node
{
    [JsonPropertyName("node_id")]
    public required string NodeId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("node_type")]
    public required string NodeType { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("console")]
    public int? Console { get; init; }

    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; init; }
}