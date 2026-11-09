using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundTypeRef : BoundNode, IBoundSymbolRef
{
    public BoundTypeRef(TypeSymbol type, ImmutableArray<BoundTypeRef> typeArguments, SyntaxNode? syntax) : base(syntax)
    {
        Guard.MustBe(typeArguments.Select(a => a.Type).SequenceEqual(type.TypeArguments));
        
        Type = type;
        TypeArguments = typeArguments;
    }

    /// <summary>
    /// Already specialized to <see cref="TypeArguments"/>.
    /// </summary>
    public TypeSymbol Type { get; }
    
    public ImmutableArray<BoundTypeRef> TypeArguments { get; }

    /// <inheritdoc />
    public Symbol Symbol => Type;
}