using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private readonly SourceFileView _source;
    private readonly DiagnosticBag _diagnosticBag;

    private readonly Scanner _scanner;


    private Parser(SourceFileView source, Scanner scanner, DiagnosticBag diagnosticBag)
    {
        _source = source;
        _scanner = scanner;
        _diagnosticBag = diagnosticBag;
    }

    public static SyntaxTree Parse(SourceFileView source)
    {
        var diagnosticBag = new DiagnosticBag();
        var tokens = Lexer.Lex(source, diagnosticBag);

        var scanner = new Scanner(tokens);
        var parser = new Parser(source, scanner, diagnosticBag);
        parser.EatRoot();
        return parser.BuildTree();
    }

    private void EatRoot()
    {
        //TODO: Distinguish script and module file
        var file = _scanner.Open();

        // Stmt can start from Expr or Decl. Recover only from
        // Decl, because Expr would be too permissive.
        var fileAnchor = Anchor.From(FirstSet.MemberDecl)
            | TokenKind.UsingKw | TokenKind.ModuleKw;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(FirstSet.Stmt))
                EatStmt(fileAnchor | TokenKind.Semicolon);
            else if (_scanner.IsAt(TokenKind.ModuleKw))
                EatModuleDecl();
            else if (_scanner.IsAt(TokenKind.UsingKw))
                EatUsingDecl();
            else if (_scanner.IsAt(FirstSet.MemberDecl))
                EatMemberDecl(fileAnchor);
            else
            {
                ReportUnexpected(expected: SyntaxCategory.Stmt);

                // Recover to the next Stmt start, which includes Expr.
                // This is deliberately different from the anchor for EatStmt above.
                // If the parser is already confused, we recover to any position that
                // can start a new statement.
                RecoverTo(Anchor.From(FirstSet.Stmt)
                          | fileAnchor
                          | TokenKind.Semicolon, expectedKind: null);

                if (_scanner.IsAt(TokenKind.Semicolon))
                {
                    ReportUnexpected(expected: SyntaxCategory.Stmt);
                    var error = _scanner.Open();
                    _scanner.EatToken(TokenKind.Semicolon);
                    _scanner.Close(error, SyntaxKind.Error);
                }
            }
        }

        _scanner.EatToken(TokenKind.Eof);
        _scanner.Close(file, SyntaxKind.TreeRoot);
    }


    /// <summary>
    /// If scanner is not at <paramref name="anchor"/>, collects garbage into
    /// a <see cref="SyntaxKind.Error"/> node and reports <see cref="Diagnostic.UnexpectedToken"/>.
    /// Always leaves the scanner on <paramref name="anchor"/>.
    /// </summary>
    /// <returns><c>True</c> iff garbage was collected and an error node added.</returns>
    private bool RecoverTo(Anchor anchor, TokenKind? expectedKind)
    {
        if (_scanner.IsAt(anchor))
            return false;

        if (expectedKind is TokenKind kind)
            ReportUnexpected(kind);

        EatGarbageIntoError(anchor);
        return true;
    }

    private bool RecoverTo(Anchor anchor, SyntaxCategory? expectedCategory)
    {
        if (_scanner.IsAt(anchor))
            return false;

        if (expectedCategory is SyntaxCategory category)
            ReportUnexpected(category);

        EatGarbageIntoError(anchor);
        return true;
    }

    private void EatGarbageIntoError(Anchor anchor)
    {
        var error = _scanner.Open();
        _scanner.EatToken();

        foreach (var __ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(anchor))
                break;

            _scanner.EatToken();
        }

        _scanner.Close(error, SyntaxKind.Error);

        Debug.Assert(_scanner.IsAt(anchor));
    }


    private void ReportUnexpected(SyntaxCategory expected)
        => _diagnosticBag.ReportError(new Diagnostic.UnexpectedToken(
            _source, _scanner.Peek(), expected));
    private void ReportUnexpected(TokenKind expected)
        => _diagnosticBag.ReportError(new Diagnostic.UnexpectedToken(
            _source, _scanner.Peek(), expected));

    private void ReportMissing(TokenKind expected)
        => _diagnosticBag.ReportError(new Diagnostic.MissingToken(
            _source,
            previous: _scanner.Last,
            next: _scanner.Peek(),
            expected));
    private void ReportMissing(SyntaxCategory expected)
        => _diagnosticBag.ReportError(new Diagnostic.MissingToken(
            _source,
            previous: _scanner.Last,
            next: _scanner.Peek(),
            expected));


    private bool HasNewlineBeforeNextToken()
    {
        var spanToNextToken = _scanner.Last is null
            ? _source.SpanFromTo(0, _scanner.Peek().Span.End)
            : SourceSpan.Between(_scanner.Last.Span, _scanner.Peek().Span);
        return _source.GetText(spanToNextToken).Contains('\n');
    }
}