using System.Text.Json;
using RouterQuack.IO.Gns3.Models;

namespace RouterQuack.IO.Gns3;

/// <summary>
/// Debug utility to inspect GNS3 node properties.
/// </summary>
public class DebugNodeInfo
{
    private readonly Gns3ApiClient _apiClient;
    private readonly ILogger<DebugNodeInfo> _logger;

    public DebugNodeInfo(Gns3ApiClient apiClient, ILogger<DebugNodeInfo> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task InspectNodeAsync(string projectId, string nodeId)
    {
        var node = await _apiClient.GetNodeAsync(projectId, nodeId);
        
        _logger.LogInformation("Node: {Name} ({NodeId})", node.Name, node.NodeId);
        _logger.LogInformation("Type: {Type}", node.NodeType);
        _logger.LogInformation("Status: {Status}", node.Status);
        
        if (node.Properties != null)
        {
            _logger.LogInformation("Properties:");
            foreach (var prop in node.Properties)
            {
                var valueJson = JsonSerializer.Serialize(prop.Value);
                _logger.LogInformation("  {Key}: {Value}", prop.Key, valueJson);
            }
        }
        else
        {
            _logger.LogWarning("No properties found");
        }
    }
}