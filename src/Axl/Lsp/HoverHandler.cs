using Axl.Compiler.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public class HoverHandler : HoverHandlerBase
{
    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability,
        ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(Lsp.LanguageId, Lsp.TestLanguageId)
        };

    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var compilation = DocumentStore.GetCompilation(request.TextDocument.Uri);
        if (compilation is null)
            return Task.FromResult<Hover?>(null);
        
        if (request.Position.Line < 0 || request.Position.Line >= compilation.SyntaxTree.SourceText.Lines.Length)
            return Task.FromResult<Hover?>(null);
        
        var index = compilation.SyntaxTree.SourceText.Lines[request.Position.Line].Start + request.Position.Character;
        var node = compilation.Analysis.SyntaxNodeAt(compilation.SyntaxTree.SourceText.GetLocationFromLength(index, 0));
        var symbol = compilation.BoundFile.TryGetSymbol(node.Location);
        if (symbol is null)
            return Task.FromResult<Hover?>(null);

        return Task.FromResult<Hover?>(new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(GetHoverText(symbol)), 
            Range = node.Location.ToLsp()
        });
    }

    private string GetHoverText(Symbol symbol) => symbol switch
    {
        VariableSymbol variable => $"var {variable.Name}: `{variable.Type.Name}`",
        TypeSymbol type => $"type `{type.Name}`",
        BaseModuleSymbol => "base module",

        _ => ""
    };
}