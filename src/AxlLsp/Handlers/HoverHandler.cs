using AxlCompiler.Symbols;
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

        if (ProjectStore.TryMapToAxl(uri, request.Position) is not { } axlLocation)
            return Task.FromResult<Hover?>(null);

        var context = SyntaxContext.At(axlLocation);
        
        if (Settings.ShowBoundExprAtHover)
            return Task.FromResult(DebugBoundNodeHover(context));
        
        var word = context.TouchingWord;
        if (word is null) return Task.FromResult<Hover?>(null);
        
        //TODO: Parent NameRef => ReferencedSymbolFor
        //                   _ => DeclaredSymbolFor
        if (word.Parent is not NameRefSyntax nameRef)
            return Task.FromResult<Hover?>(null);

        var symbol = axlLocation.Compilation.SemanticModel.ReferencedSymbolFor(nameRef);
        if (symbol is null)
            return Task.FromResult<Hover?>(null);

        var lspLocation = ProjectStore.TryMapToLsp(word.Location);
        if (lspLocation is null || lspLocation.Uri != uri)
            return Task.FromResult<Hover?>(null);

        return Task.FromResult<Hover?>(new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(symbol.FullDisplayText), 
            Range = lspLocation.Range
        });
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