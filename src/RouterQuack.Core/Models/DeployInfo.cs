namespace RouterQuack.Core.Models;

public class DeployInfo
{
    public Gns3Info? Gns3 {get; init; }
}

public class Gns3Info
{
    /// <summary>
    /// GNS3 server URL (e.g., http://localhost:3080).
    /// YAML property: server
    /// </summary>
    public required Uri Server { get; init; }
    
    /// <summary>
    /// Name of the GNS3 project to deploy to.
    /// YAML property: project
    /// </summary>
    public required string Project { get; init; }
}