using System.Net;

namespace RouterQuack.Tests.Unit.TestHelpers;

internal static class TestData
{
    internal static As CreateAs(
        int number = 1,
        IgpType igp = IgpType.Ibgp,
        IPNetwork? loopbackSpace = null,
        IPNetwork? networksSpaceV4 = null,
        IPNetwork? networksSpaceV6 = null,
        IpVersion networksIpVersion = IpVersion.Ipv6,
        ICollection<Router>? routers = null)
    {
        var routerList = routers?.ToList() ?? [];

        var @as = new As
        {
            Number = number,
            Igp = igp,
            LoopbackSpace = loopbackSpace,
            NetworksSpaceV4 = networksSpaceV4,
            NetworksSpaceV6 = networksSpaceV6,
            NetworksIpVersion = networksIpVersion,
            Routers = routerList
        };

        foreach (var router in routerList)
            SetParentAs(router, @as);

        return @as;
    }

    internal static Router CreateRouter(
        string name = "R1",
        IPAddress? id = null,
        RouterBrand brand = RouterBrand.Cisco,
        Address? loopbackAddress = null,
        int ospfArea = 0,
        bool external = false,
        ICollection<Interface>? interfaces = null,
        As? parentAs = null)
    {
        var interfaceList = interfaces?.ToList() ?? [];

        var router = new Router
        {
            Name = name,
            Id = id ?? IPAddress.Parse("1.2.3.4"),
            Brand = brand,
            LoopbackAddress = loopbackAddress,
            OspfArea = ospfArea,
            External = external,
            Interfaces = interfaceList,
            ParentAs = parentAs!
        };

        foreach (var @interface in interfaceList)
            SetParentRouter(@interface, router);

        return router;
    }

    internal static Interface CreateInterface(
        string name = "GigabitEthernet0/0",
        Interface? neighbour = null,
        BgpRelationship bgp = BgpRelationship.None,
        ICollection<Address>? addresses = null,
        Router? parentRouter = null)
    {
        return new()
        {
            Name = name,
            Neighbour = neighbour,
            Bgp = bgp,
            Addresses = addresses?.ToList() ?? [],
            ParentRouter = parentRouter!
        };
    }

    internal static Address CreateAddress(string ip, int prefixLength)
    {
        var ipAddress = IPAddress.Parse(ip);
        return new Address(new IPNetwork(ipAddress, prefixLength), ipAddress);
    }

    private static void SetParentAs(Router router, As parentAs)
    {
        var field = typeof(Router).GetProperty("ParentAs");
        field?.SetValue(router, parentAs);
    }

    private static void SetParentRouter(Interface @interface, Router parentRouter)
    {
        var field = typeof(Interface).GetProperty("ParentRouter");
        field?.SetValue(@interface, parentRouter);
    }
}