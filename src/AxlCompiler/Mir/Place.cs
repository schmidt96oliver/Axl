using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Mir;

public readonly record struct LocalId(int Value)
{
    /// <inheritdoc />
    public override string ToString()
        => $"_{Value}";
}

public enum LocalKind
{
    Return,
    Parameter,
    Variable,
    Temp
}

public closed record Place(TypeSymbol Type, SyntaxNode? Syntax)
{
    public sealed record Local(LocalId Id, LocalKind Kind, TypeSymbol Type, SyntaxNode? Syntax = null) : Place(Type, Syntax);

    public sealed record Field(Place Receiver, FieldSymbol FieldSymbol, SyntaxNode? Syntax = null) : Place(FieldSymbol.Type, Syntax);

    public sealed record Deref(Place Reference, SyntaxNode? Syntax = null)
        : Place(((RefTypeSymbol)Reference.Type).Inner, Syntax);

    public sealed record Discard(TypeSymbol Type) : Place(Type, null);

    public sealed record Error(TypeSymbol Type) : Place(Type, null);
}