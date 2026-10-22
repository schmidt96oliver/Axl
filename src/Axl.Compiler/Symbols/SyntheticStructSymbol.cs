using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class SyntheticStructSymbol(
    string name,
    Symbol owner,
    bool isPublic,
    bool isPrimitive)
    : StructSymbol(name, owner, isPublic, isPrimitive)
{
    private ImmutableArray<Symbol>? _members;

    public override ImmutableArray<Symbol> Members =>
        _members ?? throw new InvalidOperationException($"Members of '{Name}' not bound yet.");
    
    /// <summary>
    /// Declaring the struct symbol and binding its members must be two phases.
    /// <see cref="SetMembers"/> is called, when members are bound. Members must
    /// be set before use and can only be set once.
    /// </summary>
    internal void SetMembers(ImmutableArray<Symbol> members)
    {
        Guard.IsState(_members is null, $"Members of {Name} already bound.");
        
        _members = members;
    }
}