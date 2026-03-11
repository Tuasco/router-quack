using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Utils;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Processors;

public class PopulateRouterIdsTests
{
    private readonly ILogger<PopulateRouterIds> _logger = Substitute.For<ILogger<PopulateRouterIds>>();
    private readonly RouterUtils _routerUtils = new();

    [Test]
    public async Task Process_RouterWithIdSet_KeepsExistingId()
    {
        var existingId = IPAddress.Parse("192.168.1.1");
        var routers = new List<Router> { TestData.CreateRouter(id: existingId, useDefaultId: false) };
        var asses = new List<As> { TestData.CreateAs(routers: routers) };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateRouterIds(_logger, context, _routerUtils);
        processor.Process();

        await Assert.That(routers[0].Id!).IsEqualTo(existingId);
    }

    [Test]
    public async Task Process_RouterWithoutId_UsesLoopbackAddressV4()
    {
        var loopbackIp = IPAddress.Parse("10.0.0.1");
        var routers = new List<Router>
        {
            TestData.CreateRouter(id: null,
                loopbackAddressV4: loopbackIp,
                useDefaultId: false)
        };
        var asses = new List<As> { TestData.CreateAs(routers: routers) };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateRouterIds(_logger, context, _routerUtils);
        processor.Process();

        await Assert.That(routers[0].Id!).IsEqualTo(loopbackIp);
    }

    [Test]
    public async Task Process_RouterWithoutIdAndNoLoopback_UsesDefaultId()
    {
        const string routerName = "R1";
        var routers = new List<Router>
        {
            TestData.CreateRouter(name: routerName,
                id: null,
                loopbackAddressV4: null,
                useDefaultId: false)
        };
        var asses = new List<As> { TestData.CreateAs(routers: routers) };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateRouterIds(_logger, context, _routerUtils);
        processor.Process();

        await Assert.That(routers[0].Id!).IsEqualTo(_routerUtils.GetDefaultId(routerName));
    }
}