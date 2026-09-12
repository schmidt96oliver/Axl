using Axl.Compiler.Syntax;
using Axl.Compiler.Testing;
using Axl.Compiler.Text;
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
                    SemanticTokenType.Regexp),
                TokenModifiers = []
            },
            Full = true
        };
    }

    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken)
    {
        var compilation = DocumentStore.GetCompilation(identifier.TextDocument.Uri);
        if (compilation is null)
            return Task.CompletedTask;

        // Push diagnostics
        facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = identifier.TextDocument.Uri,
            Diagnostics = new(DiagnosticConverter.Convert(compilation.Diagnostics)
                .Concat(DocumentStore.TryGetTestFile(identifier.TextDocument.Uri) is {} testFile ?
                    DiagnosticConverter.Convert(testFile.Diagnostics) : []))
        });

        TokenizeTree(builder, compilation.SyntaxTree, DocumentStore.TryGetTestFile(identifier.TextDocument.Uri));


        return Task.CompletedTask;
    }

    private void TokenizeTree(SemanticTokensBuilder builder, SyntaxTree tree, TestFile? testFile)
    {
        foreach (var token in EnumerateTokens(tree.FileSyntax))
        {
            if (token.FullRange.Length == 0)
                continue;
            if (token.FullRange.Start >= tree.SourceText.Length)
                continue;

            var location = SourceLocation.From(tree.SourceText, token.FullRange);
            switch (token.Kind)
            {
                case TokenKind.Comment:
                {
                    if (testFile is not null)
                        TokenizeTaxlComment(token, testFile, location.StartLine, location.StartColumn, builder);
                    else
                    {
                        builder.Push(location.StartLine, location.StartColumn, token.FullRange.Length,
                            (SemanticTokenType?)SemanticTokenType.Comment);
                    }

                    break;
                }

                case TokenKind.StringStart:
                case TokenKind.StringEnd:
                    builder.Push(location.StartLine, location.StartColumn, token.FullRange.Length,
                        (SemanticTokenType?)SemanticTokenType.String);
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
                            builder.Push(location.StartLine,
                                @char: location.StartColumn + stringTokenStart,
                                length: i - stringTokenStart,
                                (SemanticTokenType?)SemanticTokenType.String);
                        }

                        // Push escape
                        builder.Push(location.StartLine,
                            @char: location.StartColumn + i,
                            length: i + 1 < text.Length ? 2 : 1,
                            (SemanticTokenType?)SemanticTokenType.Regexp);

                        if (i + 1 < text.Length)
                            i++;
                        stringTokenStart = i + 1;
                    }

                    // Push rest string
                    if (text.Length > stringTokenStart)
                    {
                        builder.Push(location.StartLine,
                            @char: location.StartColumn + stringTokenStart,
                            length: text.Length - stringTokenStart,
                            (SemanticTokenType?)SemanticTokenType.String);
                    }

                    break;
                }

                case TokenKind.AndKw:
                case TokenKind.BoolKw:
                case TokenKind.BreakKw:
                case TokenKind.ContinueKw:
                case TokenKind.ElseKw:
                case TokenKind.FalseKw:
                case TokenKind.FnKw:
                case TokenKind.IfKw:
                case TokenKind.LoopKw:
                case TokenKind.ModuleKw:
                case TokenKind.NativeKw:
                case TokenKind.NeverKw:
                case TokenKind.NoneKw:
                case TokenKind.NotKw:
                case TokenKind.OrKw:
                case TokenKind.PrivateKw:
                case TokenKind.PublicKw:
                case TokenKind.ReturnKw:
                case TokenKind.StringKw:
                case TokenKind.TrueKw:
                case TokenKind.UsingKw:
                case TokenKind.VarKw:
                case TokenKind.F32Kw:
                case TokenKind.F64Kw:
                case TokenKind.I32Kw:
                case TokenKind.I64Kw:
                    builder.Push(location.StartLine, location.StartColumn, token.FullRange.Length,
                        (SemanticTokenType?)SemanticTokenType.Keyword);
                    break;
            }
        }
    }

    private void TokenizeTaxlComment(Token commentToken, TestFile testFile, int startLine, int startColumn, SemanticTokensBuilder builder)
    {
        var text = commentToken.Text;
        
        // --- Fragment headlines
        if (text.StartsWith("//---") || text.StartsWith("//==="))
        {
            builder.Push(startLine, startColumn, length: Math.Min(5, commentToken.FullRange.Length),
                (SemanticTokenType?)SemanticTokenType.Decorator);
            return;
        }

        // --- Output fragments
        var isInOutput = testFile.GetFragmentAt(commentToken.Location).IsOutput;
        if (isInOutput)
        {
            // `//` is now a decorator
            builder.Push(startLine, startColumn, 2,
                (SemanticTokenType?)SemanticTokenType.Decorator);

            // Everything thereafter is string
            if (commentToken.FullRange.Length > 2)
            {
                builder.Push(startLine, startColumn + 2, commentToken.FullRange.Length - 2,
                    (SemanticTokenType?)SemanticTokenType.String);
            }

            return;
        }
        
        // Directives and annotations
        var decoratorLength = GetTaxlCommentDecoratorLength(commentToken.FullRange, testFile);
        if (decoratorLength <= 0)
        {
            // Entire length is just a comment
            builder.Push(startLine, startColumn, commentToken.FullRange.Length,
                (SemanticTokenType?)SemanticTokenType.Comment);
        }
        
        builder.Push(startLine, startColumn, decoratorLength,
            (SemanticTokenType?)SemanticTokenType.Decorator);
    }
    
    private int GetTaxlCommentDecoratorLength(SourceRange commentRange, TestFile testFile)
    {
        // Directive?
        if (testFile.Directive?.Location.Range == commentRange)
            return commentRange.Length;

        // An annotation?
        var annotation = testFile.Fragments
            .SelectMany(fragment => fragment.Annotations)
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