using System.Collections.Immutable;
using AxlCompiler;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlCompiler.Text;
using AxlLsp.Projects;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace AxlLsp.Handlers;

public sealed class CompletionHandler : CompletionHandlerBase
{
    /// <inheritdoc />
    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForAxlAndTaxl,
    };

    /// <inheritdoc />
    public override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var axlLocation = ProjectStore.TryMapToAxl(request.TextDocument.Uri, request.Position);
        if (axlLocation is null)
            return Task.FromResult(new CompletionList([]));

        return Task.FromResult(new CompletionList(GetCompletionItems(axlLocation.Location, axlLocation.Compilation)));
    }

    /// <inheritdoc />
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    } 


    /// <param name="excludeReferencedCompilations">
    /// If <c>true</c>, only shows completion items that belong to <paramref name="compilation"/>.
    /// Used mainly for testing.
    /// </param>
    internal static IEnumerable<CompletionItem> GetCompletionItems(SourceLocation at, Compilation compilation, bool excludeReferencedCompilations = false)
    {
        var continuationTokens = Parser.ContinuationsAt(at.SourceText, at.Start);

        foreach (var kw in continuationTokens.GetKinds().Where(SyntaxFacts.IsKeyword))
            yield return Make(kw);

        if (continuationTokens.Contains(TokenKind.Identifier))
        {
            var symbolsByName = compilation.SemanticModel
                .VisibleSymbolsAtUnmapped(at)
                .Where(s => !excludeReferencedCompilations || !compilation.ReferencedCompilations.Contains(s.Compilation))
                .GroupBy(s => s.Name);
            foreach (var symbolGrp in symbolsByName)
                yield return Make(symbolGrp.Key, [.. symbolGrp]);
        }
    }
    

    private static CompletionItem Make(string name, ImmutableArray<Symbol> symbols) => new()
    {
        Label = name,
        Kind = CompletionItemKind.Function,
        //TODO: Improve CompletionItem
        
        // LabelDetails = symbols[0] is FunSymbol fun ?
        //     new CompletionItemLabelDetails
        // {
        //     Detail = "(" +string.Join(", ", fun.Parameters.Select(p => $"{p.Name}: {p.Type.Name}")) + ")",
        //     Description = fun.ReturnType.Name,
        // } : null,
        // Documentation = "DOC COMMENT YAAY",
        // InsertText = symbols[0].Name + "(${0:a}, ${1:b})",
        // InsertTextFormat = InsertTextFormat.Snippet
    };

    private static CompletionItem Make(TokenKind keywordKind) => new()
    {
        Label = SyntaxFacts.GetText(keywordKind) ?? "??",
        Kind = CompletionItemKind.Keyword
    };

    
}