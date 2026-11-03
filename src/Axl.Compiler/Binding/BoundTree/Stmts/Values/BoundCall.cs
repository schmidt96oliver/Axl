using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundCall(
    FunSymbol fun,
    BoundArgument? receiver,
    ImmutableArray<BoundArgument> arguments,
    SyntaxNode? syntax = null)
    : BoundValue(fun.ReturnType, syntax)
{
    public FunSymbol Fun { get; } = fun;
    public BoundArgument? Receiver { get; } = receiver;
    public ImmutableArray<BoundArgument> Arguments { get; } = arguments;

    public bool IsMutReturn => Fun.IsReturnMut;

    public ImmutableArray<BoundArgument> ReceiverAndArguments => Receiver is not null
        ? [Receiver, .. Arguments]
        : Arguments;

    /// <inheritdoc />
    public override PlaceKind PlaceKind => IsMutReturn ? PlaceKind.MutPlace : PlaceKind.Value;

    /// <inheritdoc />
    public override BoundValue? PlaceRoot => IsMutReturn ? this : null;

    public override IEnumerable<BoundStmt> GetChildren()
        => ReceiverAndArguments.Select(arg => arg.Value);
}

public sealed class BoundArgument(IdNameSyntax? nameSyntax, BoundValue value, bool isMut, ArgSyntax? syntax)
{
    public IdNameSyntax? NameSyntax => nameSyntax;
    public IdNameSyntax? FieldNameSyntax => nameSyntax ?? (Syntax as ArgSyntax)?.Expr as IdNameSyntax;
    public BoundValue Value { get; } = value;
    public bool IsMut { get; } = isMut;

    public TypeSymbol Type => Value.Type;
    public ArgSyntax? Syntax { get; } = syntax;
}