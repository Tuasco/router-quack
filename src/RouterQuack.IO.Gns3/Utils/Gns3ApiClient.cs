using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RouterQuack.IO.Gns3.Models;

namespace RouterQuack.IO.Gns3.Utils;

/// <summary>
/// Simplified HTTP client for communicating with GNS3 REST API.
/// </summary>
public sealed class Gns3ApiClient(ILogger<Gns3ApiClient> logger) : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Initialise the client with server URI.
    /// </summary>
    public void Initialize(Uri server)
    {
        if (_httpClient.BaseAddress != null && _httpClient.BaseAddress.Equals(server))
            return;

        _httpClient.BaseAddress = server;
    }

    /// <summary>
    /// Get a project by name.
    /// </summary>
    public async Task<Gns3Project?> GetProjectByNameAsync(string projectName)
    {
        var projects = await GetProjectsAsync();
        if (projects is null) return null;
        return projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all nodes in a project.
    /// </summary>
    public async Task<List<Gns3Node>?> GetProjectNodesAsync(string projectId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v2/projects/{projectId}/nodes");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Gns3Node>>() ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogDebug(ex, "Failed to retrieve nodes for project {ProjectId}.", projectId);
            return null;
        }
    }

    /// <summary>
    /// Start, stop or reload a node
    /// </summary>
    public async Task<bool> ControlNodeAsync(string projectId, string nodeId, NodeOperation operation)
    {
        var action = operation.ToString().ToLowerInvariant();

        try
        {
            var response = await _httpClient.PostAsync(
                $"/v2/projects/{projectId}/nodes/{nodeId}/{action}", null);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            logger.LogDebug(ex, "Failed to {Action} node {NodeId}.", action, nodeId);
            return false;
        }
    }

    /// <summary>
    /// Upload a configuration file to a node.
    /// For Cisco Dynamips routers, we try all detected adapter slots.
    /// </summary>
    public async Task<bool> UploadConfigFileAsync(string projectId, string nodeId, string configContent, Gns3Node node)
    {
        var detailedNode = await GetNodeAsync(projectId, nodeId);
        if (detailedNode is null) return false;

        string filePath;
        if (detailedNode is { NodeType: "dynamips", Properties: not null }
            && detailedNode.Properties.TryGetValue("dynamips_id", out var dynamipsIdObj))
        {
            // dynamips_id is the global instance counter → matches the i{n} prefix
            var dynamipsId = ((JsonElement)dynamipsIdObj).GetInt32();
            filePath = $"configs/i{dynamipsId}_startup-config.cfg";
        }
        else
            // Fallback for non-Dynamips nodes
            filePath = "configs/startup-config.cfg";

        logger.LogDebug("Uploading config to {FilePath} for node {NodeName}", filePath, node.Name);
        try
        {
            var response = await _httpClient.PostAsync(
                $"/v2/projects/{projectId}/nodes/{nodeId}/files/{filePath}",
                new StringContent(configContent, Encoding.UTF8, "application/octet-stream"));
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to upload config to node {NodeId} at {FilePath}: {Error}", nodeId, filePath,
                    error);
                return false;
            }
            return true;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while uploading config to node {NodeId}.", nodeId);
            return false;
        }
    }

    /// <summary>
    /// Get all projects from the GNS3 server.
    /// </summary>
    private async Task<List<Gns3Project>?> GetProjectsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/v2/projects");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Gns3Project>>() ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Failed to connect to GNS3 server at {Server}. Ensure the server is running. ({Message})",
                _httpClient.BaseAddress, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Get detailed information about a specific node.
    /// </summary>
    private async Task<Gns3Node?> GetNodeAsync(string projectId, string nodeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v2/projects/{projectId}/nodes/{nodeId}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<Gns3Node>();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to retrieve node {NodeId} details.", nodeId);
            return null;
        }
    }

    public void Dispose() => _httpClient.Dispose();
}

public enum NodeOperation
{
    Start,
    Stop,
    Reload
}