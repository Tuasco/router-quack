using System.Net;
using RouterQuack.Core.Utils;

namespace RouterQuack.Tests.Unit.TestHelpers;

internal static class TestData
{
    private static readonly RouterUtils RouterUtils = new();

    internal static As CreateAs(
        int number = 1,
        IgpType igp = IgpType.OSPF,
        CoreType core = CoreType.None,
        IPNetwork? loopbackSpaceV4 = null,
        IPNetwork? loopbackSpaceV6 = null,
        IPNetwork? networksSpaceV4 = null,
        IPNetwork? networksSpaceV6 = null,
        IpVersion ipVersion = IpVersion.IPv6 | IpVersion.IPv4,
        ICollection<Router>? routers = null)
    {
        var routerList = routers?.ToList() ?? [];

        var @as = new As
        {
            Number = number,
            Igp = igp,
            Core = core,
            LoopbackSpaceV4 = loopbackSpaceV4,
            LoopbackSpaceV6 = loopbackSpaceV6,
            NetworksSpaceV4 = networksSpaceV4,
            NetworksSpaceV6 = networksSpaceV6,
            AddressFamily = ipVersion,
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
        IPAddress? loopbackAddressV4 = null,
        IPAddress? loopbackAddressV6 = null,
        BgpConfig? bgp = null,
        bool external = false,
        ICollection<Interface>? interfaces = null,
        ICollection<Vrf>? vrfs = null,
        As? parentAs = null,
        bool useDefaultId = true,
        string? additionalConfig = null)
    {
        var interfaceList = interfaces?.ToList() ?? [];

        var router = new Router
        {
            Name = name,
            Id = id,
            Brand = brand,
            LoopbackAddressV4 = loopbackAddressV4,
            LoopbackAddressV6 = loopbackAddressV6,
            Vrfs = vrfs?.ToList() ?? [],
            Bgp = bgp ?? new(),
            External = external,
            Interfaces = interfaceList,
            ParentAs = parentAs!,
            AdditionalConfig = additionalConfig
        };

        if (useDefaultId)
            router.Id ??= RouterUtils.GetDefaultId(name);

        foreach (var @interface in interfaceList)
            SetParentRouter(@interface, router);

        return router;
    }

    internal static Interface CreateInterface(
        string name = "GigabitEthernet0/0",
        BgpRelationship bgp = BgpRelationship.None,
        ICollection<Address>? addresses = null,
        Router? parentRouter = null,
        string? additionalConfig = null,
        string? vrf = null)
    {
        return new()
        {
            Name = name,
            Neighbour = null,
            Bgp = bgp,
            Addresses = addresses?.ToList() ?? [],
            ParentRouter = parentRouter!,
            AdditionalConfig = additionalConfig,
            Vrf = vrf
        };
    }

    internal static Address CreateAddress(string ip, int prefixLength)
    {
        var ipAddress = IPAddress.Parse(ip);
        return new(new(ipAddress, prefixLength), ipAddress);
    }

    internal static void LinkInterfaces(
        Interface a,
        Interface b,
        bool withValidLinkAddresses = false)
    {
        a.Neighbour = b;
        b.Neighbour = a;

        if (withValidLinkAddresses)
        {
            var networkV4 = IPNetwork.Parse("192.168.1.0/31");
            a.Addresses.Add(new(networkV4, IPAddress.Parse("192.168.1.0")));
            b.Addresses.Add(new(networkV4, IPAddress.Parse("192.168.1.1")));

            var networkV6 = IPNetwork.Parse("2001:db8::/127");
            a.Addresses.Add(new(networkV6, IPAddress.Parse("2001:db8::")));
            b.Addresses.Add(new(networkV6, IPAddress.Parse("2001:db8::1")));
        }
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