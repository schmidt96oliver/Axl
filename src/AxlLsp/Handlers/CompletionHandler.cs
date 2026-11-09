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

        var items = GetCompletionItems(axlLocation.Location, axlLocation.Compilation);
        return Task.FromResult(new CompletionList(items));
    }

    /// <inheritdoc />
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    }


    private record Context(Compilation Compilation, SyntaxContext SyntaxContext)
    {
        private bool _hasScope = false;
        public Scope? Scope
        {
            get
            {
                if (!_hasScope)
                {
                    field = Compilation.SemanticModel.TryScopeAt(SyntaxContext.SyntaxTree, SyntaxContext.Caret);
                    _hasScope = true;
                }
                return field;
            }
        }

        public ImmutableArray<Symbol> VisibleSymbolsUnmapped
        {
            get
            {
                if (field.IsDefault)
                    field = Compilation.SemanticModel.VisibleSymbolsAtUnmapped(SyntaxContext.SyntaxTree, SyntaxContext.Caret);
                return field;
            }
        }

        private bool _hasContinuations = false;
        public TokenSet Continuations
        {
            get
            {
                if (!_hasContinuations)
                {
                    field = Parser.ContinuationsAt(SyntaxContext.SyntaxTree.SourceText, SyntaxContext.Caret);
                    _hasContinuations = true;
                }

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
        var context = new Context(compilation, SyntaxContext.At(at.Start, compilation.SyntaxTreeFor(at.SourceText)));
        
        if (context.SyntaxContext.IsInsideComment || context.SyntaxContext.IsInsideString) 
            yield break;

        foreach (var kw in context.Continuations.GetKinds().Where(SyntaxFacts.IsKeyword))
        {
            if (MaybeMake(kw, context) is { } item)
                yield return item;
        }

        if (context.Continuations.Contains(TokenKind.Identifier))
        {
            var symbolsByName = context.VisibleSymbolsUnmapped
                .Where(s => !excludeReferencedCompilations || !compilation.ReferencedCompilations.Contains(s.Compilation))
                .GroupBy(s => s.Name);
            foreach (var symbolGrp in symbolsByName)
            {
                if (MaybeMake(symbolGrp.Key, [.. symbolGrp]) is { } item)
                    yield return item;
            }
        }
    }

    
    // !!! CompletionItemKind is picked based on what symbol it maps in LSP4IJ for Rider.
    // !!! These do _not_ map to the correct symbol type.
    
    
    private static CompletionItem? MaybeMake(string name, ImmutableArray<Symbol> symbols)
    {
        if (symbols.All(s => s is FunSymbol))
            return MakeFunItem(name, [.. symbols.Cast<FunSymbol>()]);
        if (symbols.All(s => s is TypeSymbol))
            return MakeTypeItem(name, [.. symbols.Cast<TypeSymbol>()]);

        if (symbols.Length != 1) return null;

        return symbols[0] switch
        {
            FieldSymbol @field => MakeFieldItem(name, @field),
            VariableSymbol var => MakeVariableItem(name, var),
            
            NamespaceSymbol ns => MakeNamespaceItem(name, ns),
            
            TypeSymbol type => null,
            FunSymbol => null,
        };
    }
        
    private static CompletionItem MakeFunItem(string name, ImmutableArray<FunSymbol> symbols)
    {
        if (symbols.Length > 1)
        {
            return new CompletionItem
            {
                Label = name,
                Kind = CompletionItemKind.Function,

                LabelDetails = new CompletionItemLabelDetails
                {
                    Detail = $"+{symbols.Length - 1} overload" + (symbols.Length != 2 ? "s" : ""),
                    Description = null,
                },
            };
        }

        return new CompletionItem()
        {
            Label = name,
            Kind = symbols[0].IsStatic ? CompletionItemKind.Function : CompletionItemKind.Method,

            LabelDetails = new CompletionItemLabelDetails
            {
                Detail = TypeParamText(symbols[0]) + ParamListText(symbols[0]),
                Description = symbols[0].ReturnType.Name,
            },
        };

        string TypeParamText(FunSymbol symbol) => symbol.TypeParameters.Length > 0
            ? $"[{string.Join(", ", symbol.TypeParameters.Select(t => t.Name))}]"
            : "";
        string ParamListText(FunSymbol symbol) =>
            $"({string.Join(", ", symbol.Parameters.Select(ParamText))})";

        string ParamText(ParameterSymbol param) => param.IsMut
            ? $"{param.Name}: mut {param.Type.Name}"
            : $"{param.Name}: {param.Type.Name}";
    }
    private static CompletionItem MakeTypeItem(string name, ImmutableArray<TypeSymbol> symbols)
    {
        if (symbols.Length > 1)
        {
            return new CompletionItem
            {
                Label = name,
                Kind = CompletionItemKind.Class,

                LabelDetails = new CompletionItemLabelDetails
                {
                    Detail = $"+{ symbols.Length - 1 } variant" + (symbols.Length != 2 ? "s" : ""),
                    Description = null,
                },
            };
        }
        
        return new CompletionItem()
        {
            Label = name,
            Kind = CompletionItemKind.Class,

            LabelDetails = new CompletionItemLabelDetails
            {
                Detail = symbols[0].TypeParameters.Length > 0 ? $"[{ string.Join(", ", symbols[0].TypeParameters.Select(t => t.Name)) }]" : null,
                Description = null,
            },
        };
    }

    private static CompletionItem MakeNamespaceItem(string name, NamespaceSymbol symbol) => new()
    {
        Label = name,
        Kind = CompletionItemKind.Struct,
    };
    
    private static CompletionItem MakeVariableItem(string name, VariableSymbol symbol) => new()
    {
        Label = name,
        Kind = symbol is ParameterSymbol ? CompletionItemKind.TypeParameter : CompletionItemKind.Variable,
        LabelDetails = new CompletionItemLabelDetails
        {
            Detail = null,
            Description = symbol is ParameterSymbol { IsMut: true }
                ? $"mut {symbol.Type.Name}"
                : $"{symbol.Type.Name}",
        },
    };
    
    private static CompletionItem MakeFieldItem(string name, FieldSymbol symbol) => new()
    {
        Label = name,
        Kind = CompletionItemKind.Field,
        LabelDetails = new CompletionItemLabelDetails
        {
            Detail = null,
            Description = $"{symbol.Type.Name}",
        },
    };
    
    private static CompletionItem? MaybeMake(TokenKind keywordKind, Context context)
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
            Kind = CompletionItemKind.Event
        };
    }

    private static bool IsMutValid(Context context)
    {
        // `mut` can start a `mut fun` at declaration level
        // or `mut` argument, parameter, return or expression body on expression level.

        if (context.SyntaxContext.TargetToken is null)
        {
            // File level, where it is never valid.
            return false;
        }

        if (context.SyntaxContext.TargetToken?.Ancestors().Any(n => n is ArgListSyntax or FunDeclSyntax or StructDeclSyntax) == true)
            return true;
        
        return false;
    }

    private static bool IsStaticValid(Context context)
        => context.SyntaxContext.TargetToken?.Ancestors().OfType<StructDeclSyntax>().Any() == true;

    private static bool IsSelfValid(Context context)
        => context.Scope?.InstanceType is not null;

    private static bool IsBreakOrContinueValid(Context context)
        => context.Scope?.IsInsideLoop == true;
}