namespace RouterQuack.IO.Yaml.Models;

public class DeployInfo
{
    public Gns3Info? Gns3 {get; init; }
}

public class Gns3Info
{
    public required Uri ServerUrl { get; init; }
    public required string ProjectName { get; init; }
}