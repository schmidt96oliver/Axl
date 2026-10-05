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

    public ImmutableArray<FieldSymbol> Fields => [.. Members.OfType<FieldSymbol>()];    

    public StructSymbol(string name, bool isPrimitive, StructDeclSyntax? declSyntax = null) 
        : base(name)
    {
        IsPrimitive = isPrimitive;
        DeclarationSyntax = declSyntax;
    }
}