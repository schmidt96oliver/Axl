using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Testing;
using AxlCompiler.Text;
using AxlLsp.Projects;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace AxlLsp.Handlers;

public class SemanticTokensHandler : SemanticTokensHandlerBase
{
    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForAxlAndTaxl,
            Legend = new SemanticTokensLegend
            {
                TokenTypes = new Container<SemanticTokenType>(
                    SemanticTokenType.Comment,
                    SemanticTokenType.String,
                    SemanticTokenType.Keyword,
                    SemanticTokenType.Decorator,
                    SemanticTokenType.Regexp,
                    SemanticTokenType.Namespace,
                    SemanticTokenType.Variable,
                    SemanticTokenType.Type,
                    SemanticTokenType.Method),
                TokenModifiers = []
            },
            Full = true
        };
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params,
        CancellationToken cancellationToken)
        => Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
    
    
    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken)
    {
        var uri = identifier.TextDocument.Uri;
        var project = ProjectStore.TryGetProject(uri);
        if (project is null)
            return Task.CompletedTask;

        var semanticTokens = project
            .GetSyntaxTrees(uri)
            .SelectMany(tree => EnumerateTokens(tree.FileSyntax))
            .SelectMany(token => Single(token, project));
        
        foreach (var (axlLocation, semanticToken) in semanticTokens)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (axlLocation.Length == 0)
                continue;
            
            var lspLocation = ProjectStore.TryMapToLsp(axlLocation);
            if (lspLocation is null || lspLocation.Uri != uri)
                continue;

            builder.Push(lspLocation.Range, (SemanticTokenType?)semanticToken);
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<Token> EnumerateTokens(SyntaxElement element)
    {
        if (element is Token token)
            yield return token;
        else if (element is SyntaxNode node)
        {
            foreach (var t in node.Children.SelectMany(EnumerateTokens))
                yield return t;
        }
    }

    
    private IEnumerable<(SourceLocation, SemanticTokenType)> Single(Token token, IProject project)
        => token.Kind switch
        {
            TokenKind.Comment when project is TaxlProject { TestFile: var testFile }
                => SingleTaxlComment(token, testFile),
            TokenKind.Comment => [(token.Location, SemanticTokenType.Comment)],
            TokenKind.Identifier => SingleIdentifier(token, project),

            TokenKind.StringStart or TokenKind.StringEnd => [(token.Location, SemanticTokenType.String)],
            TokenKind.StringText => SingleStringText(token),
            
            _ when SyntaxFacts.IsKeyword(token.Kind) => [(token.Location, SemanticTokenType.Keyword)],
            _ => []
        };

    private static IEnumerable<(SourceLocation, SemanticTokenType)> SingleTaxlComment(Token token, TestFile testFile)
    {
        var text = token.Text.ToString();
        var tokenLocationInTestFile = testFile.MapToTestFile(token.Location);

        // Fragment headlines
        if (testFile.Fragments.Any(f => f.Location.StartLine == tokenLocationInTestFile.StartLine) &&
            text.StartsWith("//---"))
        {
            yield return (tokenLocationInTestFile[.."//---".Length], SemanticTokenType.Decorator);
            yield break;
        }

        // Annotation
        var annotation = testFile.Annotations.FirstOrDefault(a => tokenLocationInTestFile.Contains(a.Prefix));
        if (annotation is not null)
        {
            yield return (annotation.Prefix, SemanticTokenType.Decorator);

            if (annotation.Kind is AnnotationKind.ExpectOutput
                && tokenLocationInTestFile.Contains(annotation.Text))
            {
                yield return (annotation.Text, SemanticTokenType.String);
            }
            
            yield break;
        }
        
        yield return (tokenLocationInTestFile, SemanticTokenType.Comment);
    }

    private static IEnumerable<(SourceLocation, SemanticTokenType)> SingleIdentifier(Token token, IProject project)
    {
        if (token.Parent?.Parent is AnnotationSyntax)
            return [(token.Parent.Parent.Location, SemanticTokenType.Decorator)];

        if (token.Kind is not TokenKind.Identifier)
            return [];
        
        var reference = project.Compilation.SemanticModel.SymbolFor(token);
        
        if (reference.Symbols.All(s => s is VariableSymbol))
            return [(token.Location, SemanticTokenType.Variable)];
        
        if (reference.Symbols.All(s => s is TypeSymbol))
            return [(token.Location, SemanticTokenType.Type)];
        
        if (reference.Symbols.All(s => s is NamespaceSymbol))
            return [(token.Location, SemanticTokenType.Namespace)];
        
        if (reference.Symbols.All(s => s is FunSymbol))
            return [(token.Location, SemanticTokenType.Method)];

        return [];
    }

    private static IEnumerable<(SourceLocation, SemanticTokenType)> SingleStringText(Token token)
    {
        // String text has escapes that are highlighted differently.
        // So we partition the string text.
        // Currently, there is no way to get valid escapes, so just
        // highlight everything after a `\`.
        
        var text = token.Text.ToString();

        var stringTokenStart = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is not '\\')
                continue;

            // String before
            if (i > stringTokenStart)
                yield return (token.Location[stringTokenStart..i], SemanticTokenType.String);

            // Escape
            var escapeLength = i + 1 < text.Length ? 2 : 1;
            yield return (token.Location[i..(i + escapeLength)], SemanticTokenType.Regexp);
            
            stringTokenStart = i + escapeLength;
            i += escapeLength - 1;  // Will be incremented on next iteration
        }

        // Push rest string
        if (text.Length > stringTokenStart)
            yield return (token.Location[stringTokenStart..text.Length], SemanticTokenType.String);
    }
}