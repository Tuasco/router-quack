using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class ValidBgpRelationshipsTests
{
    private readonly ILogger<ValidBgpRelationships> _logger = Substitute.For<ILogger<ValidBgpRelationships>>();

    [Test]
    [Arguments(BgpRelationship.None, BgpRelationship.None)]
    [Arguments(BgpRelationship.Peer, BgpRelationship.Peer)]
    [Arguments(BgpRelationship.Provider, BgpRelationship.Client)]
    [Arguments(BgpRelationship.Client, BgpRelationship.Provider)]
    public async Task Validate_MatchedRelationships_NoErrors(BgpRelationship bgp1, BgpRelationship bgp2)
    {
        var intf1 = TestData.CreateInterface(bgp: bgp1);
        var intf2 = TestData.CreateInterface(bgp: bgp2);
        TestData.LinkInterfaces(intf1, intf2);

        var as1 = TestData.CreateAs(number: 1, routers: [TestData.CreateRouter(name: "R1", interfaces: [intf1])]);
        var as2 = TestData.CreateAs(number: 1, routers: [TestData.CreateRouter(name: "R2", interfaces: [intf2])]);

        var context = ContextFactory.Create(asses: [as1, as2]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    [Arguments(BgpRelationship.None, BgpRelationship.Client)]
    [Arguments(BgpRelationship.None, BgpRelationship.Provider)]
    [Arguments(BgpRelationship.None, BgpRelationship.Peer)]
    [Arguments(BgpRelationship.Client, BgpRelationship.Client)]
    [Arguments(BgpRelationship.Provider, BgpRelationship.Provider)]
    public async Task Validate_MismatchedRelationships_SetsErrorsOccurred(BgpRelationship bgp1, BgpRelationship bgp2)
    {
        var intf1 = TestData.CreateInterface(bgp: bgp1);
        var intf2 = TestData.CreateInterface(bgp: bgp2);
        TestData.LinkInterfaces(intf1, intf2);

        var as1 = TestData.CreateAs(number: 1, routers: [TestData.CreateRouter(name: "R1", interfaces: [intf1])]);
        var as2 = TestData.CreateAs(number: 1, routers: [TestData.CreateRouter(name: "R2", interfaces: [intf2])]);

        var context = ContextFactory.Create(asses: [as1, as2]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_NoneAndPeer_SetsErrorsOccurred()
    {
        var intf1 = TestData.CreateInterface(bgp: BgpRelationship.None);
        var intf2 = TestData.CreateInterface(bgp: BgpRelationship.Peer);
        TestData.LinkInterfaces(intf1, intf2);

        var as1 = TestData.CreateAs(number: 1, routers: [TestData.CreateRouter(name: "R1", interfaces: [intf1])]);
        var as2 = TestData.CreateAs(number: 1, routers: [TestData.CreateRouter(name: "R2", interfaces: [intf2])]);

        var context = ContextFactory.Create(asses: [as1, as2]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }
}