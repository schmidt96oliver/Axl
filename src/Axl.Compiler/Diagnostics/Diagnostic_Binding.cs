using System.Collections.Immutable;
using Axl.Compiler.Semantics.Hir;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Diagnostics;

public partial record Diagnostic
{
    public record TypeMismatch(HirExpr Expr, TypeSymbol Expected) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Expr.Syntax.GetLocation()];

        public override string Message
            => $"Expected type '{Expected.DisplayName}' but got '{Expr.Type.DisplayName}'.";
    }

    public sealed record MissingInitializer(VarDeclSyntax VarDeclSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [VarDeclSyntax.GetLocation()];

        public override string Message
            => "Initializer must be specified.";
    }

    public sealed record NumberSuffixMismatch(NumberLiteralSyntax NumberLiteralSyntax, TypeSymbol TypeFromSuffix) : Error
    {
        public override ImmutableArray<SourceLocation> Locations 
            => [NumberLiteralSyntax.GetLocation()];

        public override string Message
            => $"Decimal numbers can only have types 'f32' or 'f64'. Got '{TypeFromSuffix.DisplayName}'.";
    }

    public sealed record UndefinedName(IdNameSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.GetLocation()];
    
        public override string Message
            => $"Undefined name '{Syntax.Token.Identifier}'.";
    }

    public sealed record UndefinedOperator(Token OperatorToken, ImmutableArray<HirExpr> BoundOperands) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [OperatorToken.GetLocation()];

        public override string Message
            => $"Operator '{OperatorToken.Kind.DisplayName}' is not defined for {GetTypeString()}.";

        private string GetTypeString()
            => BoundOperands.Length == 1
                ? $"type '{BoundOperands[0].Type.DisplayName}'"
                : $"types {string.Join(", ", BoundOperands[..^1].Select(hir => $"'{hir.Type.DisplayName}'"))} and '{BoundOperands[^1].Type.DisplayName}'";
    }

    public sealed record InvalidAssignTarget(ExprSyntax Syntax, Symbol? ResolvedSymbol = null) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.GetLocation()];

        public override string Message
            => ResolvedSymbol is null
                ? "Invalid assignment target."
                : $"Cannot assign to {ResolvedSymbol.DisplayName}";
    }
}