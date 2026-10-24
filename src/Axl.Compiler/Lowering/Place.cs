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
}