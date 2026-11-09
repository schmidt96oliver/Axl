using System.Collections.Immutable;
using AxlCompiler;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
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


    private record Context(SourceLocation At, Compilation Compilation)
    {
        private bool _hasScope = false;
        public Scope? Scope
        {
            get
            {
                if (!_hasScope)
                {
                    field = Compilation.SemanticModel.TryScopeAt(At);
                    _hasScope = true;
                }
                return field;
            }
        }
        
        private bool _hasLeftOf = false;
        public Token? LeftOf
        {
            get
            {
                if (!_hasLeftOf)
                {
                    field = Tree.TryTokenLeftOf(At.Start);
                    _hasLeftOf = true;
                }
                return field;
            }
        }
        
        private bool _hasSignificantLeftOf = false;
        public Token? SignificantLeftOf
        {
            get
            {
                if (!_hasSignificantLeftOf)
                {
                    field = Tree.TrySignificantTokenLeftOf(At.Start);
                    _hasSignificantLeftOf = true;
                }
                return field;
            }
        }

        public SyntaxTree Tree
        {
            get
            {
                field ??= Compilation.SyntaxTreeFor(At);
                return field;
            }
        }
    }

    /// <param name="excludeReferencedCompilations">
    /// If <c>true</c>, only shows completion items that belong to <paramref name="compilation"/>.
    /// Used mainly for testing.
    /// </param>
    internal static IEnumerable<CompletionItem> GetCompletionItems(SourceLocation at, Compilation compilation, bool excludeReferencedCompilations = false)
    {
        var context = new Context(at, compilation);
        
        if (context.LeftOf is { Kind: TokenKind.Comment or TokenKind.StringStart or TokenKind.StringText } ) 
            yield break;

        var continuationTokens = Parser.ContinuationsAt(at.SourceText, at.Start);

        foreach (var kw in continuationTokens.GetKinds().Where(SyntaxFacts.IsKeyword))
        {
            if (Make(kw, context) is { } item)
                yield return item;
        }

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

    private static CompletionItem? Make(TokenKind keywordKind, Context context)
    {
        if (keywordKind is TokenKind.MutKw && !IsMutValid(context))
            return null;
        if (keywordKind is TokenKind.StaticKw && !IsStaticValid(context))
            return null;
        if (keywordKind is TokenKind.SelfKw && !IsSelfValid(context))
            return null;
        if (keywordKind is TokenKind.BreakKw or TokenKind.ContinueKw && !IsBreakOrContinueValid(context))
            return null;

        return new CompletionItem
        {
            Label = SyntaxFacts.GetText(keywordKind) ?? "??",
            Kind = CompletionItemKind.Keyword
        };
    }

    private static bool IsMutValid(Context context)
    {
        // `mut` can start a `mut fun` at declaration level
        // or `mut` argument, parameter, return or expression body on expression level.

        if (context.SignificantLeftOf is null)
        {
            // File level, where it is never valid.
            return false;
        }

        if (context.SignificantLeftOf?.Ancestors().Any(n => n is ArgListSyntax or FunDeclSyntax or StructDeclSyntax) == true)
            return true;
        
        return false;
    }

    private static bool IsStaticValid(Context context)
        => context.SignificantLeftOf?.Ancestors().OfType<StructDeclSyntax>().Any() == true;

    private static bool IsSelfValid(Context context)
        => context.Scope?.InstanceType is not null;

    private static bool IsBreakOrContinueValid(Context context)
        => context.Scope?.IsInsideLoop == true;
}