using Axl.Compiler;
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
            
        return Task.FromResult(container)!;
    }

    private IEnumerable<FoldingRange> GetFoldingRanges(SyntaxTree tree)
    {
        foreach (var range in GetCommentFoldingRanges(tree.Root, tree.Source))
            yield return range;
        
        foreach (var node in EnumerateAllChildNodes(tree.Root))
        {
            if (GetFnOrModuleFoldingRange(node) is FoldingRange foldingRange)
                yield return foldingRange;

            foreach (var range in GetCommentFoldingRanges(node, tree.Source))
                yield return range;
        }
        
        FoldingRange? GetFnOrModuleFoldingRange(SyntaxNode node)
        {
            if (node.Kind is not (SyntaxKind.ModuleDecl or SyntaxKind.BlockExpr))
                return null;
            if (node.Span?.IsEmpty != false)
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
            
            return FoldingRangeFromTo(start.FullSpan.First, end.FullSpan.End);
        }

        IEnumerable<FoldingRange> GetCommentFoldingRanges(SyntaxNode node, SourceFileView source)
        {
            for (var i = 0; i < node.Children.Length; i++)
            {
                if (node.Children[i] is not Token { Kind: TokenKind.Comment })
                    continue;

                // Search last comment in this group
                var firstComment = i;
                var lastComment = i;
                for (; i < node.Children.Length; i++)
                {
                    if (node.Children[i] is Token { Kind: TokenKind.Comment })
                        lastComment = i;

                    else if (node.Children[i] is Token { Kind: TokenKind.Whitespace, FullSpan: var span })
                    { 
                        // More than one newline breaks the group.
                        // One newline is expected after each comment.
                        if (source.GetText(span).Count('\n') > 1)
                            break;
                    }
                    
                    else
                        break;
                }

                if (lastComment > firstComment)
                {
                    var lastPos = node.Children[lastComment].FullSpan.End;
                    
                    // If last position is at EOF, the editor will discard the
                    // folding range. Weirdly enough. So we just crop the range
                    // by one at the end. Looks a little weird, but it does the job.
                    yield return FoldingRangeFromTo(
                        start: node.Children[firstComment].FullSpan.End,
                        end: lastPos == source.File.Text.Length
                            ? lastPos - 1
                            : lastPos,
                        kind: FoldingRangeKind.Comment);
                }
            }
        }

        FoldingRange FoldingRangeFromTo(int start, int end, FoldingRangeKind? kind = null)
        {
            var startLinePos = tree.Source.File.GetLinePositionOrEof(start);
            var endLinePos = tree.Source.File.GetLinePositionOrEof(end);
            return new FoldingRange
            {
                StartLine = startLinePos.Line,
                StartCharacter = startLinePos.Column,
                EndLine = endLinePos.Line,
                EndCharacter = endLinePos.Column,
                Kind = kind,
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