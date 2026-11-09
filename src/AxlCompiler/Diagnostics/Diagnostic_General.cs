using System.Collections.Immutable;
using AxlCompiler.Syntax;
using AxlCompiler.Text;

namespace AxlCompiler.Diagnostics;

public closed partial record Diagnostic
{
    public sealed record UnsupportedFeature(SyntaxElement Element, string? CustomMessage = null) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => Element is SyntaxNode node ? [node.SyntaxElements().First().Location] : [Element.Location];

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
}