using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
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

        var container = Container.From(GetFoldingRanges(compilation.SyntaxTree));
            
        return Task.FromResult(container)!;
    }

    private IEnumerable<FoldingRange> GetFoldingRanges(SyntaxTree tree)
    {
        foreach (var range in GetCommentFoldingRanges(tree.FileSyntax, tree.SourceText))
            yield return range;
        
        foreach (var node in EnumerateAllChildNodes(tree.FileSyntax))
        {
            if (GetFnOrModuleFoldingRange(node) is FoldingRange foldingRange)
                yield return foldingRange;

            foreach (var range in GetCommentFoldingRanges(node, tree.SourceText))
                yield return range;
        }
        
        FoldingRange? GetFnOrModuleFoldingRange(SyntaxNode node)
        {
            if (node.Kind is not (SyntaxKind.ModuleDecl or SyntaxKind.BlockExpr))
                return null;
            if (node.Range?.IsEmpty != false)
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
            
            return FoldingRangeFromTo(start.FullRange.First, end.FullRange.End);
        }

        IEnumerable<FoldingRange> GetCommentFoldingRanges(SyntaxNode node, SourceText source)
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

                    else if (node.Children[i] is Token { Kind: TokenKind.Whitespace, FullRange: var range })
                    { 
                        // More than one newline breaks the group.
                        // One newline is expected after each comment.
                        if (source.GetText(range).Count('\n') > 1)
                            break;
                    }
                    
                    else
                        break;
                }

                if (lastComment > firstComment)
                {
                    var lastPos = node.Children[lastComment].FullRange.End;
                    
                    // If last position is at EOF, the editor will discard the
                    // folding range. Weirdly enough. So we just crop the range
                    // by one at the end. Looks a little weird, but it does the job.
                    yield return FoldingRangeFromTo(
                        start: node.Children[firstComment].FullRange.End,
                        end: lastPos == source.Length
                            ? lastPos - 1
                            : lastPos,
                        kind: FoldingRangeKind.Comment);
                }
            }
        }

        FoldingRange FoldingRangeFromTo(int start, int end, FoldingRangeKind? kind = null)
        {
            var location = SourceLocation.FromBounds(tree.SourceText, start, end);
            return new FoldingRange
            {
                StartLine = location.FirstLine,
                StartCharacter = location.FirstColumn,
                EndLine = location.EndLine,
                EndCharacter = location.EndColumn,
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