using System.Collections.Concurrent;
using RouterQuack.Core.ConfigDeployers;
using RouterQuack.Core.Extensions;
using RouterQuack.IO.Gns3.Exceptions;
using RouterQuack.IO.Gns3.Models;
using RouterQuack.IO.Gns3.Utils;

namespace RouterQuack.IO.Gns3;

/// <summary>
/// Deploys generated router configurations to GNS3 projects.
/// </summary>
public sealed class Gns3Deployer(
    Gns3ApiClient apiClient,
    ILogger<Gns3Deployer> logger,
    Context context) : IConfigDeployer
{
    private readonly Gns3ApiClient _apiClient = apiClient;
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public string? BeginMessage => "Deploying configurations to GNS3";

    /// <summary>
    /// Deploy configurations to GNS3 for all ASes that have deploy info.
    /// </summary>
    public void DeployConfigs(string configDirectory)
    {
        // Find ASes with GNS3 deploy info
        var assesToDeploy = context.Asses
            .Where(a => a.Deploy?.Gns3 != null)
            .ToList();

        if (assesToDeploy.Count == 0)
        {
            Logger.LogInformation("No ASes configured for GNS3 deployment. Skipping.");
            return;
        }

        Logger.LogInformation("Found {Count} AS(es) configured for GNS3 deployment", assesToDeploy.Count);

        foreach (var @as in assesToDeploy)
        {
            var success = DeployAsAsync(@as, configDirectory).GetAwaiter().GetResult();

            if (!success && Context.Strict)
                return;
        }
    }

    /// <summary>
    /// Deploy a single AS to GNS3.
    /// </summary>
    private async Task<bool> DeployAsAsync(As @as, string configDirectory)
    {
        var gns3Info = @as.Deploy!.Gns3!;

        _apiClient.Initialize(gns3Info.Server);

        var project = await _apiClient.GetProjectByNameAsync(gns3Info.Project);
        if (project is null)
        {
            this.LogError("Project '{ProjectName}' not found on GNS3 server at {Server}.",
                gns3Info.Project, gns3Info.Server);
            return false;
        }

        var nodes = await _apiClient.GetProjectNodesAsync(project.ProjectId);
        var deployedNodes = new ConcurrentBag<(string NodeId, string RouterName)>();

        var routersToDeploy = @as.Routers.Where(r => !r.External).ToList();
        if (routersToDeploy.Count == 0)
        {
            Logger.LogInformation("AS {AsNumber} has no non-external routers to deploy. Skipping.", @as.Number);
            return true;
        }

        var deploymentTasks = routersToDeploy.Select(router =>
            DeployRouterAsync(project.ProjectId, @as, router, nodes, configDirectory, deployedNodes));

        var results = await Task.WhenAll(deploymentTasks);
        if (results.Any(success => !success))
        {
            await RollbackDeploymentAsync(project.ProjectId, deployedNodes);
            return false;
        }

        Logger.LogInformation("Successfully deployed {Count} router(s) for AS {AsNumber}",
            deployedNodes.Count, @as.Number);
        return true;
    }

    /// <summary>
    /// Deploy a single router configuration.
    /// </summary>
    private async Task<bool> DeployRouterAsync(
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
            this.LogError("router '{routerName}' not found in GNS3 project. Available nodes: {availableNodes}",
                router.Name, availableNodes);
            return false;
        }

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
            await _apiClient.ControlNodeAsync(projectId, node.NodeId, Gns3ApiClient.NodeOperation.Stop);
            // Wait a bit for the router to fully stop
            await Task.Delay(2000);
        }

        // Upload config to all detected slots
        await _apiClient.UploadConfigFileAsync(projectId, node.NodeId, configContent, node);

        // Start the router if it was running before
        if (wasRunning)
        {
            await _apiClient.ControlNodeAsync(projectId, node.NodeId, Gns3ApiClient.NodeOperation.Start);
        }

        // Track for rollback
        deployedNodes.Add((node.NodeId, router.Name));

        Logger.LogDebug("Deployed config for router {RouterName}{Status}",
            router.Name,
            wasRunning ? " (restarted)" : " (stopped, start manually)");

        return true;
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
                await _apiClient.ControlNodeAsync(projectId, nodeId, Gns3ApiClient.NodeOperation.Reload);
            }
            catch (Gns3Exception)
            {
                Logger.LogWarning("Failed to rollback router {RouterName}.", routerName);
            }
        }

        Logger.LogInformation("Rollback completed for {Count} router(s)", deployedNodes.Count);
    }
}
