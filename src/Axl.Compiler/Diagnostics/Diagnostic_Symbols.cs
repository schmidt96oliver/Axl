using System.Collections.Immutable;
using Axl.Compiler.Syntax;

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
}