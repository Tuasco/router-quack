using System.Net.Http.Json;
using System.Text;
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
    /// Detect all slots with adapters for a Dynamips router.
    /// Returns a list of slot numbers that have adapters installed.
    /// </summary>
    private List<int> DetectDynamipsSlots(Gns3Node node)
    {
        var slots = new List<int>();
        
        if (node.Properties == null)
        {
            _logger.LogWarning("Node {NodeId} has no properties, will try common slots", node.NodeId);
            return [1, 2, 3]; // Try common slots
        }

        // Check slots 0-6 for installed adapters
        for (int slot = 0; slot <= 6; slot++)
        {
            var slotKey = $"slot{slot}";
            if (node.Properties.TryGetValue(slotKey, out var adapter))
            {
                var adapterStr = adapter?.ToString() ?? "";
                // Skip empty slots and FastEthernet adapters (slot 0)
                if (!string.IsNullOrEmpty(adapterStr) && slot > 0)
                {
                    _logger.LogDebug("Found adapter in slot {Slot}: {Adapter}", slot, adapterStr);
                    slots.Add(slot);
                }
            }
        }

        if (slots.Count == 0)
        {
            _logger.LogWarning("No adapter slots found for node {NodeId}, will try common slots", node.NodeId);
            return [1, 2, 3];
        }
        
        return slots;
    }

    /// <summary>
    /// Upload a configuration file to a node.
    /// For Cisco Dynamips routers, we try all detected adapter slots.
    /// </summary>
    public async Task UploadConfigFileAsync(string projectId, string nodeId, string configContent, Gns3Node node)
    {
        try
        {
            // Get detailed node information to detect slots
            var detailedNode = await GetNodeAsync(projectId, nodeId);
            
            List<int> slotsToTry;
            if (detailedNode.NodeType == "dynamips")
            {
                slotsToTry = DetectDynamipsSlots(detailedNode);
                _logger.LogInformation("Uploading config to node {NodeName}, will try slots: {Slots}",
                    node.Name, string.Join(", ", slotsToTry));
            }
            else
            {
                // For non-Dynamips nodes, use a generic path
                slotsToTry = [0];
            }
            
            var content = new StringContent(configContent, Encoding.UTF8, "application/octet-stream");
            bool uploadedSuccessfully = false;
            
            // Try uploading to each detected slot
            foreach (var slot in slotsToTry)
            {
                var slotPrefix = $"i{slot}";
                var filePath = $"configs/{slotPrefix}_startup-config.cfg";
                
                try
                {
                    _logger.LogDebug("Trying to upload to {FilePath}", filePath);
                    var response = await _httpClient.PostAsync(
                        $"/v2/projects/{projectId}/nodes/{nodeId}/files/{filePath}",
                        new StringContent(configContent, Encoding.UTF8, "application/octet-stream"));

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully uploaded config to node {NodeName} slot {Slot}", 
                            node.Name, slotPrefix);
                        uploadedSuccessfully = true;
                        // Don't break - upload to all slots to ensure it works
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug("Upload to slot {Slot} failed: {Error}", slotPrefix, errorContent);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogDebug(ex, "HTTP error uploading to slot {Slot}", slotPrefix);
                }
            }
            
            if (!uploadedSuccessfully)
            {
                throw new Gns3Exception($"Failed to upload configuration to any slot for node {nodeId}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error uploading config to node {NodeId}", nodeId);
            throw new Gns3Exception(
                $"Failed to upload configuration to node {nodeId}.", ex);
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