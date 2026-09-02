using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnsupportedFeature(SyntaxElement Element) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Element.GetLocation()];

        public override string Message
            => $"{GetElementText(Element)} is not (yet) supported.";

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
}