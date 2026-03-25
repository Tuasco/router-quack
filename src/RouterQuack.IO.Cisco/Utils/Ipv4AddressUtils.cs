using System.Net;

namespace RouterQuack.IO.Cisco.Utils;

public static class Ipv4AddressUtils
{
    public static string GetV4Mask(int subnet)
    {
        var mask = (0xffffffffL << (32 - subnet)) & 0xffffffffL;
        mask = IPAddress.HostToNetworkOrder((int)mask);
        return new IPAddress((uint)mask).ToString();
    }
}