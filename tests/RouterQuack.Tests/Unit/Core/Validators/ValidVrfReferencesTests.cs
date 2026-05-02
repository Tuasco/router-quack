using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class ValidVrfReferencesTests
{
    private readonly ILogger<ValidVrfReferences> _logger = Substitute.For<ILogger<ValidVrfReferences>>();

    [Test]
    public async Task Validate_InterfaceVrfDeclaredOnRouter_NoErrors()
    {
        var vrf = new Vrf { Name = "CUSTOMER_A" };
        var iface = TestData.CreateInterface(vrf: "CUSTOMER_A");
        var router = TestData.CreateRouter(interfaces: [iface], vrfs: [vrf]);
        var asses = new List<As> { TestData.CreateAs(routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        new ValidVrfReferences(_logger, context).Validate();

        await Assert.That(context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_InterfaceVrfNotDeclaredOnRouter_SetsErrorsOccurred()
    {
        var iface = TestData.CreateInterface(vrf: "CUSTOMER_A");
        var router = TestData.CreateRouter(interfaces: [iface], vrfs: []); // no VRFs declared
        var asses = new List<As> { TestData.CreateAs(routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        new ValidVrfReferences(_logger, context).Validate();

        await Assert.That(context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_InterfaceWithNoVrf_NoErrors()
    {
        var iface = TestData.CreateInterface(); // vrf: null
        var router = TestData.CreateRouter(interfaces: [iface], vrfs: []);
        var asses = new List<As> { TestData.CreateAs(routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        new ValidVrfReferences(_logger, context).Validate();

        await Assert.That(context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_MultipleInterfaces_OnlyOrphanedOneErrors()
    {
        var vrf = new Vrf { Name = "CUSTOMER_A" };
        var validIface = TestData.CreateInterface(name: "GigabitEthernet0/0", vrf: "CUSTOMER_A");
        var orphanIface = TestData.CreateInterface(name: "GigabitEthernet1/0", vrf: "CUSTOMER_B"); // not declared
        var router = TestData.CreateRouter(interfaces: [validIface, orphanIface], vrfs: [vrf]);
        var asses = new List<As> { TestData.CreateAs(routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        new ValidVrfReferences(_logger, context).Validate();

        await Assert.That(context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_RouterWithNoVrfsOrVrfInterfaces_NoErrors()
    {
        var router = TestData.CreateRouter(vrfs: []);
        var asses = new List<As> { TestData.CreateAs(routers: [router]) };

        var context = ContextFactory.Create(asses: asses);
        new ValidVrfReferences(_logger, context).Validate();

        await Assert.That(context.ErrorsOccurred).IsFalse();
    }
}