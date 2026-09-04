using System.Collections.Immutable;
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
}