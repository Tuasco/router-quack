using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are duplicate IP Addresses.
/// </summary>
public class NoDuplicateIpAddress(ILogger<NoDuplicateIpAddress> logger) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public ILogger Logger { get; set; } = logger;

    public void Validate(ICollection<As> asses)
    {
        var addresses = asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .SelectMany(i => i.Addresses)
            .CountBy(a => a.IpAddress)
            .Where(c => c.Value > 1)
            .Select(i => i.Key);

        foreach (var address in addresses)
            this.LogError("Duplicate addresses \"{IpAddress}\".", address);
    }
}