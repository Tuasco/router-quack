using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Models;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Processors.Models;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Processors;

public class PopulateVrfRdRtTests
{
    private readonly ILogger<PopulateVrfRdRt> _logger = Substitute.For<ILogger<PopulateVrfRdRt>>();

    private static Vrf CreateVrf(string name, string? rd = null,
        ICollection<string>? importTargets = null,
        ICollection<string>? exportTargets = null) => new()
    {
        Name = name,
        RouteDistinguisher = rd,
        ImportTargets = importTargets,
        ExportTargets = exportTargets
    };

    [Test]
    public async Task Process_VrfWithoutRdRt_GeneratesFromAsnAndIndex()
    {
        var vrf = CreateVrf("CUSTOMER_A");
        var router = TestData.CreateRouter(vrfs: [vrf]);
        var asses = new List<As> { TestData.CreateAs(number: 111, routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        new PopulateVrfRdRt(_logger, context).Process();

        await Assert.That(vrf.RouteDistinguisher).IsEqualTo("111:1");
        await Assert.That(vrf.ImportTargets).Contains("111:100");
        await Assert.That(vrf.ExportTargets).Contains("111:100");
    }

    [Test]
    public async Task Process_VrfWithRdAlreadySet_KeepsExistingRd()
    {
        var vrf = CreateVrf("CUSTOMER_A", rd: "999:42");
        var router = TestData.CreateRouter(vrfs: [vrf]);
        var asses = new List<As> { TestData.CreateAs(number: 111, routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        new PopulateVrfRdRt(_logger, context).Process();

        await Assert.That(vrf.RouteDistinguisher).IsEqualTo("999:42");
    }

    [Test]
    public async Task Process_VrfWithRtAlreadySet_KeepsExistingRt()
    {
        var vrf = CreateVrf("CUSTOMER_A",
            importTargets: ["999:42"],
            exportTargets: ["999:42"]);
        var router = TestData.CreateRouter(vrfs: [vrf]);
        var asses = new List<As> { TestData.CreateAs(number: 111, routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        new PopulateVrfRdRt(_logger, context).Process();

        await Assert.That(vrf.ImportTargets).Contains("999:42");
        await Assert.That(vrf.ExportTargets).Contains("999:42");
    }

    [Test]
    public async Task Process_TwoVrfs_GetDifferentIndices()
    {
        var vrfA = CreateVrf("CUSTOMER_A");
        var vrfB = CreateVrf("CUSTOMER_B");
        var router = TestData.CreateRouter(vrfs: [vrfA, vrfB]);
        var asses = new List<As> { TestData.CreateAs(number: 111, routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        new PopulateVrfRdRt(_logger, context).Process();

        await Assert.That(vrfA.RouteDistinguisher).IsEqualTo("111:1");
        await Assert.That(vrfB.RouteDistinguisher).IsEqualTo("111:2");
        await Assert.That(vrfA.ImportTargets).Contains("111:100");
        await Assert.That(vrfB.ImportTargets).Contains("111:200");
    }

    [Test]
    public async Task Process_SameVrfNameAcrossRouters_GetsSameIndex()
    {
        var vrf1 = CreateVrf("CUSTOMER_A");
        var vrf2 = CreateVrf("CUSTOMER_A");
        var r1 = TestData.CreateRouter(name: "R1", vrfs: [vrf1]);
        var r2 = TestData.CreateRouter(name: "R2", vrfs: [vrf2]);
        var asses = new List<As> { TestData.CreateAs(number: 111, routers: [r1, r2]) };

        var context = ContextFactory.Create(asses: asses);
        new PopulateVrfRdRt(_logger, context).Process();

        // Same VRF name → same index → same RD on both routers
        await Assert.That(vrf1.RouteDistinguisher).IsEqualTo("111:1");
        await Assert.That(vrf2.RouteDistinguisher).IsEqualTo("111:1");
    }

    [Test]
    public async Task Process_RouterWithNoVrfs_DoesNotThrow()
    {
        var router = TestData.CreateRouter(vrfs: []);
        var asses = new List<As> { TestData.CreateAs(routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        var act = () => new PopulateVrfRdRt(_logger, context).Process();

        await Assert.That(act).ThrowsNothing();
    }
}