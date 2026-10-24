using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Lowering;

public readonly record struct LocalId(int Value)
{
    /// <inheritdoc />
    public override string ToString()
        => $"_{Value}";
}

public closed record Place(TypeSymbol Type, SyntaxNode? Syntax)
{
    public sealed record Local(LocalId Id, TypeSymbol Type, SyntaxNode? Syntax) : Place(Type, Syntax);

    public sealed record Field(Place Receiver, FieldSymbol FieldSymbol, SyntaxNode? Syntax) : Place(FieldSymbol.Type, Syntax);

    public sealed record Deref(Place Reference, SyntaxNode? Syntax = null)
        : Place(((RefTypeSymbol)Reference.Type).Inner, Syntax);

    public sealed record Discard() : Place(ErrorTypeSymbol.Instance, null)
    {
        public static readonly Discard Instance = new();
    }
}

public sealed class RefTypeSymbol(TypeSymbol inner) : TypeSymbol($"ref {inner.Name}", null, false)
{
    public TypeSymbol Inner { get; } = inner;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members => [];
}