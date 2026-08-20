using Axl.Compiler.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public class FoldingRangeHandler : FoldingRangeHandlerBase
{
    protected override FoldingRangeRegistrationOptions CreateRegistrationOptions(FoldingRangeCapability capability,
        ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(Lsp.LanguageId, Lsp.TestLanguageId)
        };

    public override Task<Container<FoldingRange>?> Handle(FoldingRangeRequestParam request,
        CancellationToken cancellationToken)
    {
        var compilation = DocumentStore.GetCompilation(request.TextDocument.Uri);
        if (compilation is null)
            return Task.FromResult<Container<FoldingRange>?>(null);

        var container = Container.From(DocumentStore.GetFileIds(request.TextDocument.Uri)
            .Select(compilation.GetSyntaxTree)
            .SelectMany(GetFoldingRanges));
            
        return Task.FromResult(container);
    }

    private IEnumerable<FoldingRange> GetFoldingRanges(SyntaxTree tree)
    {
        return EnumerateAllChildNodes(tree.Root)
            .Select(GetFoldingRange)
            .Where(range => range is not null)
            .Select(range => range!);

        FoldingRange? GetFoldingRange(SyntaxNode node)
        {
            if (node.Kind is not (SyntaxKind.ModuleDecl or SyntaxKind.BlockExpr))
                return null;
            if (node.SyntaxSpan?.IsEmpty != false)
                return null;

            // Folding range starts one token after `{` and ends
            // one token before `}`.
            
            var start = node.Children
                .SkipWhile(el => el is not Token { Kind: TokenKind.OpenBrace })
                .Skip(1)
                .FirstOrDefault();
            var end = node.Children
                .Reverse()
                .SkipWhile(el => el is not Token { Kind: TokenKind.CloseBrace })
                .Skip(1)
                .FirstOrDefault();
            
            // start might be the closing brace in `{}` and end
            // vica versa. Don't publish a folding range then.
            if (start is null || end is null ||
                start is Token { Kind: TokenKind.CloseBrace } ||
                end is Token { Kind: TokenKind.OpenBrace })
            {
                return null;
            }
            
            var startLinePos = tree.Source.File.GetLinePositionOrEof(start.Span.First);
            var endLinePos = tree.Source.File.GetLinePositionOrEof(end.Span.End);
            return new FoldingRange()
            {
                StartLine = startLinePos.Line,
                StartCharacter = startLinePos.Column,
                EndLine = endLinePos.Line,
                EndCharacter = endLinePos.Column,
                
            };
        }
    }

    private IEnumerable<SyntaxNode> EnumerateAllChildNodes(SyntaxNode node)
    {
        foreach (var child in node.Children.OfType<SyntaxNode>())
        {
            yield return child;

            foreach (var childNode in EnumerateAllChildNodes(child))
                yield return childNode;
        }
    }
}