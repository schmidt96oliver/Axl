using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private readonly SourceFileView _source;
    private readonly Scanner _scanner;


    private Parser(SourceFileView source, Scanner scanner)
    {
        _source = source;
        _scanner = scanner;
    }

    
    public static SyntaxTree Parse(SourceFileView source)
    {
        var diagnosticBag = new DiagnosticBag();
        var tokens = Lexer.Lex(source, diagnosticBag);

        var scanner = new Scanner(source, tokens);
        var parser = new Parser(source, scanner);
        parser.EatRoot();
        return parser.BuildTree(tokens, diagnosticBag);
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
                // Recover to the next Stmt start, which includes Expr.
                // This is deliberately different from the anchor for EatStmt above.
                // If the parser is already confused, we recover to any position that
                // can start a new statement.
                RecoverTo(Anchor.From(FirstSet.Stmt) | fileAnchor | TokenKind.Semicolon, 
                    ExpectedSyntax.Stmt);

                if (_scanner.IsAt(TokenKind.Semicolon))
                    _scanner.EatIntoErrorAndReport(ExpectedSyntax.Stmt);
            }
        }

        _scanner.EatKnown(TokenKind.Eof);
        _scanner.Close(file, SyntaxKind.TreeRoot);
    }


    /// <summary>
    /// If scanner is not at <paramref name="anchor"/>, collects garbage into
    /// a <see cref="SyntaxKind.Error"/> node and reports <see cref="Diagnostic.UnexpectedToken"/>.
    /// Always leaves the scanner on <paramref name="anchor"/>.
    /// </summary>
    /// <returns><c>True</c> iff garbage was collected and an error node added.</returns>
    private bool RecoverTo(Anchor anchor, ExpectedSyntax expectedSyntax)
    {
        var first = _scanner.Position;
        if (RecoverToUnexplained(anchor))
        {
            Debug.Assert(first < _scanner.Position);
            _scanner.ReportUnexpectedTokensUntilHere(first, expectedSyntax);
            return true;
        }

        return false;
    }

    /// <summary>
    /// If scanner is not at <paramref name="anchor"/>, collects garbage into
    /// a <see cref="SyntaxKind.Error"/> node and reports no error.
    /// Always leaves the scanner on <paramref name="anchor"/>.
    /// </summary>
    /// <returns><c>True</c> iff garbage was collected and an error node added.</returns>
    private bool RecoverToUnexplained(Anchor anchor)
    {
        if (_scanner.IsAt(anchor))
            return false;

        var error = _scanner.Open();
        _scanner.Eat();

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(anchor))
                break;

            _scanner.Eat();
        }
        
        _scanner.Close(error, SyntaxKind.Error);
        return true;
    }
    
    
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
            _scanner.MakeAndReport(expectedKind, expectedSyntax);
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
            _scanner.MakeAndReport(closeToken);
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
            
            var firstUnexpected = _scanner.Position;
            if (RecoverToUnexplained(itemAnchor | itemFirst))
            {
                _scanner.ReportUnexpectedTokensUntilHere(firstUnexpected,
                    expectedSyntax: _scanner.IsAt(closeToken) || _scanner.IsAt(anchor)
                        ? closeToken
                        : TokenKind.Comma);
            }

            if (_scanner.IsAt(closeToken) || _scanner.IsAt(anchor))
                break;
            
            EnsureToken(TokenKind.Comma);
        }

        // --- Close Token
        EnsureToken(closeToken);
        return _scanner.Close(list, listKind);
    }
}