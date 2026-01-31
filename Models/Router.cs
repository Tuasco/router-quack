using System.Net;

namespace RouterQuack.Models;

public class Router
{
    public required string Name { get; init; }

    public required IPAddress Id { get; init; }
    
    public required int OspfArea { get; init; }
    
    public required ICollection<Interface> Interfaces { get; set; }
    
    public required As ParentAs { get; init; }
    
    public required RouterBrand Brand { get; init; }


    public static IPAddress GetDefaultId(string routerName)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(routerName));
    
        var bytes = new byte[4];
        Array.Copy(hash, bytes, 4);
    
        bytes[0] = Math.Max((byte) 1, bytes[0]);
        bytes[3] = Math.Max((byte) 1, bytes[3]);
    
        return new IPAddress(bytes);
    }
}

public enum RouterBrand
{
    Unknwon,
    Cisco
}