using System.Net;

namespace RouterQuack.Models;

public class Router
{
    public required string Name { get; set; }

    public IPAddress Id { get; set; } = new (new byte[] { 1, 1, 1, 1 });

    public required int OspfArea { get; set; }
    
    public required ICollection<Interface> Interfaces { get; set; }
    
    public required As ParentAs  { get; set; }
}