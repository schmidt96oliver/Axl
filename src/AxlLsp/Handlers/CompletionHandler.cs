using System.Collections.Immutable;
using AxlCompiler;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Text;
using AxlLsp.Projects;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace AxlLsp.Handlers;

public sealed class CompletionHandler(ILanguageServerFacade facade) : CompletionHandlerBase
{
    private bool IsRider => facade.Client.ClientSettings.ClientInfo?.Name.Contains("Rider") == true;
    
    /// <inheritdoc />
    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForAxlAndTaxl,
    };

    /// <inheritdoc />
    public override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var axlLocation = ProjectStore.TryMapToAxl(request);
        if (axlLocation is null)
            return Task.FromResult(new CompletionList([]));

        var items = GetCompletionItems(axlLocation.Location, axlLocation.Compilation, isRider: IsRider);
        return Task.FromResult(new CompletionList(items));
    }

    /// <inheritdoc />
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    }


    internal static IEnumerable<CompletionItem> GetStructInitFieldItems(BoundTypeInitError typeInitError, SyntaxContext context, bool isRider)
    {
        // Show only those candidates that have all the fields
        // that have been written. The binder does not filter
        // those out.
        var filledFieldNames = typeInitError.BoundArguments
            .Select(arg => arg.NameSyntax?.Text.ToString() ?? "")
            .Where(fieldName => fieldName is not "" &&
                context.TouchingWord?.Text.SequenceEqual(fieldName) != true)
            .ToHashSet();

        var names = typeInitError.Candidates.SelectMany(RemainingFields).Distinct();
        
        return names.Select(fieldName => new CompletionItem
        {
            Label = $"{fieldName} = ",
            Kind = isRider ? CompletionItemKind.File : CompletionItemKind.Field,
        });

        IEnumerable<string> RemainingFields(TypeSymbol type)
        {
            var fields = type.Members.OfType<FieldSymbol>().ToImmutableArray();
            if (filledFieldNames.Any(name => !fields.Any(f => f.Name == name)))
                return [];

            return fields.Where(f => !filledFieldNames.Contains(f.Name))
                .Select(f => f.Name);
        }
    }
    
    /// <param name="excludeReferencedCompilations">
    /// If <c>true</c>, only shows completion items that belong to <paramref name="compilation"/>.
    /// Used mainly for testing.
    /// </param>
    internal static IEnumerable<CompletionItem> GetCompletionItems(SourceLocation at, Compilation compilation, bool excludeReferencedCompilations = false, bool isRider = false)
    {
        var context = SyntaxContext.At(at, compilation);
        
        // Nothing to show
        if (context.IsInsideComment || context.IsInsideString) 
            yield break;

        // Args for struct initializers
        if (context.TargetToken?.Parent is ArgListSyntax argListSyntax)
        {
            var callSyntax = argListSyntax.Ancestors().OfType<CallExprSyntax>().First();
            if (compilation.SemanticModel.BoundNodeFor(callSyntax) is BoundTypeInitError error)
            {
                foreach (var item in GetStructInitFieldItems(error, context, isRider))
                    yield return item;
                yield break;
            }
        }
        
        // Normal completion
        foreach (var kw in context.Continuations.GetKinds().Where(SyntaxFacts.IsKeyword))
        {
            if (MaybeMake(kw, context, isRider) is { } item)
                yield return item;
        }

        if (context.Continuations.Contains(TokenKind.Identifier))
        {
            var symbolsByName = compilation.SemanticModel.VisibleSymbolsAtUnmapped(at)
                .Where(s => !excludeReferencedCompilations || !compilation.ReferencedCompilations.Contains(s.Compilation))
                .GroupBy(s => s.Name);
            foreach (var symbolGrp in symbolsByName)
            {
                if (MaybeMake(symbolGrp.Key, [.. symbolGrp], context, isRider) is { } item)
                    yield return item;
            }
        }
    }

    
    // !!! CompletionItemKind is picked based on what symbol it maps in LSP4IJ for Rider.
    // !!! These do _not_ map to the correct symbol type.
    
    
    private static CompletionItem? MaybeMake(string name, ImmutableArray<Symbol> symbols, SyntaxContext context, bool isRider)
    {
        if (symbols.All(s => s is FunSymbol))
            return MakeFunItem(name, [.. symbols.Cast<FunSymbol>()]);
        if (symbols.All(s => s is TypeSymbol))
            return MakeTypeItem(name, [.. symbols.Cast<TypeSymbol>()], context, isRider);

        if (symbols.Length != 1) return null;

        return symbols[0] switch
        {
            FieldSymbol field => MakeFieldItem(name, field),
            LocalSymbol local => MakeLocalItem(name, local, isRider),
            ConstSymbol constSymbol => MakeConstItem(name, constSymbol),
            
            NamespaceSymbol => MakeNamespaceItem(name, isRider),
            
            TypeSymbol => null,
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
                InsertText = $"{name}($0)",
                InsertTextFormat = InsertTextFormat.Snippet,
                
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
            InsertText = $"{name}($0)",
            InsertTextFormat = InsertTextFormat.Snippet,
            
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
    private static CompletionItem MakeTypeItem(string name, ImmutableArray<TypeSymbol> symbols, SyntaxContext context, bool isRider)
    {
        // Insert brackets only for type positions, because in expression position
        // the user might want to use inference.
        var insertBrackets = symbols.All(t => t.TypeParameters.Length > 0) &&
                             context.TargetToken?.Ancestors().Any(p =>
                                 p is TypeAnnotationClauseSyntax or ReturnTypeAnnotationSyntax) == true;

        var kind = isRider ? CompletionItemKind.Class :
            symbols.All(s => s is NamedTypeSymbol { Definition: ClassDefinition }) ? CompletionItemKind.Class :
            symbols.All(s => s is NamedTypeSymbol { Definition: StructDefinition }) ? CompletionItemKind.Struct :
            symbols.All(s => s is TypeParameterSymbol) ? CompletionItemKind.TypeParameter : CompletionItemKind.Text;
        
        if (symbols.Length > 1)
        {
            return new CompletionItem
            {
                Label = name,
                Kind = kind,
                InsertText = insertBrackets ? $"{name}[$0]" : name,
                InsertTextFormat = InsertTextFormat.Snippet,

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
            Kind = kind,
            InsertText = insertBrackets ? $"{name}[$0]" : name,
            InsertTextFormat = InsertTextFormat.Snippet,

            LabelDetails = new CompletionItemLabelDetails
            {
                Detail = symbols[0].TypeParameters.Length > 0 ? $"[{ string.Join(", ", symbols[0].TypeParameters.Select(t => t.Name)) }]" : null,
                Description = null,
            },
        };
    }

    private static CompletionItem MakeNamespaceItem(string name, bool isRider) => new()
    {
        Label = name,
        Kind = isRider ? CompletionItemKind.Struct : CompletionItemKind.Module,
    };
    
    private static CompletionItem MakeLocalItem(string name, LocalSymbol symbol, bool isRider) => new()
    {
        Label = name,
        Kind = symbol is ParameterSymbol && isRider ? CompletionItemKind.TypeParameter : CompletionItemKind.Variable,
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
    private static CompletionItem MakeConstItem(string name, ConstSymbol symbol) => new()
    {
        Label = name,
        Kind = CompletionItemKind.Constant,
        LabelDetails = new CompletionItemLabelDetails
        {
            Detail = null,
            Description = $"{symbol.Type.Name}",
        },
    };
    
    private static CompletionItem? MaybeMake(TokenKind keywordKind, SyntaxContext context, bool isRider)
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
            Kind = isRider ? CompletionItemKind.Event : CompletionItemKind.Keyword
        };
    }

    private static bool IsMutValid(SyntaxContext context)
    {
        // `mut` can start a `mut fun` at declaration level
        // or `mut` argument, parameter, return or expression body on expression level.

        if (context.TargetToken is null)
        {
            // File level, where it is never valid.
            return false;
        }

        var contextNode = context.TargetToken?.Ancestors().FirstOrDefault(n => n is
            ArgListSyntax or ParamListSyntax or ReturnTypeAnnotationSyntax or
            FunBodySyntax { IsExpressionBodied: true } or ReturnExprSyntax or
            StructOrClassDeclSyntax);

        return contextNode is StructOrClassDeclSyntax { StructOrClassKw.Kind: TokenKind.StructKw }
            or not StructOrClassDeclSyntax and not null;
    }

    private static bool IsStaticValid(SyntaxContext context)
        => context.TargetToken?.Ancestors().OfType<StructOrClassDeclSyntax>().Any() == true;

    private static bool IsSelfValid(SyntaxContext context)
    {
        var scope = context.Compilation.SemanticModel.TryScopeAtUnmapped(context.SyntaxTree, context.Caret);
        return scope?.InstanceType is not null;
    }

    private static bool IsBreakOrContinueValid(SyntaxContext context)
    {
        var scope = context.Compilation.SemanticModel.TryScopeAtUnmapped(context.SyntaxTree, context.Caret);
        return scope?.IsInsideLoop == true;
    }
}