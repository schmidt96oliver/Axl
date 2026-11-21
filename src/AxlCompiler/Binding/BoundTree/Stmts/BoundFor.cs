using AxlCompiler.Symbols;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundFor(
    IterationVariableSymbol iterationVariable,
    BoundValue iterator,
    FieldSymbol startField,
    FieldSymbol endField,
    BoundValue body,
    ForStmtSyntax syntax) : BoundStmt(syntax)
{
    public IterationVariableSymbol IterationVariable { get; } = iterationVariable;
    public BoundValue Iterator { get; } = iterator;
    public FieldSymbol StartField { get; } = startField;
    public FieldSymbol EndField { get; } = endField;
    public BoundValue Body { get; } = body;

    /// <inheritdoc />
    public override IEnumerable<BoundStmt> GetChildren()
        => [Iterator, Body];
}