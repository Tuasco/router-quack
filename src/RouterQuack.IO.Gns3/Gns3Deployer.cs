using System.Collections.Concurrent;
using RouterQuack.Core.ConfigDeployers;
using RouterQuack.IO.Gns3.Exceptions;
using RouterQuack.IO.Gns3.Models;

namespace RouterQuack.IO.Gns3;

/// <summary>
/// Deploys generated router configurations to GNS3 projects.
/// </summary>
public class Gns3Deployer : IConfigDeployer
{
    private readonly Gns3ApiClient _apiClient;
    private readonly ILogger<Gns3Deployer> _logger;
    private readonly Context _context;

    public Gns3Deployer(
        Gns3ApiClient apiClient,
        ILogger<Gns3Deployer> logger,
        Context context)
    {
        _apiClient = apiClient;
        _logger = logger;
        _context = context;
    }

    public string? BeginMessage => "Deploying configurations to GNS3";

    public ILogger Logger => _logger;

    public Context Context => _context;

    /// <summary>
    /// Deploy configurations to GNS3 for all ASes that have deploy info.
    /// </summary>
    public void DeployConfigs(string configDirectory)
    {
        // Find ASes with GNS3 deploy info
        var assesToDeploy = _context.Asses
            .Where(a => a.Deploy?.Gns3 != null)
            .ToList();

        if (assesToDeploy.Count == 0)
        {
            _logger.LogInformation("No ASes configured for GNS3 deployment. Skipping.");
            return;
        }

        _logger.LogInformation("Found {Count} AS(es) configured for GNS3 deployment", assesToDeploy.Count);

        foreach (var @as in assesToDeploy)
        {
            try
            {
                DeployAsAsync(@as, configDirectory).GetAwaiter().GetResult();
            }
            catch (Gns3Exception ex)
            {
                _logger.LogError("Failed to deploy AS {AsNumber}: {Message}", @as.Number, ex.Message);
                _context.ErrorsOccurred = true;
                
                if (_context.Strict)
                {
                    _logger.LogError("Stopping deployment due to error (strict mode)");
                    return;
                }
            }
        }

        if (_context.ErrorsOccurred)
        {
            _logger.LogWarning("Deployment completed with errors");
        }
        else
        {
            _logger.LogInformation("All configurations deployed successfully");
        }
    }

    /// <summary>
    /// Deploy a single AS to GNS3.
    /// </summary>
    private async Task DeployAsAsync(As @as, string configDirectory)
    {
        var gns3Info = @as.Deploy!.Gns3!;
        
        _logger.LogInformation("Deploying AS {AsNumber} to GNS3 project '{Project}'",
            @as.Number, gns3Info.Project);

        // Initialize API client
        _apiClient.Initialize(gns3Info.Server);

        // Get project
        Gns3Project project;
        try
        {
            project = await _apiClient.GetProjectByNameAsync(gns3Info.Project);
            _logger.LogDebug("Found project {ProjectName} (ID: {ProjectId})", 
                project.Name, project.ProjectId);
        }
        catch (Gns3ProjectNotFoundException ex)
        {
            _logger.LogError("{Message}", ex.Message);
            throw;
        }

        // Get all nodes in project
        var nodes = await _apiClient.GetProjectNodesAsync(project.ProjectId);
        _logger.LogDebug("Found {Count} node(s) in project", nodes.Count);

        // Track deployed nodes for potential rollback (thread-safe collection)
        var deployedNodes = new ConcurrentBag<(string NodeId, string RouterName)>();

        try
        {
            // Deploy routers concurrently
            var routersToDeploy = @as.Routers.Where(r => !r.External).ToList();
            
            if (routersToDeploy.Count == 0)
            {
                _logger.LogInformation("AS {AsNumber} has no non-external routers to deploy. Skipping.", @as.Number);
                return;
            }
            
            var deploymentTasks = routersToDeploy.Select(router =>
                DeployRouterAsync(project.ProjectId, @as, router, nodes, configDirectory, deployedNodes)
            );

            await Task.WhenAll(deploymentTasks);

            _logger.LogInformation("Successfully deployed {Count} router(s) for AS {AsNumber}",
                deployedNodes.Count, @as.Number);
        }
        catch (Exception)
        {
            // Rollback on failure
            _logger.LogWarning("Deployment failed, attempting rollback...");
            await RollbackDeploymentAsync(project.ProjectId, deployedNodes);
            throw;
        }
    }

    /// <summary>
    /// Deploy a single router configuration.
    /// </summary>
    private async Task DeployRouterAsync(
        string projectId,
        As @as,
        Router router,
        List<Gns3Node> nodes,
        string configDirectory,
        ConcurrentBag<(string NodeId, string RouterName)> deployedNodes)
    {
        // Find matching node by name
        var node = nodes.FirstOrDefault(n =>
            n.Name.Equals(router.Name, StringComparison.OrdinalIgnoreCase));

        if (node == null)
        {
            var availableNodes = string.Join(", ", nodes.Select(n => $"'{n.Name}'"));
            throw new Gns3NodeNotFoundException(
                $"Router '{router.Name}' not found in GNS3 project. " +
                $"Available nodes: {(availableNodes.Length > 0 ? availableNodes : "none")}");
        }

        _logger.LogDebug("Deploying config for router {RouterName} (Node ID: {NodeId})",
            router.Name, node.NodeId);

        // Read config file - files are organized by AS number in subdirectories
        var configPath = Path.Combine(configDirectory, @as.Number.ToString(), $"{router.Name}.cfg");
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(
                $"Configuration file not found: {configPath}");
        }

        var configContent = await File.ReadAllTextAsync(configPath);

        // For Dynamips routers, we need to stop them before uploading config
        var wasRunning = node.Status == "started";
        
        if (wasRunning)
        {
            _logger.LogDebug("Stopping router {RouterName} before config upload", router.Name);
            await _apiClient.StopNodeAsync(projectId, node.NodeId);
            
            // Wait a bit for the router to fully stop
            await Task.Delay(2000);
        }

        // Upload config to all detected slots
        await _apiClient.UploadConfigFileAsync(projectId, node.NodeId, configContent, node);

        // Start the router if it was running before
        if (wasRunning)
        {
            _logger.LogDebug("Starting router {RouterName} with new config", router.Name);
            await _apiClient.StartNodeAsync(projectId, node.NodeId);
        }

        // Track for rollback
        deployedNodes.Add((node.NodeId, router.Name));

        _logger.LogInformation("Deployed config for router {RouterName}{Status}",
            router.Name,
            wasRunning ? " (restarted)" : " (stopped, start manually)");
    }

    /// <summary>
    /// Rollback deployment by reloading nodes (they will revert to previous config).
    /// </summary>
    private async Task RollbackDeploymentAsync(
        string projectId,
        ConcurrentBag<(string NodeId, string RouterName)> deployedNodes)
    {
        foreach (var (nodeId, routerName) in deployedNodes)
        {
            try
            {
                _logger.LogDebug("Rolling back router {RouterName}", routerName);
                await _apiClient.ReloadNodeAsync(projectId, nodeId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to rollback router {RouterName}: {Message}", 
                    routerName, ex.Message);
            }
        }

        _logger.LogInformation("Rollback completed for {Count} router(s)", deployedNodes.Count);
    }
}