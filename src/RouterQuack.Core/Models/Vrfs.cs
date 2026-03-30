namespace RouterQuack.Core.Models;

/// <summary>
/// Represents a VRF instance on a PE router for MPLS BGP VPN.
/// </summary>
public sealed class Vrf
{
    /// <summary>VRF name, e.g. "CUSTOMER_A".</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Route Distinguisher — makes VPN routes globally unique.
    /// Format: "ASN:nn", e.g. "111:1".
    /// </summary>
    public string? RouteDistinguisher { get; set; }

    /// <summary>Route targets to import into this VRF.</summary>
    public ICollection<string>? ImportTargets { get; set; }

    /// <summary>Route targets to export from this VRF.</summary>
    public ICollection<string>? ExportTargets { get; set; }
}