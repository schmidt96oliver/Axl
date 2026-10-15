using Axl.Compiler;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Testing;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Axl.Lsp;

public class SemanticTokensHandler(ILanguageServerFacade facade) : SemanticTokensHandlerBase
{
    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(Lsp.LanguageId, Lsp.TestLanguageId),
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
                    SemanticTokenType.Type),
                TokenModifiers = []
            },
            Full = true
        };
    }

    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken)
    {
        var uri = identifier.TextDocument.Uri;
        var project = ProjectStore.TryGetProject(uri);
        if (project is null)
            return Task.CompletedTask;

        // Push diagnostics
        facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = project.GetDiagnostics(uri)
        });

        TokenizeFile(builder, uri, project);

        return Task.CompletedTask;
    }

    private void TokenizeFile(SemanticTokensBuilder builder, DocumentUri uri, IProject project)
    {
        foreach (var tree in project.GetSyntaxTrees(uri))
        foreach (var token in EnumerateTokens(tree.FileSyntax))
        {
            if (token.FullRange.Length == 0)
                continue;
            if (token.FullRange.Start >= tree.SourceText.Length)
                continue;

            var lspLocation = project.TryMapToLsp(token.Location);
            switch (token.Kind)
            {
                case TokenKind.Comment:
                {
                    if (project is TaxlProject taxlProject)
                        TokenizeTaxlComment(token, taxlProject.TestFile, lspLocation, builder);
                    else
                        builder.Push(lspLocation.Range, (SemanticTokenType?)SemanticTokenType.Comment);

                    break;
                }
                
                case TokenKind.Identifier:
                    var semanticTokenType = project.Compilation.GetBoundScript(tree).SemanticSideTable.TryGetSymbol(token.Location) switch
                    {
                        TypeSymbol => SemanticTokenType.Type,
                        VariableSymbol => SemanticTokenType.Variable,
                        NamespaceOrTypeSymbol => SemanticTokenType.Namespace,
                        _ => (SemanticTokenType?)null
                    };
                    
                    if (semanticTokenType is not null)
                        builder.Push(lspLocation.Range, semanticTokenType);
                    break;

                case TokenKind.StringStart:
                case TokenKind.StringEnd:
                    builder.Push(lspLocation.Range, (SemanticTokenType?)SemanticTokenType.String);
                    break;

                case TokenKind.StringText:
                {
                    // Partition the string text into escape and non-escape
                    var text = tree.SourceText.GetText(token.FullRange);

                    var stringTokenStart = 0;
                    for (var i = 0; i < text.Length; i++)
                    {
                        if (text[i] is not '\\')
                            continue;

                        // Push string text before
                        if (i > stringTokenStart)
                        {
                            builder.Push(lspLocation.Range.Start.Line,
                                @char: lspLocation.Range.Start.Character + stringTokenStart,
                                length: i - stringTokenStart,
                                (SemanticTokenType?)SemanticTokenType.String);
                        }

                        // Push escape
                        builder.Push(lspLocation.Range.Start.Line,
                            @char: lspLocation.Range.Start.Character + i,
                            length: i + 1 < text.Length ? 2 : 1,
                            (SemanticTokenType?)SemanticTokenType.Regexp);

                        if (i + 1 < text.Length)
                            i++;
                        stringTokenStart = i + 1;
                    }

                    // Push rest string
                    if (text.Length > stringTokenStart)
                    {
                        builder.Push(lspLocation.Range.Start.Line,
                            @char: lspLocation.Range.Start.Character + stringTokenStart,
                            length: text.Length - stringTokenStart,
                            (SemanticTokenType?)SemanticTokenType.String);
                    }

                    break;
                }

                default:
                    if (SyntaxFacts.IsKeyword(token.Kind))
                        builder.Push(lspLocation.Range, (SemanticTokenType?)SemanticTokenType.Keyword);
                    break;
            }
        }
    }

    private void TokenizeTaxlComment(Token commentToken, TestFile testFile, Location lspLocation, SemanticTokensBuilder builder)
    {
        var text = commentToken.Text;
        
        // --- Fragment headlines
        if (text.StartsWith("//---"))
        {
            builder.Push(lspLocation.Range.Start.Line, lspLocation.Range.Start.Character, length: Math.Min(5, commentToken.FullRange.Length),
                (SemanticTokenType?)SemanticTokenType.Decorator);
            return;
        }

        // Directives and annotations
        var decoratorLength = GetTaxlCommentDecoratorLength(commentToken.FullRange, testFile, out var restIsString);
        if (decoratorLength <= 0)
        {
            // Entire length is just a comment
            builder.Push(lspLocation.Range, (SemanticTokenType?)SemanticTokenType.Comment);
        }
        
        builder.Push(lspLocation.Range.Start.Line, lspLocation.Range.Start.Character, decoratorLength,
            (SemanticTokenType?)SemanticTokenType.Decorator);
        
        if (restIsString && decoratorLength < commentToken.FullRange.Length)
        {
            var restStart = lspLocation.Range.Start.Character + decoratorLength;
            var restLength = commentToken.FullRange.Length - decoratorLength;
            builder.Push(lspLocation.Range.Start.Line, restStart, restLength,
                (SemanticTokenType?)SemanticTokenType.String);
        }
    }
    
    private int GetTaxlCommentDecoratorLength(SourceRange commentRange, TestFile testFile, out bool restIsString)
    {
        restIsString = false;
        
        // Directive?
        if (commentRange.Contains(testFile.Directive.Location.Range))
            return commentRange.Length;
        
        // Expectation?
        if (testFile.Expectation is not null && testFile.Expectation.Location.Range.Contains(commentRange))
        {
            SourceLocation? prefixLocation = testFile.Expectation.PrefixLocations.FirstOrDefault(l => commentRange.Contains(l.Range));
            if (prefixLocation is SourceLocation loc)
            {
                restIsString = true;
                return loc.Length;
            }
            
            return 0;
        }

        // An annotation?
        var annotation = testFile.Annotations
            .FirstOrDefault(annotation => annotation.FullLocation.Range == commentRange);
        if (annotation is not null)
            return annotation.PrefixLocation.Length;
        
        // Nothing
        return 0;
    }
    
    private IEnumerable<Token> EnumerateTokens(SyntaxElement element)
    {
        if (element is Token token)
            yield return token;
        else if (element is SyntaxNode node)
        {
            foreach (var t in node.Children.SelectMany(EnumerateTokens))
                yield return t;
        }
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
    }
}