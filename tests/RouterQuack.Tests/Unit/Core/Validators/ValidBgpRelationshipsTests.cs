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
    [Arguments(BgpRelationship.Internal, BgpRelationship.Internal)]
    [Arguments(BgpRelationship.Internal, BgpRelationship.None)]
    [Arguments(BgpRelationship.None, BgpRelationship.Internal)]
    public async Task Validate_MatchedRelationships_NoErrors(BgpRelationship bgp1, BgpRelationship bgp2)
    {
        var (intf1, intf2) = CreateLinkedInterfaces(bgp1, bgp2);

        var context = ContextFactory.Create(asses: [intf1.ParentRouter.ParentAs, intf2.ParentRouter.ParentAs]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    [Arguments(BgpRelationship.None, BgpRelationship.Client)]
    [Arguments(BgpRelationship.None, BgpRelationship.Provider)]
    [Arguments(BgpRelationship.None, BgpRelationship.Peer)]
    [Arguments(BgpRelationship.Internal, BgpRelationship.Client)]
    [Arguments(BgpRelationship.Internal, BgpRelationship.Provider)]
    [Arguments(BgpRelationship.Internal, BgpRelationship.Peer)]
    [Arguments(BgpRelationship.Client, BgpRelationship.Client)]
    [Arguments(BgpRelationship.Provider, BgpRelationship.Provider)]
    public async Task Validate_MismatchedRelationships_SetsErrorsOccurred(BgpRelationship bgp1, BgpRelationship bgp2)
    {
        var (intf1, intf2) = CreateLinkedInterfaces(bgp1, bgp2);

        var context = ContextFactory.Create(asses: [intf1.ParentRouter.ParentAs, intf2.ParentRouter.ParentAs]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();


        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_NoneAndPeer_SetsErrorsOccurred()
    {
        var (intf1, intf2) = CreateLinkedInterfaces(BgpRelationship.None, BgpRelationship.Peer);

        var context = ContextFactory.Create(asses: [intf1.ParentRouter.ParentAs, intf2.ParentRouter.ParentAs]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    private static (Interface, Interface) CreateLinkedInterfaces(
        BgpRelationship bgp1 = BgpRelationship.None,
        BgpRelationship bgp2 = BgpRelationship.None,
        int as1 = 1,
        int as2 = 1)
    {
        var interface1 = TestData.CreateInterface(bgp: bgp1);
        var interface2 = TestData.CreateInterface(bgp: bgp2);

        interface1.Neighbour = interface2;
        interface2.Neighbour = interface1;

        TestData.CreateAs(number: as1, routers: [TestData.CreateRouter(name: "R1", interfaces: [interface1])]);
        TestData.CreateAs(number: as2, routers: [TestData.CreateRouter(name: "R2", interfaces: [interface2])]);

        return (interface1, interface2);
    }
}