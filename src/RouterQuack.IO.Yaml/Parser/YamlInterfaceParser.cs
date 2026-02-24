namespace RouterQuack.IO.Yaml.Parser;

public partial class YamlParser
{
    private ICollection<Interface> YamlInterfaceToCoreInterface(IDictionary<string, YamlInterface> interfaceDict,
        Router parentRouter)
    {
        ICollection<Interface> interfaces = [];

        foreach (var (key, value) in interfaceDict)
        {
            var dummyNeighbour = new Interface
            {
                Name = value.Neighbour,
                ParentRouter = parentRouter,
                Neighbour = null,
                Addresses = []
            };

            ICollection<Address> addresses = [];
            foreach (var address in value.Addresses ?? [])
                try
                {
                    addresses.Add(new(address));
                }
                catch (ArgumentException e)
                {
                    this.Log(dummyNeighbour, e.Message);
                }

            interfaces.Add(new()
            {
                Name = key,
                Neighbour = dummyNeighbour, // Populate it now, will resolve it later in Step 1
                ParentRouter = parentRouter,
                Addresses = addresses,
                Bgp = interfaceUtils.ParseBgp(value.Bgp)
            });
        }

        return interfaces;
    }
}