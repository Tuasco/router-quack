using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class NoDuplicateRouterNamesTests
{
    private readonly ILogger<NoDuplicateRouterNames> _logger = Substitute.For<ILogger<NoDuplicateRouterNames>>();

    [Test]
    [Arguments("R1", "R2", "R3")]
    public async Task Validate_NoDuplicates_NoErrors(params string[] names)
    {
        var routers = names.Select(r => TestData.CreateRouter(name: r));
        var asses = new List<As> { TestData.CreateAs(routers: routers.ToArray()) };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateRouterNames(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    [Arguments("R1", "R2")]
    public async Task Validate_WithDuplicates_SetsErrorsOccurred(params string[] names)
    {
        var routers = names.Select(r => TestData.CreateRouter(name: r)).ToList();
        routers.Add(TestData.CreateRouter(name: names[Random.Shared.Next(names.Length)]));
        var asses = new List<As> { TestData.CreateAs(routers: routers.ToArray()) };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateRouterNames(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsTrue();
    }

    [Test]
    [Arguments("R1")]
    public async Task Validate_DuplicatesInDifferentAs_SetsErrorsOccurred(string name)
    {
        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, routers: [TestData.CreateRouter(name: name)]),
            TestData.CreateAs(number: 2, routers: [TestData.CreateRouter(name: name)])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateRouterNames(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsTrue();
    }
}