using AxlCompiler.Syntax;
using AxlLsp.Projects;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace AxlLsp.Handlers;

public class GotoDefinitionHandler : DefinitionHandlerBase
{
    /// <inheritdoc />
    protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForAxlAndTaxl
    };

    /// <inheritdoc />
    public override Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;
        if (ProjectStore.TryMapToAxl(request) is not { } axlLocation)
            return Task.FromResult<LocationOrLocationLinks?>(null);

        var syntaxContext = SyntaxContext.At(axlLocation);
        if (syntaxContext.TouchingWord is not { Kind: TokenKind.Identifier })
            return Task.FromResult<LocationOrLocationLinks?>(null);

        if (ProjectStore.TryMapToLsp(syntaxContext.TouchingWord.Location) is not { } originalSelectionLocation 
            || originalSelectionLocation.Uri != uri)
        {
            return Task.FromResult<LocationOrLocationLinks?>(null);
        }
        
        var symbols = axlLocation.Compilation.SemanticModel.SymbolFor(syntaxContext.TouchingWord).Symbols;
        var links = new List<LocationOrLocationLink>();

        foreach (var symbol in symbols)
        foreach (var decl in symbol.DeclaringSyntax)
        {
            var nameSyntax = symbol.NameSyntax.FirstOrDefault(name => decl.Location.Contains(name.Location));
            if (nameSyntax is null) continue;

            var targetLocation = ProjectStore.TryMapToLsp(decl.Location);
            var targetSelectionLocation = ProjectStore.TryMapToLsp(nameSyntax.Location);

            if (targetLocation is null || targetSelectionLocation is null ||
                targetLocation.Uri != targetSelectionLocation.Uri)
                continue;

            links.Add(new LocationLink
            {
                OriginSelectionRange = originalSelectionLocation.Range,
                TargetUri = targetLocation.Uri,
                TargetRange = targetLocation.Range,
                TargetSelectionRange = targetSelectionLocation.Range,
            });
        }

        return Task.FromResult(new LocationOrLocationLinks(links))!;
    }
}