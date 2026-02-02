using System.Net;
using System.Text;

namespace RouterQuack.Models;

public class Router
{
    public required string Name { get; init; }

    public required IPAddress Id { get; init; }
    
    public required int OspfArea { get; init; }
    
    public required ICollection<Interface> Interfaces { get; set; }
    
    public required As ParentAs { get; init; }
    
    public required RouterBrand Brand { get; init; }


    // TODO extract from class, it really doesn't have anything to do here.
    public static IPAddress GetDefaultId(string routerName)
    {
        var hash = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(routerName));
    
        var bytes = new byte[4];
        Array.Copy(hash, bytes, 4);
    
        bytes[0] = Math.Max((byte) 1, bytes[0]);
        bytes[3] = Math.Max((byte) 1, bytes[3]);
    
        return new IPAddress(bytes);
    }
    
    public override string ToString()
    {
        var str = new StringBuilder();
        str.AppendLine($"* {Brand.ToString()} router {Name}:");
        
        foreach (var @interface in Interfaces)
            str.AppendLine(@interface.ToString());
        
        return str.ToString();
    }
}

public enum RouterBrand
{
    Unknwon,
    Cisco
}