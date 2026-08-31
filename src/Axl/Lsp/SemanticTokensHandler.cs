using Axl.Compiler.Syntax;
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
        
        PushDiagnostics(identifier.TextDocument.Uri, compilation.Diagnostics);

        foreach (var tree in compilation.SyntaxTrees)
        {
            TokenizeTree(builder, tree);
        }

        return Task.CompletedTask;
    }

    private void PushDiagnostics(DocumentUri uri, IEnumerable<Axl.Compiler.Diagnostics.Diagnostic> diagnostics)
    {
        facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = DiagnosticConverter.Convert(diagnostics)
        });
    }
    
    private void TokenizeTree(SemanticTokensBuilder builder, SyntaxTree tree)
    {
        var isInOutput = false;
        foreach (var token in EnumerateTokens(tree.FileSyntax))
        {
            if (token.FullSpan.Length == 0)
                continue;
            if (token.FullSpan.First >= tree.Source.File.Text.Length)
                continue;

            var startLinePos = tree.Source.File.GetLinePosition(token.FullSpan.First);
            switch (token.Kind)
            {
                case TokenKind.Comment:
                {
                    var text = tree.Source.File.GetText(token.FullSpan);
                    if (text.StartsWith("//@") || text.StartsWith("//~"))
                    {
                        var length = 3;
                        while (length < text.Length &&
                               (char.IsAsciiLetterOrDigit(text[length]) || text[length] is '_' or '-'))
                        {
                            length++;
                        }

                        builder.Push(startLinePos.Line, startLinePos.Column, length,
                            (SemanticTokenType?)SemanticTokenType.Decorator);

                        // See if there is a comment
                        var commentStart = text[2..].IndexOf("//") + 2;
                        if (commentStart > 2)
                        {
                            builder.Push(startLinePos.Line, startLinePos.Column + commentStart,
                                text.Length - commentStart,
                                (SemanticTokenType?)SemanticTokenType.Comment);
                        }
                    }
                    else if (text.StartsWith("//---") || text.StartsWith("//==="))
                    {
                        var length = 5;
                        while (length < text.Length && text[length] is '-' or '=')
                            length++;

                        builder.Push(startLinePos.Line, startLinePos.Column, length,
                            (SemanticTokenType?)SemanticTokenType.Decorator);

                        isInOutput = text.StartsWith("//===");
                    }
                    else if (!isInOutput)
                    {
                        // Entire line is a comment
                        builder.Push(startLinePos.Line, startLinePos.Column, token.FullSpan.Length,
                            (SemanticTokenType?)SemanticTokenType.Comment);
                    }
                    else if (isInOutput)
                    {
                        // `//` is now a decorator
                        builder.Push(startLinePos.Line, startLinePos.Column, 2,
                            (SemanticTokenType?)SemanticTokenType.Decorator);

                        // Everything thereafter is string
                        if (text.Length > 2)
                        {
                            builder.Push(startLinePos.Line, startLinePos.Column + 2, text.Length - 2,
                                (SemanticTokenType?)SemanticTokenType.String);
                        }
                    }

                    break;
                }

                case TokenKind.StringStart:
                case TokenKind.StringEnd:
                    builder.Push(startLinePos.Line, startLinePos.Column, token.FullSpan.Length,
                        (SemanticTokenType?)SemanticTokenType.String);
                    break;

                case TokenKind.StringText:
                {
                    // Partition the string text into escape and non-escape
                    var text = tree.Source.File.GetText(token.FullSpan);

                    var stringTokenStart = 0;
                    for (var i = 0; i < text.Length; i++)
                    {
                        if (text[i] is not '\\')
                            continue;

                        // Push string text before
                        if (i > stringTokenStart)
                        {
                            builder.Push(startLinePos.Line,
                                @char: startLinePos.Column + stringTokenStart,
                                length: i - stringTokenStart,
                                (SemanticTokenType?)SemanticTokenType.String);
                        }

                        // Push escape
                        builder.Push(startLinePos.Line,
                            @char: startLinePos.Column + i,
                            length: i + 1 < text.Length ? 2 : 1,
                            (SemanticTokenType?)SemanticTokenType.Regexp);

                        if (i + 1 < text.Length)
                            i++;
                        stringTokenStart = i + 1;
                    }

                    // Push rest string
                    if (text.Length > stringTokenStart)
                    {
                        builder.Push(startLinePos.Line,
                            @char: startLinePos.Column + stringTokenStart,
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
                    builder.Push(startLinePos.Line, startLinePos.Column, token.FullSpan.Length,
                        (SemanticTokenType?)SemanticTokenType.Keyword);
                    break;
            }
        }
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