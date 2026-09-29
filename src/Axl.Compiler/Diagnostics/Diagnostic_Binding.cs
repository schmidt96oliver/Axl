using System.Collections.Immutable;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;
using Axl.Compiler.Text;

namespace Axl.Compiler.Diagnostics;

public partial record Diagnostic
{
    public sealed record TypeMismatch(BoundExpr Expr, TypeSymbol Expected) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Expr.Syntax.Location];

        public override string Message
            => $"Expected type '{Expected.Name}' but got '{Expr.Type.Name}'.";
    }

    public sealed record MissingInitializer(VarDeclSyntax VarDeclSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [VarDeclSyntax.SyntaxElements().First().Location];

        public override string Message
            => "Initializer must be specified.";
    }

    public sealed record SuffixInvalidForDecimalNumber(NumberLiteralSyntax NumberLiteralSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations 
            => [NumberLiteralSyntax.Tree.SourceText.GetLocation(NumberLiteralSyntax.Token.SuffixRange)];

        public override string Message
            => $"Suffix '{NumberLiteralSyntax.Token.Suffix.ToString().ToLower()}' describes an integral type. Expected a type that describes a decimal number.";
    }

    public sealed record UndefinedName(IdNameSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.Location];
    
        public override string Message
            => $"Undefined name '{Syntax.Token.Identifier}'.";
    }

    public sealed record UndefinedOperator(string OperatorName, ImmutableArray<TypeSymbol> OperandTypes, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                if (Syntax is ExprStmtSyntax)
                {
                    // Do not mark the semicolon at the end.
                    var nonSemicolonElements = Syntax.SyntaxElements()
                        .TakeWhile(el => el is not Token { Kind: TokenKind.Semicolon })
                        .ToList();
                    var range = SourceRange.FromTo(nonSemicolonElements[0].Range!.Value, nonSemicolonElements[^1].Range!.Value);
                    return [Syntax.Tree.SourceText.GetLocation(range)];
                }

                return [Syntax.Location];
            }
        }

        public override string Message
            => $"Operator '{OperatorName}' is not defined for {GetTypeString()}.";

        private string GetTypeString()
            => OperandTypes.Length == 1
                ? $"type '{OperandTypes[0].Name}'"
                : $"types {string.Join(", ", OperandTypes[..^1].Select(expr => $"'{expr.Name}'"))} and '{OperandTypes[^1].Name}'";
    }

    public sealed record InvalidAssignTarget(ExprSyntax Syntax, Symbol? ResolvedSymbol = null) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.Location];

        public override string Message
            => ResolvedSymbol switch
            {
                null => "The assignment target must be a variable.",
                ParameterSymbol => $"Cannot assign to parameter '{ResolvedSymbol.Name}'.",
                VariableSymbol { IsReadOnly: true } => $"Cannot assign to readonly variable '{ResolvedSymbol.Name}'.",
                _ =>
                    $"'{ResolvedSymbol.Name}' is {ResolvedSymbol.Kind.DisplayName}. The assignment target must be a variable."
            };
    }

    public sealed record BreakOrContinueOutsideLoop(ExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.SyntaxElements().First().Location];

        public override string Message => Syntax is BreakExprSyntax
            ? "Break is only valid inside loops."
            : "Continue is only valid inside loops.";
    }

    public sealed record MissingElse(IfExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.SyntaxElements().First().Location];

        public override string Message
            => "'if' must have an 'else' branch if used as an expression.";
    }

    public sealed record IncompatibleBranches(BoundExpr First, BoundExpr Second) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [First.Syntax.Location, Second.Syntax.Location];

        public override string Message
            => $"Both 'if' and 'else' branches must have the same type. Got '{First.Type.Name}' and '{Second.Type.Name}'.";
    }

    public sealed record UnexpectedSymbolKind(SyntaxNode Syntax, Symbol Symbol, SymbolKind Expected) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"'{Symbol.Name}' is {Symbol.Kind.DisplayName}. Expected {Expected.DisplayName}.";
    }

    public sealed record UndefinedMember(IdNameSyntax Syntax, Symbol? Symbol) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => Symbol is not null 
            ? $"'{Symbol.Name}' has no member '{Syntax.Token.Identifier}'."
            : $"Could not resolved member '{Syntax.Token.Identifier}'.";
    }

    public sealed record CannotConvert(ExprSyntax Syntax, TypeSymbol From, TypeSymbol To) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"Cannot convert from type '{From.Name}' to '{To.Name}'.";
    }

    public sealed record ArityMismatch(ArgListSyntax Syntax, FunSymbol Fun, int Got) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                var argExprs = Syntax.Arguments.ToList();
                if (argExprs.Count == 0) return [Syntax.Location];
                
                var range = SourceRange.FromTo(argExprs[0].Location.Range, argExprs[^1].Location.Range);
                return [Syntax.Tree.SourceText.GetLocation(range)];
            }
        }

        public override string Message =>
            $"'{Fun.Name}' has {Fun.Parameters.Length} parameter(s), but was called with {Got} argument(s).";
    }

    public sealed record InvalidCallee(ExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"Expected {SymbolKind.Fun.DisplayName}.";
    }

    public sealed record NumberTooBig(NumberLiteralSyntax Syntax, TypeSymbol TargetType) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message
            => $"The integral is too big to fit into '{TargetType.Name}'.";
    }

    public sealed record CannotCallWithoutReceiver(FunSymbol Fun, ExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"'{Fun.Name}' is a method. It cannot be called from static context.";
    }
    
    public sealed record CannotCallWithReceiver(FunSymbol Fun, ExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"'{Fun.Name}' is static. It cannot be called from an instance.";
    }

    public sealed record DuplicateFunDeclarations(ImmutableArray<FunSymbol> Duplicates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle the name and parameter list, since they are at fault.
            =>
            [
                .. Duplicates.Where(fun => fun.DeclarationSyntax is not null)
                    .Select(fun =>
                        fun.DeclarationSyntax!.Location.SourceText.GetLocation(SourceRange.FromTo(fun.DeclarationSyntax!.Name.Location.Range,
                            fun.DeclarationSyntax!.ParameterList.Location.Range)))
            ];

        public override string LocationLabel => "Also declared here.";

        public override string Message => $"'{Duplicates[0].Name}' with the same signature is already declared.";
    }

    public sealed record DuplicateParameters(ImmutableArray<ParameterSymbol> Duplicates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle all parameter names
            => [.. Duplicates.Where(param => param.DeclarationSyntax is not null).Select(param => param.DeclarationSyntax!.Name.Location)];

        public override string LocationLabel => "Also declared here.";

        public override string Message => $"Duplicate parameter '{Duplicates[0].Name}'.";
    }

    public sealed record CannotResolveFun(FunGroupSymbol FunGroup, ImmutableArray<TypeSymbol> ArgumentTypes, ExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => $"Cannot resolve fun '{FunGroup.Name}({
            string.Join(", ", ArgumentTypes.Select(argType => argType.Name))
        })'.";
    }

    public sealed record CannotShadow(Symbol ShadowedSymbol, IdentifierToken NameSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [NameSyntax.Location];

        public override string Message
            => $"'{ShadowedSymbol.Name}' is already declared on this scope. Only variables can be shadowed.";
    }
}