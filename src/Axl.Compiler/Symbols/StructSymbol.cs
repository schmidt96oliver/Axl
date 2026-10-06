using System.Collections.Immutable;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class StructSymbol : TypeSymbol
{
    /// <summary>
    /// Whether this structs memory representation is internal.
    /// </summary>
    public bool IsPrimitive { get; }

    public StructDeclSyntax? DeclarationSyntax { get; }

    private ImmutableArray<Symbol>? _members;

    public override ImmutableArray<Symbol> Members =>
        _members ?? throw new InvalidOperationException($"Members of '{Name}' not bound yet.");
    
    
    public ImmutableArray<FieldSymbol> Fields => [.. Members.OfType<FieldSymbol>()];    

    public StructSymbol(string name, bool isPrimitive, StructDeclSyntax? declSyntax = null) 
        : base(name)
    {
        IsPrimitive = isPrimitive;
        DeclarationSyntax = declSyntax;
    }

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