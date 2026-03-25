using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RouterQuack.IO.Gns3.Exceptions;
using RouterQuack.IO.Gns3.Models;

namespace RouterQuack.IO.Gns3;

/// <summary>
/// Simplified HTTP client for communicating with GNS3 REST API.
/// </summary>
public class Gns3ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Gns3ApiClient> _logger;

    public Gns3ApiClient(ILogger<Gns3ApiClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Initialize the client with server URI.
    /// </summary>
    public void Initialize(Uri server)
    {
        // Only set BaseAddress if it hasn't been set or if it's different
        if (_httpClient.BaseAddress == null || !_httpClient.BaseAddress.Equals(server))
        {
            _httpClient.BaseAddress = server;
            _logger.LogDebug("GNS3 API client initialized for {Server}", server);
        }
        else
        {
            _logger.LogDebug("GNS3 API client already initialized for {Server}", server);
        }
    }

    /// <summary>
    /// Get all projects from the GNS3 server.
    /// </summary>
    public async Task<List<Gns3Project>> GetProjectsAsync()
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
    /// Get a project by name.
    /// </summary>
    public async Task<Gns3Project> GetProjectByNameAsync(string projectName)
    {
        var projects = await GetProjectsAsync();
        var project = projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        if (project == null)
        {
            var availableProjects = string.Join(", ", projects.Select(p => $"'{p.Name}'"));
            throw new Gns3ProjectNotFoundException(
                $"Project '{projectName}' not found on GNS3 server. " +
                $"Available projects: {(availableProjects.Length > 0 ? availableProjects : "none")}");
        }

        return project;
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

            var nodes = await response.Content.ReadFromJsonAsync<List<Gns3Node>>();
            return nodes ?? [];
        }
        catch (HttpRequestException ex)
        {
            throw new Gns3ConnectionException(
                $"Failed to retrieve nodes for project {projectId}.", ex);
        }
    }

    /// <summary>
    /// Get detailed information about a specific node.
    /// </summary>
    public async Task<Gns3Node> GetNodeAsync(string projectId, string nodeId)
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
    /// <summary>
    /// Upload a configuration file to a node.
    /// For Cisco Dynamips routers, we try all detected adapter slots.
    /// </summary>
    public async Task UploadConfigFileAsync(string projectId, string nodeId, string configContent, Gns3Node node)
    {
        var detailedNode = await GetNodeAsync(projectId, nodeId);

        string filePath;
        if (detailedNode.NodeType == "dynamips" &&
            detailedNode.Properties != null &&
            detailedNode.Properties.TryGetValue("dynamips_id", out var dynamipsIdObj) &&
            dynamipsIdObj != null)
        {
            // dynamips_id is the global instance counter → matches the i{n} prefix
            var dynamipsId = ((JsonElement)dynamipsIdObj).GetInt32();
            filePath = $"configs/i{dynamipsId}_startup-config.cfg";
        }
        else
        {
            // Fallback for non-Dynamips nodes
            filePath = "configs/startup-config.cfg";
        }

        _logger.LogDebug("Uploading config to {FilePath} for node {NodeName}", filePath, node.Name);

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
    /// Stop a node.
    /// </summary>
    public async Task StopNodeAsync(string projectId, string nodeId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/v2/projects/{projectId}/nodes/{nodeId}/stop",
                null);

            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Stopped node {NodeId}", nodeId);
        }
        catch (HttpRequestException ex)
        {
            throw new Gns3Exception(
                $"Failed to stop node {nodeId}.", ex);
        }
    }

    /// <summary>
    /// Start a node.
    /// </summary>
    public async Task StartNodeAsync(string projectId, string nodeId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/v2/projects/{projectId}/nodes/{nodeId}/start",
                null);

            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Started node {NodeId}", nodeId);
        }
        catch (HttpRequestException ex)
        {
            throw new Gns3Exception(
                $"Failed to start node {nodeId}.", ex);
        }
    }

    /// <summary>
    /// Reload a node to apply new configuration.
    /// </summary>
    public async Task ReloadNodeAsync(string projectId, string nodeId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/v2/projects/{projectId}/nodes/{nodeId}/reload",
                null);

            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Reloaded node {NodeId}", nodeId);
        }
        catch (HttpRequestException ex)
        {
            throw new Gns3Exception(
                $"Failed to reload node {nodeId}.", ex);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}