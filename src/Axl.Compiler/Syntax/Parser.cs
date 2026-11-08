using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Text;

// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private readonly SourceText _sourceText;
    private readonly Scanner _scanner;


    private Parser(SourceText sourceText, Scanner scanner)
    {
        _sourceText = sourceText;
        _scanner = scanner;
    }

    
    public static SyntaxTree Parse(SourceText sourceText)
    {
        var lexerDiagnostics = new DiagnosticBag();
        var tokens = Lexer.Lex(sourceText, lexerDiagnostics);

        var scanner = new Scanner(sourceText, tokens);
        var parser = new Parser(sourceText, scanner);
        parser.EatRoot();

        var parserDiagnostics = new DiagnosticBag();
        var rootNode = parser.BuildTree(tokens, parserDiagnostics);
        
        var tree = new SyntaxTree(
            fileSyntax: rootNode,
            sourceText,
            diagnostics: [..lexerDiagnostics.Drain(), ..parserDiagnostics.Drain()],
            hasError: lexerDiagnostics.HasError);

        rootNode.Tree = tree;
        
        return tree;
    }

    public static TokenSet ContinuationsAt(SourceText sourceText, int position)
    {
        var tokens = InsertContinuationProbe(Lexer.Lex(sourceText, DiagnosticBag.Discard), position);

        var scanner = new Scanner(sourceText, tokens);
        var parser = new Parser(sourceText, scanner);
        parser.EatRoot();

        return scanner.ContinuationsAtProbe;
    }

    // Internal for testing.
    internal static ImmutableArray<Token> InsertContinuationProbe(ImmutableArray<Token> tokens, int position)
    {
        Debug.Assert(tokens.Length >= 1, "There must be at least EOF!");
        
        // Insert the probe before the token it is at.
        // E.g. "12 ab|cd" needs to insert at "12 |abcd",
        // while "12 |abcd" stays that way.

        var lo = 0;
        var hi = tokens.Length - 1;

        while (lo <= hi)
        {
            var pivot = (lo + hi) / 2;
            var token = tokens[pivot];
            if (token.FullRange.Start <= position)
                lo = pivot + 1;
            else
                hi = pivot - 1;
        }

        // If the probe is at the end of an identifier or keyword,
        // snap it before that token, so that completion sees the
        // keyword as well. E.g. "12 abc|" => "12 |abc".
        var probeIndex = hi;
        var isAtEndOfIdToken = hi > 0 &&
                                (tokens[hi - 1].Kind is TokenKind.Identifier ||
                                 SyntaxFacts.IsKeyword(tokens[hi - 1].Kind)) &&
                                position == tokens[hi - 1].FullRange.End;
        if (isAtEndOfIdToken)
            probeIndex--;

        return tokens.Insert(probeIndex, Token.MakeSimple(
            SourceRange.EmptyBefore(tokens[probeIndex].FullRange), TokenKind.ContinuationProbe));
    }
    
    
    private void EatRoot()
    {
        var file = _scanner.Open();

        // Don't anchor on namespace or using, because these are expected at
        // the start of a file, and it makes sense to eat them as garbage
        // inside each grammar.
        var fileAnchor = Anchor.Forced | TokenKind.FunKw | TokenKind.StructKw | FirstSet.NonExprStmt;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(TokenKind.MutKw) && _scanner.IsAt(TokenKind.FunKw, skip: 1))
                EatMember(fileAnchor, allowedAfterHeader: FirstSet.FunDeclAfterHeader);
            else if (_scanner.IsAt(FirstSet.Stmt))
                EatStmt(fileAnchor | TokenKind.Semicolon);
            else if (_scanner.IsAt(FirstSet.StructDecl | FirstSet.FunDecl))
                EatMember(fileAnchor, allowedAfterHeader: FirstSet.StructDeclAfterHeader | FirstSet.FunDeclAfterHeader);
            else if (_scanner.IsAt(TokenKind.NamespaceKw))
                EatNamespaceDecl();
            else if (_scanner.IsAt(TokenKind.UsingKw))
                EatUsingDirective(fileAnchor);
            else
            {
                // Recover to the next Stmt start, which includes Expr.
                // This is deliberately different from the anchor for EatStmt above.
                // If the parser is already confused, we recover to any position that
                // can start a new statement.
                var recoverAnchor = Anchor.From(FirstSet.Stmt)
                                    | TokenKind.NamespaceKw
                                    | TokenKind.UsingKw
                                    | TokenKind.FunKw
                                    | TokenKind.StructKw
                                    | TokenKind.Semicolon;
                var recovered = RecoverToAndReport(recoverAnchor, ExpectedSyntax.Stmt);

                if (_scanner.IsAt(TokenKind.Semicolon, isGrammarCheck: false))
                {
                    // If the parser has recovered before, there already is an error
                    // and we eat the ';' silently. Otherwise, report the error.
                    
                    if (recovered)
                        _scanner.EatInto(SyntaxKind.Garbage);
                    else
                        _scanner.EatIntoGarbageAndReport(ExpectedSyntax.Stmt);
                }
            }
        }

        // There might be an early stop, in case the continuation probe is finished.
        // So EOF might not be present.
        
        _scanner.EatIfPresent(TokenKind.Eof);
        _scanner.Close(file, SyntaxKind.File);
    }

    private MarkClose EatUsingDirective(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.UsingKw));

        var usingDirective = _scanner.Open(); 

        _scanner.EatKnown(TokenKind.UsingKw);
        EnsureTypeName(anchor);
        EnsureToken(TokenKind.Semicolon);
        
        return _scanner.Close(usingDirective, SyntaxKind.UsingDirective);
    }

    /// <summary>
    /// If scanner is not at <paramref name="anchor"/>, collects garbage into
    /// a <see cref="SyntaxKind.Garbage"/> node and reports <see cref="Diagnostic.UnexpectedToken"/>.
    /// Always leaves the scanner on <paramref name="anchor"/>.
    /// </summary>
    /// <returns><c>True</c> iff garbage was collected and an error node added.</returns>
    private bool RecoverToAndReport(Anchor anchor, ExpectedSyntax expectedSyntax)
    {
        var first = _scanner.Position;
        if (RecoverTo(anchor))
        {
            Debug.Assert(first < _scanner.Position);
            _scanner.ReportUnexpectedTokensUntilHere(first, expectedSyntax);
            return true;
        }

        return false;
    }

    /// <summary>
    /// If scanner is not at <paramref name="anchor"/>, collects garbage into
    /// a <see cref="SyntaxKind.Garbage"/> node and reports no error.
    /// Always leaves the scanner on <paramref name="anchor"/>.
    /// </summary>
    /// <returns><c>True</c> iff garbage was collected and an error node added.</returns>
    private bool RecoverTo(Anchor anchor)
    {
        if (_scanner.IsAt(anchor, isGrammarCheck: false))
            return false;

        var error = _scanner.Open();

        var braceCount = 0;
        
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            var garbageToken = _scanner.Eat();
            
            // Balance '{ }'
            switch (garbageToken.Kind)
            {
                case TokenKind.OpenBrace: 
                    braceCount++; break;
                case TokenKind.CloseBrace when braceCount > 0: 
                    braceCount--; break;
            }

            // Only allow breaking off, if braces are balanced.
            if (_scanner.IsAt(anchor, isGrammarCheck: false) && braceCount == 0)
                break;
        }
        
        _scanner.Close(error, SyntaxKind.Garbage);
        return true;
    }
    
    
    private bool HasNewlineBeforeNextToken()
    {
        var spanToNextToken = _scanner.Last is null
            ? SourceRange.FromBounds(0, _scanner.Peek().FullRange.End)
            : SourceRange.Between(_scanner.Last.FullRange, _scanner.Peek().FullRange);
        return _sourceText.GetText(spanToNextToken).Contains('\n');
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
        bool allowEmpty,
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

        if (allowEmpty)
        {
            // --- Empty list?
            if (_scanner.IsAt(closeToken))
            {
                _scanner.EatKnown(closeToken);
                return _scanner.Close(list, listKind);
            }

            // --- Already at anchor?
            // Special-case here, so we report "expected close" instead of
            // "expected item" in cases like `Call( `.
            if (!_scanner.IsAt(itemFirst) && _scanner.IsAt(anchor, isGrammarCheck: false))
            {
                EnsureToken(closeToken);
                return _scanner.Close(list, listKind);
            }
        }

        // --- Items
        var itemAnchor = anchor | closeToken | TokenKind.Comma;
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // --- Item
            if (!_scanner.IsAt(TokenKind.ContinuationProbe))
                RecoverToAndReport(itemAnchor | itemFirst, expectedSyntax: expectedItemSyntax);
            ensureItem(itemAnchor);
            
            // --- Comma
            // Recover without error first, so we can report "expected close" or
            // "expected ','" based on what followed.
            
            var firstUnexpected = _scanner.Position;
            if (RecoverTo(itemAnchor | itemFirst))
            {
                _scanner.ReportUnexpectedTokensUntilHere(firstUnexpected,
                    expectedSyntax: _scanner.IsAt(closeToken, isGrammarCheck: false) || _scanner.IsAt(anchor,
                        isGrammarCheck: false)
                        ? closeToken
                        : TokenKind.Comma);
            }

            // Comma might be in the anchor set, so handle it before.
            if (_scanner.IsAt(TokenKind.Comma))
            {
                _scanner.Eat();
                continue;
            }
            
            if (_scanner.IsAt(closeToken) || _scanner.IsAt(anchor, isGrammarCheck: false))
                break;
            
            EnsureToken(TokenKind.Comma);
        }

        // --- Close Token
        EnsureToken(closeToken);
        return _scanner.Close(list, listKind);
    }
}