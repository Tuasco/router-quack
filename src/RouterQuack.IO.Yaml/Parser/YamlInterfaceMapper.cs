using System.Collections;
using Microsoft.Extensions.Logging;
using NeighbourInfo = RouterQuack.IO.Yaml.Models.NeighbourInfo;

namespace RouterQuack.IO.Yaml.Parser;

/// <summary>
/// Maps <see cref="YamlInterface"/> definitions to core <see cref="Interface"/> models.
/// </summary>
public class YamlInterfaceMapper(ILogger<YamlInterfaceMapper> logger)
{
    /// <summary>
    /// Convert <see cref="YamlInterface"/> definitions of a router into core interface models.
    /// </summary>
    /// <param name="interfaceDict"><see cref="YamlInterface"/> definitions keyed by interface name.</param>
    /// <param name="parentRouter">Router that owns the mapped interfaces.</param>
    /// <param name="context">Using execution context.</param>
    /// <returns>The mapped interfaces.</returns>
    public ICollection<Interface> Map(IDictionary<string, YamlInterface> interfaceDict, Router parentRouter,
        Context context)
    {
        ICollection<Interface> interfaces = [];

        foreach (var (key, value) in interfaceDict)
        {
            var name = ParseNeighbour(value.Neighbour, parentRouter.ParentAs.Number)?.ToString();

            if (name is null)
                LogInterfaceError(context, "Invalid neighbour format", parentRouter, key);

            // This dummy neighbour is only there to hold a reference path (name) to the actual neighbour
            // which is resolved in the first processor (00_ResolveNeighbours).
            var dummyNeighbour = new Interface
            {
                Name = name ?? string.Empty,
                ParentRouter = parentRouter,
                Neighbour = null,
                AdditionalConfig = null,
                Addresses = []
            };

            ICollection<Address> addresses = [];
            foreach (var address in value.Addresses ?? [])
            {
                try
                {
                    addresses.Add(new(address));
                }
                catch (ArgumentException e)
                {
                    LogInterfaceError(context,
                        $"Invalid address {address} ({e.Message})",
                        parentRouter,
                        key);
                }
            }

            interfaces.Add(new()
            {
                Name = key,
                Neighbour = dummyNeighbour, // Populate it now, will resolve it later in Step 1
                ParentRouter = parentRouter,
                AdditionalConfig = value.AdditionalConfig,
                Addresses = addresses,
            });
        }

        return interfaces;
    }

    private void LogInterfaceError(Context context, string message, Router parentRouter, string interfaceName)
    {
        logger.LogError(
            #pragma warning disable CA2254
            "Interface {InterfaceName} of router {RouterName} in AS number {AsNumber}: " + message + '.',
            interfaceName, parentRouter.Name, parentRouter.ParentAs.Number);
        #pragma warning restore CA2254
        context.ApplyError();
    }

    private static NeighbourInfo? ParseNeighbour(object? neighbour, int defaultAs)
    {
        return neighbour switch
        {
            null => null,
            string s => new(s, defaultAs),
            IDictionary dict => ParseNeighbourFromDict(dict, defaultAs),
            _ => null
        };
    }

    private static NeighbourInfo? ParseNeighbourFromDict(IDictionary dict, int defaultAs)
    {
        var asNumber = defaultAs;
        var router = string.Empty;
        string? @interface = null;

        foreach (DictionaryEntry kvp in dict)
        {
            var key = kvp.Key.ToString();
            var val = kvp.Value?.ToString();

            switch (key)
            {
                case "as":
                    if (int.TryParse(val, out var parsed))
                        asNumber = parsed;
                    break;

                case "router":
                    router = val ?? string.Empty;
                    break;

                case "interface":
                    @interface = val;
                    break;

                default:
                    return null;
            }
        }

        return new(asNumber, router, @interface);
    }
}