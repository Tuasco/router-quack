using System.Net;

namespace RouterQuack.Core.Models;

public class Address(IPNetwork networkAddress, IPAddress ipAddress)
{
    public IPNetwork NetworkAddress { get; } = networkAddress;
    
    public IPAddress IpAddress { get; } = ipAddress;

    public override string ToString()
    {
        return $"{IpAddress.ToString()}/{NetworkAddress.PrefixLength}";
    }
}