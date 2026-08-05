using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;

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


    private bool AdvanceOrError(TokenKind expectedKind)
    {
        if (!_scanner.TryAdvance(expectedKind))
        {
            _diagnosticBag.ReportError(new Diagnostic.UnexpectedToken(
                _source, _scanner.Peek(0), expectedKind));
            return false;
        }

        return true;
    }

    private void AdvanceWithError(TokenKind expectedKind)
    {
        Debug.Assert(!_scanner.IsAt(expectedKind));
        
        _diagnosticBag.ReportError(new Diagnostic.UnexpectedToken(
            _source, _scanner.Peek(0), expectedKind));
        
        var expr = _scanner.Open();
        _scanner.Advance();
        _scanner.Close(expr, SyntaxKind.Error);
    }
    

    private void ReportUnexpected(SyntaxCategory expected)
        => _diagnosticBag.ReportError(new Diagnostic.UnexpectedToken(
            _source, _scanner.Peek(0), expected));



    private void Parse()
    {
        var file = _scanner.Open();

        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            ParseStmt();
        }

        _scanner.Close(file, SyntaxKind.TreeRoot);
    }

    private record BuildingNode(SyntaxKind Kind, ImmutableArray<SyntaxElement>.Builder Nodes);

    private SyntaxTree BuildTree()
    {
        Stack<BuildingNode> nodes = [];

        //TODO: Add good trivia logic here

        var allTokens = _scanner.AllTokens;
        var fullTokenIndex = 0;
        foreach (var e in _scanner.GetEvents())
        {
            switch (e.EventKind)
            {
                case ParseEventKind.Advance:
                    // Just flush all trivia here
                    while (allTokens[fullTokenIndex].Kind.IsTrivia)
                    {
                        nodes.Peek().Nodes.Add(allTokens[fullTokenIndex]);
                        fullTokenIndex++;
                    }

                    // Add the actual node
                    nodes.Peek().Nodes.Add(allTokens[fullTokenIndex]);
                    fullTokenIndex++;
                    break;

                case ParseEventKind.Open:
                    nodes.Push(new BuildingNode(e.SyntaxKind, ImmutableArray.CreateBuilder<SyntaxElement>()));
                    break;

                case ParseEventKind.Close:
                    var builtNode = nodes.Pop();
                    if (builtNode.Kind is SyntaxKind.TreeRoot)
                    {
                        Debug.Assert(nodes.Count == 0);

                        // Flush all trivia here
                        while (fullTokenIndex < allTokens.Length)
                        {
                            builtNode.Nodes.Add(allTokens[fullTokenIndex]);
                            fullTokenIndex++;
                        }

                        SyntaxTree tree;
                        if (builtNode.Nodes.Count == 0)
                        {
                            Debug.Assert(allTokens.Length == 1 && fullTokenIndex == 0 && allTokens[0].Kind is TokenKind.Eof);
                            tree = new SyntaxTree(
                                emptySpan: SourceSpan.EmptyBefore(allTokens[0].Span),
                                diagnostics: _diagnosticBag.Drain(),
                                hasError: _diagnosticBag.HasError);
                        }
                        else
                        {
                            tree = new SyntaxTree(
                                children: builtNode.Nodes.DrainToImmutable(),
                                diagnostics: _diagnosticBag.Drain(),
                                hasError: _diagnosticBag.HasError);
                        }
                        
                        return tree;
                    }

                    var node = builtNode.Nodes.Count == 0
                        ? new SyntaxNode(builtNode.Kind, emptySpan: SourceSpan.EmptyBefore(allTokens[fullTokenIndex].Span))
                        : new SyntaxNode(builtNode.Kind, builtNode.Nodes.DrainToImmutable());
                    
                    nodes.Peek().Nodes.Add(node);

                    break;
            }
        }

        throw new UnreachableException();
    }

    public static SyntaxTree Parse(SourceFileView source)
    {
        var diagnosticBag = new DiagnosticBag();
        var tokens = Lexer.Lex(source, diagnosticBag);

        var scanner = new Scanner(tokens);
        var parser = new Parser(source, scanner, diagnosticBag);
        parser.Parse();
        return parser.BuildTree();
    }


    #region Parsing

    private void ParseStmt()
    {
        var markOpen = _scanner.Open();

        if (_scanner.IsAt(OperandExprFirst))
        {
            ParseOperandExpr(null);
            AdvanceOrError(TokenKind.Semicolon);
            _scanner.Close(markOpen, SyntaxKind.ExprStmt);
            return;
        }

        // Could not find a valid stmt.
        ReportUnexpected(expected: SyntaxCategory.Stmt);
        _scanner.Advance();
        _scanner.Close(markOpen, SyntaxKind.Error);
    }

    
    
    private void ParseExpr()
    {
        ParseOperandExpr(null);
    }

    #endregion

    #region Operand Expressions

    private static readonly TokenSet OperandExprFirst = TokenSet.Of(
        TokenKind.TrueKw, TokenKind.FalseKw,
        TokenKind.NumberLiteral,
        TokenKind.I32Kw, TokenKind.I64Kw, TokenKind.F32Kw, TokenKind.F64Kw, TokenKind.StringKw, TokenKind.NoneKw,
        TokenKind.Identifier,
        TokenKind.StringStart,
        TokenKind.OpenParen,
        TokenKind.Minus, TokenKind.NotKw
    );
    private static readonly TokenSet ExprFirst = OperandExprFirst;

    private void ParseOperandExpr(LeftOperator? left)
    {
        Debug.Assert(_scanner.IsAt(OperandExprFirst));

        var lhs = ParsePrimaryOperandExpr();

        //TODO: Special case `(` (that is currently a normal infix operator)

        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            var opToken = _scanner.Peek(0);
            var opPrecedence = PrecedenceTable.TryGetInfixPrecedence(opToken.Kind);

            if (opPrecedence is null)
                break;

            var precedenceComparison = left is LeftOperator actualLeft
                ? PrecedenceTable.Compare(actualLeft.Precedence, opPrecedence.Value)
                : PrecedenceComparison.RightBindsTighter;

            if (precedenceComparison is PrecedenceComparison.LeftBindsTighter)
                break;
            
            var expr = _scanner.OpenBefore(lhs);
            _scanner.Advance();  // Advance the operator
            
            if (precedenceComparison is PrecedenceComparison.Ambiguous)
            {
                // Precedence is ambiguous.
                // Report the error and continue in this loop. This
                // will parse the ambiguous operator as right-associative
                // which is just easier to handle. Close later on will
                // close with SyntaxKind.Error, since this is not a valid
                // BinaryExpr.

                // Only Compare can return Ambiguous, and it only runs when
                // there is a left operator.
                Debug.Assert(left is not null);

                _diagnosticBag.ReportError(new Diagnostic.AmbiguousPrecedence(
                    _source, left.Value.Token, opToken));
            }

            var syntaxKind = precedenceComparison is PrecedenceComparison.Ambiguous
                ? SyntaxKind.Error
                : SyntaxKind.BinaryExpr;

            if (_scanner.IsAt(OperandExprFirst))
            {
                ParseOperandExpr(new LeftOperator(opPrecedence.Value, opToken));
                lhs = _scanner.Close(expr, syntaxKind);
            }
            else
            {
                ReportUnexpected(expected: SyntaxCategory.Expr);
                _scanner.Close(expr, syntaxKind);
                break;
            }
        }
    }

    private MarkClose ParsePrimaryOperandExpr()
    {
        Debug.Assert(_scanner.IsAt(OperandExprFirst));

        // --- String
        if (_scanner.IsAt(TokenKind.StringStart))
        {
            return ParseStringExpr();
        }
        
        var openMark = _scanner.Open();
        var token = _scanner.Advance();
        
        // --- Prefix
        if (PrecedenceTable.TryGetPrefixPrecedence(token.Kind) is Precedence prefixPrecedence)
        {
            ParseOperandExpr(new LeftOperator(prefixPrecedence, token));
            return _scanner.Close(openMark, SyntaxKind.UnaryExpr);
        }
        

        switch (token.Kind)
        {
            // --- Group
            case TokenKind.OpenParen:
                ParseExpr();
                AdvanceOrError(TokenKind.CloseParen);
                return _scanner.Close(openMark, SyntaxKind.GroupExpr);

            // --- Literals
            case TokenKind.NumberLiteral:
                return _scanner.Close(openMark, SyntaxKind.NumberLiteral);

            case TokenKind.Identifier:
                return _scanner.Close(openMark, SyntaxKind.Identifier);

            case TokenKind.I32Kw:
            case TokenKind.I64Kw:
            case TokenKind.F32Kw:
            case TokenKind.F64Kw:
            case TokenKind.NoneKw:
            case TokenKind.StringKw:
                return _scanner.Close(openMark, SyntaxKind.NativeTypeName);
            
            case TokenKind.TrueKw:
                return _scanner.Close(openMark, SyntaxKind.TrueLiteral);
            case TokenKind.FalseKw:
                return _scanner.Close(openMark, SyntaxKind.FalseLiteral);
        }

        throw new UnreachableException();
    }

    private MarkClose ParseStringExpr()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.StringStart));

        var expr = _scanner.Open();
        var stringStartToken = _scanner.AdvanceKnown(TokenKind.StringStart);

        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            switch (_scanner.Peek(0).Kind)
            {
                // --- StringText: Just add
                case TokenKind.StringText:
                    var text = _scanner.Open();
                    _scanner.AdvanceKnown(TokenKind.StringText);
                    _scanner.Close(text, SyntaxKind.StringText);
                    break;
                
                // --- StringEnd: Finish
                case TokenKind.StringEnd:
                    _scanner.AdvanceKnown(TokenKind.StringEnd);
                    return _scanner.Close(expr, SyntaxKind.StringExpr);
                
                // --- Interpolation
                case TokenKind.OpenBrace:
                    ParseStringInterpolation();
                    break;
                    
                // --- Anything else is an unclosed string
                default:
                    // We advanced at least the StringStart token, so there must be a 
                    // previous token.
                    Debug.Assert(_scanner.PreviousToken is not null);
                    
                    _diagnosticBag.ReportError(new Diagnostic.UnclosedString(_source.GetLocation(
                        SourceSpan.FromTo(stringStartToken.Span, _scanner.PreviousToken.Span))));
                    return _scanner.Close(expr, SyntaxKind.StringExpr);
            }
        }

        // Eof, but string was not closed.
        // We advanced at least the StringStart token, so there must be a 
        // previous token.
        Debug.Assert(_scanner.PreviousToken is not null);
        
        _diagnosticBag
            .ReportError(new Diagnostic.UnclosedString(_source.GetLocation(
            SourceSpan.FromTo(stringStartToken.Span, _scanner.PreviousToken.Span))));
        return _scanner.Close(expr, SyntaxKind.StringExpr);
    }

    private void ParseStringInterpolation()
    {
        // The Lexer will emit normal tokens after `{` which means that it is in
        // "syntax" mode. It is basically our choice to interpret that data. We must
        // keep what the Lexer is thinking about this string while also breaking out of
        // the interpolation in normal typing cases:
        //   "Foo {a
        //   fn Test() { }
        // Must abort after `a` and interpret fn as a normal FnDecl. More importantly: The `}`
        // that belongs visually to the FnDecl must belong there and _not_ close this interpolation.
        // So we must end this interpolation before. If there is ever a StringText/StringEnd token
        // without a StringStart before, we must stay inside the hole, however. As in:
        //   "Foo {a
        //   fn Test() { }
        //   } Bar
        // The Lexer here will emit `Bar` as StringText. Everything inside the interpolation must
        // be skipped and produce an error node.
        
        Debug.Assert(_scanner.IsAt(TokenKind.OpenBrace));
        
        var interpolationHole = _scanner.Open();
        _scanner.AdvanceKnown(TokenKind.OpenBrace);

        // --- Parse Expression, if possible
        var errorReported = false;
        if (_scanner.IsAt(ExprFirst))
        {
            ParseExpr();
        }
        else if (_scanner.IsAt(TokenKind.CloseBrace))
        {
            // Interpolation is `{ }` and we accept empty interpolations.
            // Let the case run through to the end.
        }
        else
        {
            ReportUnexpected(SyntaxCategory.Expr);
            errorReported = true;
        }

        // --- Garbage left that needs to be inside the StringInterpolation node?
        if (!_scanner.IsAt(TokenKind.CloseBrace))
        {
            // Report error and gobble up the garbage
            if (!errorReported)
            {
                _diagnosticBag.ReportError(new Diagnostic.UnexpectedToken(
                    _source, _scanner.Peek(0), TokenKind.CloseBrace));
                errorReported = true;
            }
            
            AdvanceStringInterpolationGarbage();
        }

        // --- Closing brace
        if (_scanner.IsAt(TokenKind.CloseBrace))
        {
            // Closing brace can only be a valid interpolation close,
            // if it is followed by StringText, StringEnd or another OpenBrace
            // (starts a new interpolation directly). If that is not the case, the
            // string is unclosed. The same heuristic is valid: Take it if its
            // on the same line, otherwise, leave it to the parser.
            
            // This catches typing cases like
            //    fn a() {
            //       "Hello {
            //    }
            // The last `}` will close the FnDecl.
            if (!HasNewlineBeforeNextToken() ||
                _scanner.Peek(1).Kind is TokenKind.StringText or TokenKind.StringEnd or TokenKind.OpenBrace)
            {
                _scanner.AdvanceKnown(TokenKind.CloseBrace);
            }
        }
        else
        {
            if (!errorReported)
                AdvanceOrError(TokenKind.CloseBrace);
        }

        _scanner.Close(interpolationHole, SyntaxKind.StringInterpolation);
    }

    private void AdvanceStringInterpolationGarbage()
    {
        Debug.Assert(!_scanner.IsAt(TokenKind.CloseBrace));
        
        MarkOpen? errorExpr = null;
        var braceCount = 0;

        // Calculate once before the gobble-loop and recalculate
        // only when StartStart is gobbled up. That is the only time
        // its result will change.
        var willCurrentStringBeContinued = WillCurrentStringBeContinued();
        
        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            // --- Nominal Termination on BraceClose:
            // We must keep track of inner braces, in cases like `"Foo { a {} b`,
            // we must gobble b.
            if (_scanner.IsAt(TokenKind.OpenBrace))
                braceCount++;
            else if (_scanner.IsAt(TokenKind.CloseBrace))
            {
                if (braceCount == 0)
                    break;
                braceCount--;
            }
            
            // If there is a StringText or StringEnd without StringStart before, that
            // means the Lexer thinks it's inside a string. That means, we need to gobble
            // everything in this interpolation up, so that it belong to the correct string.
            
            // Otherwise, we are free to choose what to do. As a heuristic: Same-line errors
            // shall belong to the interpolation/string, everything after a newline stops
            // the string, so the Parser can parse it normally.
            if (!willCurrentStringBeContinued && HasNewlineBeforeNextToken())
                break;
            
            // --- Gobble Gobble Gobble
            // Error has been reported by ParseStringInterpolation already,
            // so we must not report again.
            errorExpr ??= _scanner.Open();
            var advancedToken = _scanner.Advance();
            
            // Recalculate if necessary.
            if (advancedToken.Kind is TokenKind.StringStart)
                willCurrentStringBeContinued = WillCurrentStringBeContinued();
        }

        if (errorExpr is MarkOpen openedErrorExpr)
            _scanner.Close(openedErrorExpr, SyntaxKind.Error);
        return;
        
        bool WillCurrentStringBeContinued()
        {
            // Whether the string the scanner is currently inside has a continuation.
            // A continuation is a StringEnd or StringText that belongs to that string.
            // Ownership is determined by simply tracking string depth.
            
            // Note that if the scanner is currently sitting on StringStart, we will
            // regard as being outside (just one before) the string that is opened by 
            // this StringStart.
            
            // The outcome of this method only changes, when a StringStart token
            // is advanced.
            
            var depth = 0;
            for (var n = 0;; n++)
            {
                // We can and must use UnsafePeek, because our loop is bounded and
                // does not nest. It is necessary, because we might be scanning an entire file
                // ahead and normal Peek might/will trigger the infinite loop protection of scanner.
                var token = _scanner.UnsafePeek(n);
                switch (token.Kind)
                {
                    case TokenKind.StringStart:
                        depth++;
                        break;
                
                    case TokenKind.StringEnd:
                        if (depth == 0)
                            return true;
                        depth--;
                        break;
                
                    case TokenKind.StringText:
                        if (depth == 0)
                            return true;
                        break;
                
                    case TokenKind.Eof:
                        return false;
                }
            }
        }
    }

    

    private bool HasNewlineBeforeNextToken()
    {
        if (_scanner.PreviousToken is null)
            return false;
        
        var spanToNextToken = SourceSpan.Between(_scanner.PreviousToken.Span, _scanner.Peek(0).Span);
        return _source.GetText(spanToNextToken).Contains('\n');
    }

    #endregion
}