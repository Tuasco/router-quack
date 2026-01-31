namespace RouterQuack.Models.Yaml;

public class Router
{
    public int OspfArea { get; set; } = 0;
    
    public required IDictionary<string, Interface> Interfaces { get; set; }
}