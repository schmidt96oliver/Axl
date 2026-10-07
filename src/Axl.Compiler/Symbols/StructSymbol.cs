using System.Collections.Immutable;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class StructSymbol(
    string name,
    Symbol parent,
    bool isPublic,
    bool isPrimitive,
    StructDeclSyntax? declSyntax = null)
    : TypeSymbol(name, parent, isPublic)
{
    /// <summary>
    /// Whether this structs memory representation is internal.
    /// </summary>
    public bool IsPrimitive { get; } = isPrimitive;

    public StructDeclSyntax? DeclarationSyntax { get; } = declSyntax;

    private ImmutableArray<Symbol>? _members;

    public override ImmutableArray<Symbol> Members =>
        _members ?? throw new InvalidOperationException($"Members of '{Name}' not bound yet.");
    
    
    public ImmutableArray<FieldSymbol> Fields => [.. Members.OfType<FieldSymbol>()];

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