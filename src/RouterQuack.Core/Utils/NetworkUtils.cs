using System.Net;
using System.Security.Cryptography;
using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Utils;

public interface INetworkUtils
{
    public IPAddress GetDefaultId(string routerName);

    public Address ParseIpAddress(string ip);

    public IgpType ParseIgp(string? igp);

    public BgpRelationship ParseBgp(string? bgp);

    public RouterBrand ParseBrand(string? brand, RouterBrand? defaultBrand = null);
}

public class NetworkUtils : INetworkUtils
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
    
    public Address ParseIpAddress(string ip)
    {
        var parts = ip.Split('/');
        
        if(parts.Length != 2)
            throw new InvalidCastException("Couldn't translate IP address");
        
        if (!int.TryParse(parts[1], out var mask))
            throw new InvalidCastException("Couldn't translate IP address (invalid mask)");
            
        if (!IPAddress.TryParse(parts[0], out var ipAddress))
            throw new InvalidCastException("Couldn't translate IP address (invalid IP)");
        
        return new (new (ipAddress, mask), ipAddress);
    }

    public IgpType ParseIgp(string? igp)
        => Enum.TryParse<IgpType>(igp, ignoreCase: true, out var igpType)
            ? igpType
            : IgpType.Ibgp;

    public BgpRelationship ParseBgp(string? bgp)
        => Enum.TryParse<BgpRelationship>(bgp, ignoreCase: true, out var bgpRelationship)
            ? bgpRelationship
            : BgpRelationship.None;

    public RouterBrand ParseBrand(string? brand, RouterBrand? defaultBrand = null)
        => Enum.TryParse<RouterBrand>(brand, ignoreCase: true, out var routerBrand)
            ? routerBrand
            : defaultBrand ?? RouterBrand.Unknwon;
}