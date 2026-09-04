using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnsupportedFeature(SyntaxElement Element, string? CustomMessage = null) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Element.GetLocation()];

        public override string Message
            => CustomMessage ?? $"{GetElementText(Element)} is not (yet) supported.";

        private static string GetElementText(SyntaxElement element)
            => element switch
            {
                Token token => token.Kind.DisplayName,
                SyntaxNode node => node.Kind.ToString(),
                _ => "??"
            };
    }

    public sealed record ModuleDeclAfterCode(ModuleDeclSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.GetLocation()];

        public override string Message
            => "Module declarations must come before members and code.";
    }

    public sealed record MultipleModuleDecls(ModuleDeclSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.GetLocation()];

        public override string Message
            => "There can only be one module declaration.";
    }
    public sealed record StmtInModuleFile(StmtSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.GetLocation()];

        public override string Message
            => "Modules cannot contain statements.";
    }

    public sealed record DuplicateParameters(ImmutableArray<LocalSymbol> ParametersWithSameName) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [.. ParametersWithSameName.Select(local => ((ParamSyntax)local.Syntax).Name.GetLocation())];

        public override string LocationLabel
            => "Also declared here.";

        public override string Message
            => $"Duplicate parameter '{ParametersWithSameName[0].Name}'.";
    }

    public sealed record MissingParameterTypeAnnotation(ParamSyntax ParamSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [ParamSyntax.GetLocation()];

        public override string Message
            => "Type annotation is missing.";
    }
}