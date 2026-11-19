using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

/// <summary>
/// A reference to a <see cref="TypeSymbol"/> which still requires type arguments.
/// </summary>
public sealed class BoundUnspecializedTypeRef : BoundNode, IBoundSymbolRef
{
    /// <summary>
    /// A reference to a <see cref="TypeSymbol"/> which still requires type arguments.
    /// </summary>
    public BoundUnspecializedTypeRef(TypeSymbol type, SyntaxNode? syntax) : base(syntax)
    {
        Guard.MustBe(type.TypeParameters.Length > 0 && type.TypeArguments.SequenceEqual(type.TypeParameters));
        Type = type;
    }

    public TypeSymbol Type { get; }

    /// <inheritdoc />
    public Symbol Symbol => Type;
}