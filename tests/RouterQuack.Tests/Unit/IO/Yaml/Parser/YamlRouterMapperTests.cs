using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.IO.Yaml.Parser;
using RouterQuack.Tests.Unit.TestHelpers;
using YamlAs = RouterQuack.IO.Yaml.Models.As;
using YamlRouter = RouterQuack.IO.Yaml.Models.Router;
using YamlInterface = RouterQuack.IO.Yaml.Models.Interface;

namespace RouterQuack.Tests.Unit.IO.Yaml.Parser;

public class YamlRouterMapperTests
{
    private readonly ILogger<YamlRouterMapper> _logger = Substitute.For<ILogger<YamlRouterMapper>>();
    private readonly ILogger<YamlInterfaceMapper> _interfaceLogger = Substitute.For<ILogger<YamlInterfaceMapper>>();

    [Test]
    public async Task Map_AsLevelVrfsAcrossMultipleRouters_ProducesIndependentInstances()
    {
        // Arrange
        var sharedVrf = new Vrf
        {
            Name = "SHARED_VRF",
            RouteDistinguisher = "100:1",
            ImportTargets = ["100:100"],
            ExportTargets = ["100:100"]
        };

        var yamlAs = new YamlAs
        {
            Routers = new Dictionary<string, YamlRouter>
            {
                ["R1"] = new()
                {
                    Interfaces = new Dictionary<string, YamlInterface>
                    {
                        ["GigabitEthernet0/0"] = new() { Neighbour = "R2" }
                    }
                },
                ["R2"] = new()
                {
                    Interfaces = new Dictionary<string, YamlInterface>
                    {
                        ["GigabitEthernet0/0"] = new() { Neighbour = "R1" }
                    }
                }
            },
            Vrfs = new Dictionary<string, Vrf>
            {
                ["SHARED_VRF"] = sharedVrf
            }
        };

        var parentAs = new As
        {
            Number = 100,
            Igp = IgpType.OSPF,
            Core = CoreType.None,
            AddressFamily = IpVersion.Both,
            Routers = []
        };

        var context = ContextFactory.Create();
        var mapper = new YamlRouterMapper(_logger, new YamlInterfaceMapper(_interfaceLogger));

        // Act
        var routers = mapper.Map(yamlAs, parentAs, context).ToList();

        // Assert
        await Assert.That(routers).Count().IsEqualTo(2);
        await Assert.That(routers[0].Vrfs[0]).IsNotSameReferenceAs(routers[1].Vrfs[0]);
    }

    [Test]
    public async Task Map_AsLevelVrfsAcrossMultipleRouters_MutatingOneDoesNotAffectOther()
    {
        // Arrange
        var sharedVrf = new Vrf
        {
            Name = "SHARED_VRF",
            RouteDistinguisher = null,
            ImportTargets = [],
            ExportTargets = []
        };

        var yamlAs = new YamlAs
        {
            Routers = new Dictionary<string, YamlRouter>
            {
                ["R1"] = new()
                {
                    Interfaces = new Dictionary<string, YamlInterface>
                    {
                        ["GigabitEthernet0/0"] = new() { Neighbour = "R2" }
                    }
                },
                ["R2"] = new()
                {
                    Interfaces = new Dictionary<string, YamlInterface>
                    {
                        ["GigabitEthernet0/0"] = new() { Neighbour = "R1" }
                    }
                }
            },
            Vrfs = new Dictionary<string, Vrf>
            {
                ["SHARED_VRF"] = sharedVrf
            }
        };

        var parentAs = new As
        {
            Number = 100,
            Igp = IgpType.OSPF,
            Core = CoreType.None,
            AddressFamily = IpVersion.Both,
            Routers = []
        };

        var context = ContextFactory.Create();
        var mapper = new YamlRouterMapper(_logger, new YamlInterfaceMapper(_interfaceLogger));

        // Act
        var routers = mapper.Map(yamlAs, parentAs, context).ToList();
        routers[0].Vrfs[0].RouteDistinguisher = "999:1";

        // Assert
        await Assert.That(routers[1].Vrfs[0].RouteDistinguisher).IsNull();
    }

    [Test]
    public async Task Map_RouterLevelVrfOverridesAsLevelVrf_UsesRouterValue()
    {
        // Arrange
        var asVrf = new Vrf
        {
            Name = "CUSTOMER_A",
            RouteDistinguisher = "100:1",
            ImportTargets = ["100:100"],
            ExportTargets = ["100:100"]
        };

        var routerVrf = new Vrf
        {
            Name = "CUSTOMER_A",
            RouteDistinguisher = "200:2",
            ImportTargets = ["200:200"],
            ExportTargets = ["200:200"]
        };

        var yamlAs = new YamlAs
        {
            Routers = new Dictionary<string, YamlRouter>
            {
                ["R1"] = new()
                {
                    Interfaces = new Dictionary<string, YamlInterface>
                    {
                        ["GigabitEthernet0/0"] = new() { Neighbour = "R2" }
                    },
                    Vrfs = new Dictionary<string, Vrf>
                    {
                        ["CUSTOMER_A"] = routerVrf
                    }
                }
            },
            Vrfs = new Dictionary<string, Vrf>
            {
                ["CUSTOMER_A"] = asVrf
            }
        };

        var parentAs = new As
        {
            Number = 100,
            Igp = IgpType.OSPF,
            Core = CoreType.None,
            AddressFamily = IpVersion.Both,
            Routers = []
        };

        var context = ContextFactory.Create();
        var mapper = new YamlRouterMapper(_logger, new YamlInterfaceMapper(_interfaceLogger));

        // Act
        var routers = mapper.Map(yamlAs, parentAs, context).ToList();

        // Assert
        await Assert.That(routers[0].Vrfs).Count().IsEqualTo(1);
        await Assert.That(routers[0].Vrfs[0].RouteDistinguisher).IsEqualTo("200:2");
    }
}