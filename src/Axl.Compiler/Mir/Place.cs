using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Mir;

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
    public sealed record Local(LocalId Id, LocalKind Kind, TypeSymbol Type, SyntaxNode? Syntax) : Place(Type, Syntax);

    public sealed record Field(Place Receiver, FieldSymbol FieldSymbol, SyntaxNode? Syntax) : Place(FieldSymbol.Type, Syntax);

    public sealed record Deref(Place Reference, SyntaxNode? Syntax = null)
        : Place(((RefTypeSymbol)Reference.Type).Inner, Syntax);

    public sealed record Discard(TypeSymbol Type) : Place(Type, null);
}