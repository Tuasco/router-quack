using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class ValidBgpRelationshipsTests
{
    private readonly ILogger<ValidBgpRelationships> _logger = Substitute.For<ILogger<ValidBgpRelationships>>();

    [Test]
    public async Task Validate_BothNone_NoErrors()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();

        var context = ContextFactory.Create(asses: [intf1.ParentRouter.ParentAs, intf2.ParentRouter.ParentAs]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_BothPeer_NoErrors()
    {
        var (intf1, intf2) = CreateLinkedInterfaces(BgpRelationship.Peer, BgpRelationship.Peer);

        var context = ContextFactory.Create(asses: [intf1.ParentRouter.ParentAs, intf2.ParentRouter.ParentAs]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_ClientProvider_NoErrors()
    {
        var (intf1, intf2) = CreateLinkedInterfaces(BgpRelationship.Client, BgpRelationship.Provider);

        var context = ContextFactory.Create(asses: [intf1.ParentRouter.ParentAs, intf2.ParentRouter.ParentAs]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_ProviderClient_NoErrors()
    {
        var (intf1, intf2) = CreateLinkedInterfaces(BgpRelationship.Provider, BgpRelationship.Client);

        var context = ContextFactory.Create(asses: [intf1.ParentRouter.ParentAs, intf2.ParentRouter.ParentAs]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_MismatchedRelationships_SetsErrorsOccurred()
    {
        var (intf1, intf2) = CreateLinkedInterfaces(BgpRelationship.Client, BgpRelationship.Client);

        var context = ContextFactory.Create(asses: [intf1.ParentRouter.ParentAs, intf2.ParentRouter.ParentAs]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();


        await Assert.That(validator.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_NoneAndPeer_SetsErrorsOccurred()
    {
        var (intf1, intf2) = CreateLinkedInterfaces(BgpRelationship.None, BgpRelationship.Peer);

        var context = ContextFactory.Create(asses: [intf1.ParentRouter.ParentAs, intf2.ParentRouter.ParentAs]);
        var validator = new ValidBgpRelationships(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsTrue();
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