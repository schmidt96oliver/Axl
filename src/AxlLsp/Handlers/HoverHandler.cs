using AxlCompiler;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlLsp.Projects;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace AxlLsp.Handlers;

public class HoverHandler : HoverHandlerBase
{
    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability,
        ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = TextDocumentSelector.ForAxlAndTaxl
        };

    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;

        if (ProjectStore.TryMapToAxl(request) is not { } axlLocation)
            return Task.FromResult<Hover?>(null);

        var context = SyntaxContext.At(axlLocation);

        if (Settings.ShowBoundExprAtHover)
            return Task.FromResult(DebugBoundNodeHover(context));

        var word = context.TouchingWord;
        if (word?.Kind is not TokenKind.Identifier) 
            return Task.FromResult<Hover?>(null);

        var symbol = axlLocation.Compilation.SemanticModel.SymbolFor(word);

        if (HoverTextFor(symbol) is { } markup)
        {
            var lspLocation = ProjectStore.TryMapToLsp(word.Location);
            if (lspLocation is null || lspLocation.Uri != uri)
                return Task.FromResult<Hover?>(null);

            return Task.FromResult<Hover?>(new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(markup),
                Range = lspLocation.Range
            });
        }

        return Task.FromResult<Hover?>(null);
    }

    private static MarkupContent? HoverTextFor(SymbolReference reference) => reference switch
    {
        SymbolReference.Resolved resolved => new MarkupContent{ Kind = MarkupKind.Markdown, Value =
            $"```axl\n{resolved.Symbol.FullDisplayText}\n```"
        },
        SymbolReference.Unresolved unresolved => HoverTextFor(unresolved),
    };

    private static MarkupContent? HoverTextFor(SymbolReference.Unresolved unresolved)
    {
        if (unresolved.Reason is UnresolvedSymbolReason.NotFound || unresolved.Candidates.Length == 0)
            return null;

        var candidatesText = string.Join('\n', unresolved.Candidates.Select(s => s.FullDisplayText));
        return new MarkupContent { Kind = MarkupKind.Markdown, Value = $"{unresolved.Reason}:\n```axl\n{candidatesText}\n```" };
    }
    
    private static Hover? DebugBoundNodeHover(SyntaxContext context)
    {
        var token = context.SyntaxTree.TokenAt(context.Caret);
        var exprSyntax = token.Ancestors().OfType<ExprSyntax>().FirstOrDefault();
        if (exprSyntax is null) return null;
        
        var node = context.Compilation.SemanticModel.BoundNodeFor(exprSyntax);
        var lspLocation = ProjectStore.TryMapToLsp(exprSyntax.Location);
        if (lspLocation is null)
            return null;

        return new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(node.GetType().Name),
            Range = lspLocation.Range
        };
    }

}