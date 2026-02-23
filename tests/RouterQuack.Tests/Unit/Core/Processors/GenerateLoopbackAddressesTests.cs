using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Utils;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Processors;

public class GenerateLoopbackAddressesTests
{
    private readonly ILogger<GenerateLoopbackAddresses> _logger = Substitute.For<ILogger<GenerateLoopbackAddresses>>();
    private readonly NetworkUtils _networkUtils = new();

    [Test]
    public async Task Process_RouterWithoutLoopback_GeneratesAddress()
    {
        var routers = new List<Router> { TestData.CreateRouter(loopbackAddress: null, external: false) };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpace: IPNetwork.Parse("10.0.0.0/24"),
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddress).IsNotNull();
        await Assert.That(processor.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_RouterWithExistingLoopback_KeepsExisting()
    {
        var existingAddress = TestData.CreateAddress("10.0.0.100", 32);
        var routers = new List<Router> { TestData.CreateRouter(loopbackAddress: existingAddress, external: false) };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpace: IPNetwork.Parse("10.0.0.0/24"),
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddress!.IpAddress.ToString()).IsEqualTo("10.0.0.100");
    }

    [Test]
    public async Task Process_ExternalRouter_NoLoopbackGenerated()
    {
        var routers = new List<Router> { TestData.CreateRouter(loopbackAddress: null, external: true) };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpace: IPNetwork.Parse("10.0.0.0/24"),
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddress).IsNull();
    }

    [Test]
    public async Task Process_NoLoopbackSpace_SetsErrorsOccurred()
    {
        var routers = new List<Router> { TestData.CreateRouter(loopbackAddress: null, external: false) };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpace: null,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(processor.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Process_MultipleRouters_GeneratesUniqueAddresses()
    {
        var routers = new List<Router>
        {
            TestData.CreateRouter(name: "Router1", loopbackAddress: null, external: false),
            TestData.CreateRouter(name: "Router2", loopbackAddress: null, external: false)
        };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpace: IPNetwork.Parse("10.0.0.0/24"),
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddress!.IpAddress)
            .IsNotEqualTo(routers[1].LoopbackAddress!.IpAddress);
    }
}