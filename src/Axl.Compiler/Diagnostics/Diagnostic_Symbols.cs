using System.Collections.Immutable;
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

    public sealed record InvalidFileScopedModuleDecl(FileScopedModuleDeclSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations 
            => [Syntax.GetLocation()];

        public override string Message => Syntax.Parent is FileSyntax
            ? $"File-scoped module declarations must be the first declaration in the file."
            : $"File-scoped module declarations are only allowed on the file scope.";
    }
}