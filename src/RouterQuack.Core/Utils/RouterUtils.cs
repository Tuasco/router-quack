using System.Net;
using System.Security.Cryptography;
using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Utils;

public interface IRouterUtils
{
    public IPAddress GetDefaultId(string routerName);
    
    public RouterBrand ParseBrand(string? brand, RouterBrand? defaultBrand = null);
}

public class RouterUtils : IRouterUtils
{
    public IPAddress GetDefaultId(string routerName)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(routerName));
    
        var bytes = new byte[4];
        Array.Copy(hash, bytes, 4);
    
        bytes[0] = Math.Max((byte) 1, bytes[0]);
        bytes[3] = Math.Max((byte) 1, bytes[3]);
    
        return new IPAddress(bytes);
    }
    
    public RouterBrand ParseBrand(string? brand, RouterBrand? defaultBrand = null)
    {
        if (brand == null)
            return defaultBrand ?? RouterBrand.Cisco;
        
        return Enum.TryParse<RouterBrand>(brand, ignoreCase: true, out var routerBrand)
            ? routerBrand
            : throw new ArgumentException("Couldn't parse router brand");
    }
}