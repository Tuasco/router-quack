namespace RouterQuack.Core.Models;

public class Interface
{
    public required string Name { get; init; }

    // Initially, this will point to a dummy interface, with one relevant piece of information which is Name.
    // It will be used to resolve the actual neighbour interface starting from the collection of ASs
    public required Interface? Neighbour { get; set; }

    public BgpRelationship Bgp { get; init; } = BgpRelationship.None;
    
    public ICollection<Address>? Addresses { get; init; }
    
    public required Router ParentRouter { get; init; }


    public override string ToString() => $"  - Interface {Name} -> {Neighbour?.ParentRouter.Name}";
}

public enum BgpRelationship
{
    None,
    Client,
    Peer,
    Provider
}