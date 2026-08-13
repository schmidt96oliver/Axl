using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private record ErrorContext(DiagnosticBag Bag)
    {
        /// <summary>
        /// Position <see cref="Scanner"/> was at, when the last error was reported.
        /// </summary>
        public int LastErrorPosition { get; set; } = -1;
    }
    
    private readonly SourceFileView _source;
    private readonly ErrorContext _errorContext;

    private readonly Scanner _scanner;


    private Parser(SourceFileView source, Scanner scanner, DiagnosticBag diagnosticBag)
    {
        _source = source;
        _scanner = scanner;
        _errorContext = new ErrorContext(diagnosticBag);
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
            | TokenKind.UsingKw | TokenKind.ModuleKw
            | TokenKind.VarKw;

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
                ReportUnexpected(ExpectedSyntax.Stmt);

                // Recover to the next Stmt start, which includes Expr.
                // This is deliberately different from the anchor for EatStmt above.
                // If the parser is already confused, we recover to any position that
                // can start a new statement.
                RecoverTo(Anchor.From(FirstSet.Stmt)
                          | fileAnchor
                          | TokenKind.Semicolon, expected: null);

                if (_scanner.IsAt(TokenKind.Semicolon))
                {
                    ReportUnexpected(ExpectedSyntax.Stmt);
                    _scanner.EatTokenIntoNode(SyntaxKind.Error);
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
    private bool RecoverTo(Anchor anchor, ExpectedSyntax? expected)
    {
        if (_scanner.IsAt(anchor))
            return false;

        if (expected is ExpectedSyntax actualExpected)
            ReportUnexpected(actualExpected);

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


    /// <summary>
    /// Reports <paramref name="error"/> and suppresses multiple <see cref="Diagnostic.UnexpectedToken"/>
    /// or <see cref="Diagnostic.MissingToken"/> errors at the same
    /// position. 
    /// </summary>
    private void ReportError(Diagnostic.Error error)
    {
        if (error is not (Diagnostic.UnexpectedToken or Diagnostic.MissingToken))
        {
            // Don't update the LastErrorPosition, because otherwise an InvalidOperatorChaining
            // error could suppress a missing ";" error, which we don't want. Error that
            // are not unexpected or missing token should always be reported and not influence
            // reporting of those.
            
            _errorContext.Bag.ReportError(error);
            return;
        }
        
        if (_errorContext.LastErrorPosition != _scanner.Position)
        {
            _errorContext.Bag.ReportError(error);
            _errorContext.LastErrorPosition = _scanner.Position;
        }
    }
    
    private void ReportUnexpected(ExpectedSyntax expected)
        => ReportError(new Diagnostic.UnexpectedToken(
            _source, _scanner.Peek(), expected));

    private void ReportMissing(ExpectedSyntax expected)
        => ReportError(new Diagnostic.MissingToken(
            _source,
            Previous: _scanner.Last,
            Next: _scanner.Peek(),
            expected));


    private bool HasNewlineBeforeNextToken()
    {
        var spanToNextToken = _scanner.Last is null
            ? _source.SpanFromTo(0, _scanner.Peek().Span.End)
            : SourceSpan.Between(_scanner.Last.Span, _scanner.Peek().Span);
        return _source.GetText(spanToNextToken).Contains('\n');
    }

    /// <summary>
    /// Eats <c>open (item ("," item)*)? close</c> into a <paramref name="listKind"/> node.
    /// </summary>
    /// <param name="eatItem">
    /// Eats a single item. Gets an anchor that also stops on "," and
    /// <paramref name="closeToken"/>, so a confused item hands control back here.
    /// </param>
    private MarkClose EatDelimitedList(
        Anchor anchor,
        TokenKind openToken,
        TokenKind closeToken,
        SyntaxKind listKind,
        TokenSet itemFirst,
        ExpectedSyntax expectedItemSyntax,
        Func<Anchor, MarkClose> eatItem)
    {
        Debug.Assert(_scanner.IsAt(openToken));

        var list = _scanner.Open();
        _scanner.EatToken(openToken);

        // --- Special-case the empty list
        if (_scanner.IsAt(closeToken))
        {
            _scanner.EatToken(closeToken);
            return _scanner.Close(list, listKind);
        }

        // --- Expect items
        var itemAnchor = anchor | closeToken | TokenKind.Comma;
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // Each iteration expects another item.
            if (_scanner.IsAt(itemFirst))
                eatItem(itemAnchor);
            else
            {
                if (_scanner.IsAt(anchor | TokenKind.Comma | closeToken | itemFirst))
                    ReportMissing(expectedItemSyntax);
                else
                    ReportUnexpected(expectedItemSyntax);
            }

            // After item expected: closing or ','
            if (_scanner.IsAt(TokenKind.Comma))
                _scanner.EatToken(TokenKind.Comma);
            else if (_scanner.IsAt(closeToken))
                break; 
            
            // Next item without comma?
            else if (_scanner.IsAt(itemFirst))
            {
                // Another item without comma
                ReportMissing(TokenKind.Comma);
            }
            
            // Anchor?
            else if (_scanner.IsAt(anchor))
            {
                ReportMissing(closeToken);
                break;
            }
            
            // Confused?
            else
            {
                RecoverTo(itemAnchor | itemFirst, expected: TokenKind.Comma);

                if (_scanner.IsAt(TokenKind.Comma))
                    _scanner.EatToken(TokenKind.Comma);
                else if (_scanner.IsAt(closeToken))
                    break;
            }
        }

        // --- Expect close
        ExpectToken(closeToken);
        return _scanner.Close(list, listKind);
    }

    /// <summary>
    /// Applies the semicolon rule. Expects ";" if required, otherwise eats it
    /// only if it's there.
    /// <para>
    /// Semicolon rule: ";" is omissible iff the statements owns its body and last
    /// token is "}".
    /// </para>
    /// </summary>
    /// <param name="ownsBody">Whether the consuming nodes owns its body.</param>
    private void EatSemicolonIfRequired(bool ownsBody)
    {
        var omissible = ownsBody && _scanner.Last?.Kind is TokenKind.CloseBrace;
        
        if (omissible && _scanner.IsAt(TokenKind.Semicolon))
            _scanner.EatToken(TokenKind.Semicolon);
        else if (!omissible)
            ExpectToken(TokenKind.Semicolon);
    }
}