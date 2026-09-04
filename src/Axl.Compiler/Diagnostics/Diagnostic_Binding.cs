using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Diagnostics;

public partial record Diagnostic
{
    public record TypeMismatch(
        AxlType SourceType,
        AxlType TargetType,
        ExprSyntax SourceSyntax,
        TypeNameSyntax TargetSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [SourceSyntax.GetLocation()];

        public override string Message
            => $"Expression of type '{SourceType.DisplayName}' is not assignable to '{TargetType.DisplayName}'.";

        public override ImmutableArray<LabeledSourceLocation> Related
            => [new(TargetSyntax.GetLocation(), "Target type declared here.")];
    }

    public sealed record MissingInitializer(VarDeclSyntax VarDeclSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [VarDeclSyntax.GetLocation()];

        public override string Message
            => "Initializer must be specified.";
    }

    public sealed record NumberSuffixMismatch(NumberLiteralSyntax NumberLiteralSyntax, AxlType TypeFromSuffix) : Error
    {
        public override ImmutableArray<SourceLocation> Locations 
            => [NumberLiteralSyntax.GetLocation()];

        public override string Message
            => $"Decimal numbers can only have types 'f32' or 'f64'. Got '{TypeFromSuffix.DisplayName}'.";
    }

    public sealed record StringInterpolationTypeMismatch(ExprSyntax InterpolationExpr, AxlType ActualType) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [InterpolationExpr.GetLocation()];

        public override string Message
            => $"For now, string interpolations must have type 'string'. Got '{ActualType.DisplayName}'.";
    }

    public sealed record UndefinedName(IdNameSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.GetLocation()];

        public override string Message
            => $"Undefined name '{Syntax.Token.Identifier}'.";
    }

    public sealed record AmbiguousName(IdNameSyntax Syntax, ImmutableArray<Symbol> Candidates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations 
            => [Syntax.GetLocation()];

        public override string Message 
            => $"Ambiguous reference. Candidates are:\n{GetCandidatesText()}";

        public override ImmutableArray<LabeledSourceLocation> Related
            =>
            [
                .. Candidates
                    .Where(candidate => candidate.DeclaringSyntaxes.Length > 0)
                    .Select(candidate =>
                        new LabeledSourceLocation(candidate.DeclaringSyntaxes[0].GetLocation(), "This is a candidate."))
            ];
        
        private string GetCandidatesText()
            => string.Join('\n', Candidates.Select(symbol => symbol.DisplayName));
    }

    public sealed record InvalidLocalRef(IdNameSyntax Syntax, Symbol ResolvedSymbol) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.GetLocation()];

        public override string Message
            => $"Expected reference to a local variable. Got {ResolvedSymbol.DisplayName} instead.";
    }
}