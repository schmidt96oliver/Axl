using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private record ErrorContext(DiagnosticBag Bag)
    {
        public int LastMissingOrUnexpectedTokenError { get; set; } = -1;
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
                          | TokenKind.Semicolon, ExpectedSyntax.Stmt);

                if (_scanner.IsAt(TokenKind.Semicolon))
                {
                    ReportUnexpected(ExpectedSyntax.Stmt);
                    _scanner.EatIntoErrorNode(ExpectedSyntax.Stmt);
                }
            }
        }

        _scanner.EatKnown(TokenKind.Eof);
        _scanner.Close(file, SyntaxKind.TreeRoot);
    }


    /// <summary>
    /// If scanner is not at <paramref name="anchor"/>, collects garbage into
    /// a <see cref="SyntaxKind.Error"/> node and reports <see cref="Diagnostic.UnexpectedToken"/>.
    /// Suppresses further <see cref="Diagnostic.UnexpectedToken"/> or <see cref="Diagnostic.MissingToken"/>
    /// on the position after garbage.
    /// Always leaves the scanner on <paramref name="anchor"/>.
    /// </summary>
    /// <returns><c>True</c> iff garbage was collected and an error node added.</returns>
    private bool RecoverTo(Anchor anchor, ExpectedSyntax expectedSyntax)
    {
        if (_scanner.IsAt(anchor))
            return false;

        ReportUnexpected(expectedSyntax);

        var error = _scanner.Open();
        EatGarbageIntoError(anchor);
        _scanner.CloseAsError(error, expectedSyntax, ExpectedSyntaxErrorContext.Unexpected);

        Debug.Assert(_scanner.IsAt(anchor));
        
        SuppressErrorsAtCurrentPosition();
        
        return true;
    }

    private bool RecoverTo(Anchor anchor, Func<ExpectedSyntax> expectedSyntaxCallback)
    {
        if (_scanner.IsAt(anchor))
            return false;

        var error = _scanner.Open();
        EatGarbageIntoError(anchor);
        _scanner.CloseAsError(error, expectedSyntaxCallback(), ExpectedSyntaxErrorContext.Unexpected);

        Debug.Assert(_scanner.IsAt(anchor));
        
        return true;
    }

    private void EatGarbageIntoError(Anchor anchor)
    {
        _scanner.Eat();

        foreach (var __ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(anchor))
                break;

            _scanner.Eat();
        }
    }


    /// <summary>
    /// Reports <paramref name="error"/> and suppresses multiple <see cref="Diagnostic.UnexpectedToken"/>
    /// or <see cref="Diagnostic.MissingToken"/> errors at the same
    /// position. 
    /// </summary>
    private bool ReportError(Diagnostic.Error error)
    {
        if (error is not (Diagnostic.UnexpectedToken or Diagnostic.MissingToken))
        {
            // We only suppress the two mentioned errors. Other ones may be reported freely.
            
            _errorContext.Bag.ReportError(error);
            return true;
        }
        
        if (_errorContext.LastMissingOrUnexpectedTokenError != _scanner.Position)
        {
            _errorContext.Bag.ReportError(error);
            _errorContext.LastMissingOrUnexpectedTokenError = _scanner.Position;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Suppresses further <see cref="Diagnostic.UnexpectedToken"/>
    /// or <see cref="Diagnostic.MissingToken"/> at the current position.
    /// </summary>
    private void SuppressErrorsAtCurrentPosition()
    {
        _errorContext.LastMissingOrUnexpectedTokenError = _scanner.Position;
    }

    private bool ReportUnexpected(ExpectedSyntax expected)
        => ReportUnexpected(_scanner.Peek(), expected);
    
    private bool ReportUnexpected(Token actual, ExpectedSyntax expected)
        => ReportError(new Diagnostic.UnexpectedToken(
            _source, actual, expected));

    private bool ReportMissing(ExpectedSyntax expected)
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
    /// If the scanner is on <paramref name="expectedKind"/>, eats it and returns
    /// <c>true</c>. Otherwise, creates a missing token of <paramref name="expectedKind"/>,
    /// reports <see cref="Diagnostic.MissingToken"/> and returns <c>false</c>.
    /// </summary>
    /// <param name="expectedSyntax">The <see cref="ExpectedSyntax"/> a missing token will be reported with. <c>null</c> reports
    /// <paramref name="expectedKind"/>.</param>
    /// <returns>If scanner was at <paramref name="expectedKind"/>.</returns>
    private bool EnsureToken(TokenKind expectedKind, ExpectedSyntax? expectedSyntax = null)
    {
        if (!_scanner.IsAt(expectedKind))
        {
            _scanner.Make(expectedKind, expectedSyntax);
            ReportMissing(expectedSyntax ?? expectedKind);
            return false;
        }

        _scanner.EatKnown(expectedKind);
        return true;
    }
    
    /// <summary>
    /// Applies the semicolon rule. Ensures ";" if required, otherwise eats it
    /// only if it's there.
    /// <para>
    /// Semicolon rule: ";" is omissible iff the statements owns its body and last
    /// token is "}".
    /// </para>
    /// </summary>
    /// <param name="ownsBody">Whether the consuming nodes owns its body.</param>
    private void EnsureSemicolonIfRequired(bool ownsBody)
    {
        var omissible = ownsBody && _scanner.Last?.Kind is TokenKind.CloseBrace;
        
        if (omissible && _scanner.IsAt(TokenKind.Semicolon))
            _scanner.EatKnown(TokenKind.Semicolon);
        else if (!omissible)
            EnsureToken(TokenKind.Semicolon);
    }
    
    
    private MarkClose EnsureBody(Anchor anchor)
    {
        if (_scanner.IsAt(TokenKind.RightDoubleArrow))
            return EatArm(anchor);
        
        return EnsureBlock(anchor, ExpectedSyntax.Body);
    }

    /// <summary>
    /// Eats or makes a comma-delimited list of form <c>open (item ("," item)*)? close</c>
    /// into a node of kind <paramref name="listKind"/>.
    /// </summary>
    /// <param name="ensureItem">
    ///     Eats or makes a single item. Gets an anchor that also stops on "," and
    ///     <paramref name="closeToken"/>, so a confused item hands control back here.
    /// </param>
    private MarkClose EnsureDelimitedList(Anchor anchor,
        TokenKind openToken,
        TokenKind closeToken,
        SyntaxKind listKind,
        TokenSet itemFirst,
        Func<Anchor, MarkClose> ensureItem,
        ExpectedSyntax? expectedOpenSyntax,
        ExpectedSyntax expectedItemSyntax)
    {
        var list = _scanner.Open();
        
        // --- Open Token
        if (!EnsureToken(openToken, expectedOpenSyntax))
        {
            // Make closing token and bail.
            _scanner.Make(closeToken);
            return _scanner.Close(list, listKind);
        }

        // --- Empty list?
        if (_scanner.IsAt(closeToken))
        {
            _scanner.EatKnown(closeToken);
            return _scanner.Close(list, listKind);
        }

        // --- Items
        var itemAnchor = anchor | closeToken | TokenKind.Comma;
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // --- Item
            RecoverTo(itemAnchor | itemFirst, expectedSyntax: expectedItemSyntax);
            ensureItem(itemAnchor);
            
            if (_scanner.IsAt(closeToken) ||
                _scanner.IsAt(anchor))
                break;
            
            // --- Comma
            // Recover without error first, so we can report "expected close" or
            // "expected ','" based on what followed.
            var preRecoverToken = _scanner.Peek();
            var recovered = RecoverTo(itemAnchor | itemFirst, expectedSyntaxCallback: () => 
                _scanner.IsAt(anchor | closeToken) ? closeToken : TokenKind.Comma);
            
            if (_scanner.IsAt(closeToken) ||
                _scanner.IsAt(anchor))
            {
                if (recovered)
                    ReportUnexpected(preRecoverToken, expected: closeToken);
                break;
            }

            if (recovered)
                ReportUnexpected(preRecoverToken, expected: TokenKind.Comma);
            EnsureToken(TokenKind.Comma);
        }

        // --- Close Token
        EnsureToken(closeToken);
        return _scanner.Close(list, listKind);
    }
}