using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RouterQuack.IO.Gns3.Exceptions;
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
        return projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all nodes in a project.
    /// </summary>
    public async Task<List<Gns3Node>> GetProjectNodesAsync(string projectId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v2/projects/{projectId}/nodes");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Gns3Node>>() ?? [];
        }
        catch (HttpRequestException ex)
        {
            throw new Gns3ConnectionException($"Failed to retrieve nodes for project {projectId}.", ex);
        }
    }

    /// <summary>
    /// Start, stop or reload a node
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="Gns3Exception"></exception>
    public async Task ControlNodeAsync(string projectId, string nodeId, NodeOperation operation)
    {
        var action = operation.ToString().ToLowerInvariant();

        try
        {
            var response = await _httpClient.PostAsync(
                $"/v2/projects/{projectId}/nodes/{nodeId}/{action}",
                null);

            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new Gns3Exception($"Failed to {action} node {nodeId}.", ex);
        }
    }

    /// <summary>
    /// Upload a configuration file to a node.
    /// For Cisco Dynamips routers, we try all detected adapter slots.
    /// </summary>
    public async Task UploadConfigFileAsync(string projectId, string nodeId, string configContent, Gns3Node node)
    {
        var detailedNode = await GetNodeAsync(projectId, nodeId);

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

        var response = await _httpClient.PostAsync(
            $"/v2/projects/{projectId}/nodes/{nodeId}/files/{filePath}",
            new StringContent(configContent, Encoding.UTF8, "application/octet-stream"));

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Gns3Exception($"Failed to upload config to node {nodeId} at {filePath}: {error}");
        }
    }

    /// <summary>
    /// Get all projects from the GNS3 server.
    /// </summary>
    private async Task<List<Gns3Project>> GetProjectsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/v2/projects");
            response.EnsureSuccessStatusCode();

            var projects = await response.Content.ReadFromJsonAsync<List<Gns3Project>>();
            return projects ?? [];
        }
        catch (HttpRequestException ex)
        {
            throw new Gns3ConnectionException(
                $"Failed to connect to GNS3 server at {_httpClient.BaseAddress}. " +
                "Ensure the server is running and the URL is correct.", ex);
        }
    }

    /// <summary>
    /// Get detailed information about a specific node.
    /// </summary>
    private async Task<Gns3Node> GetNodeAsync(string projectId, string nodeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v2/projects/{projectId}/nodes/{nodeId}");
            response.EnsureSuccessStatusCode();

            var node = await response.Content.ReadFromJsonAsync<Gns3Node>();
            if (node == null)
            {
                throw new Gns3Exception($"Failed to deserialize node {nodeId}.");
            }

            return node;
        }
        catch (HttpRequestException ex)
        {
            throw new Gns3ConnectionException(
                $"Failed to retrieve node {nodeId} details.", ex);
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