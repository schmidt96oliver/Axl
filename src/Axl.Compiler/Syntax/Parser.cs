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

    
    #region Helpers

    private bool AdvanceOrError(TokenKind expectedKind)
    {
        if (!_scanner.TryAdvance(expectedKind))
        {
            ReportMissing(expectedKind);
            return false;
        }

        return true;
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
            previous: _scanner.PreviousToken, 
            next: _scanner.Peek(), 
            expected));
    private void ReportMissing(SyntaxCategory expected)
        => _diagnosticBag.ReportError(new Diagnostic.MissingToken(
            _source, 
            previous: _scanner.PreviousToken, 
            next: _scanner.Peek(), 
            expected));

    
    private bool HasNewlineBeforeNextToken()
    {
        var spanToNextToken = _scanner.PreviousToken is null
            ? _source.SpanFromTo(0, _scanner.Peek().Span.End)
            : SourceSpan.Between(_scanner.PreviousToken.Span, _scanner.Peek().Span);
        return _source.GetText(spanToNextToken).Contains('\n');
    }
    
    #endregion
    
    #region Tree Building
    
    private record BuildingNode(SyntaxKind Kind, ImmutableArray<SyntaxElement>.Builder Nodes);

    private SyntaxTree BuildTree()
    {
        //TODO: Add good trivia logic here

        Stack<BuildingNode> nodes = [];
        var tokens = _scanner.AllTokens;
        var nextToken = 0;
        foreach (var e in _scanner.GetEvents())
        {
            switch (e.EventKind)
            {
                case ParseEventKind.Open:
                    Debug.Assert(e.SyntaxKind is not null, "Unclosed node");
                    
                    nodes.Push(new BuildingNode(e.SyntaxKind.Value, ImmutableArray.CreateBuilder<SyntaxElement>()));
                    break;
                
                case ParseEventKind.Advance:
                    // Flush all trivia here
                    while (tokens[nextToken].Kind.IsTrivia)
                    {
                        nodes.Peek().Nodes.Add(tokens[nextToken]);
                        nextToken++;
                    }

                    // Add the actual node
                    nodes.Peek().Nodes.Add(tokens[nextToken]);
                    nextToken++;
                    break;

                case ParseEventKind.Close:
                    var builtNode = nodes.Pop();
                    var isRoot = builtNode.Kind is SyntaxKind.TreeRoot;
                    if (isRoot)
                    {
                        Debug.Assert(nodes.Count == 0, "TreeRoot was not the root.");

                        // Flush the remaining trivia and the Eof token here. This also
                        // guarantees the root has at least one child.
                        while (nextToken < tokens.Length)
                        {
                            builtNode.Nodes.Add(tokens[nextToken]);
                            nextToken++;
                        }
                    }

                    var node = new SyntaxNode(builtNode.Kind, builtNode.Nodes.DrainToImmutable());

                    if (isRoot)
                    {
                        return new SyntaxTree(
                            root: node,
                            diagnostics: _diagnosticBag.Drain(),
                            hasError: _diagnosticBag.HasError);
                    }

                    nodes.Peek().Nodes.Add(node);
                    break;
            }
        }

        throw new UnreachableException();
    }

    #endregion
    
    
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

    private void Parse()
    {
        var file = _scanner.Open();

        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            ParseStmt();
        }

        _scanner.Close(file, SyntaxKind.TreeRoot);
    }
    
    private void ParseStmt()
    {
        var markOpen = _scanner.Open();

        if (_scanner.IsAt(FirstSet.OperandExpr))
        {
            // Checked here, because the diagnostic below is better than
            // the generic "expected an expression".
            var parsed = TryParseOperandExpr(left: null);
            Debug.Assert(parsed);

            AdvanceOrError(TokenKind.Semicolon);
            _scanner.Close(markOpen, SyntaxKind.ExprStmt);
            return;
        }

        // Could not find a valid stmt.
        ReportUnexpected(expected: SyntaxCategory.Stmt);
        _scanner.Advance();
        _scanner.Close(markOpen, SyntaxKind.Error);
    }

    /// <summary>
    /// Reports a missing expression and returns <c>false</c>, if the
    /// scanner is not at an expression.
    /// </summary>
    private bool TryParseExpr()
    {
        return TryParseOperandExpr(left: null);
    }

    #endregion

    #region Operand Expressions

    /// <summary>
    /// Reports a missing expression and returns <c>false</c>, if the
    /// scanner is not at an expression. Nothing is consumed in that case.
    /// Callers that want to emit a better diagnostic must check
    /// <see cref="FirstSet.OperandExpr"/> themselves beforehand.
    /// </summary>
    private bool TryParseOperandExpr(LeftOperator? left)
    {
        // Report missing, if not at a valid first OperandExpr
        // token.
        if (!_scanner.IsAt(FirstSet.OperandExpr))
        {
            ReportMissing(expected: SyntaxCategory.Expr);
            return false;
        }

        // --- Head
        var lhs = ParseOperandExprHead();

        // --- Pratt loop
        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            // --- Read operator and check precedence
            var opToken = _scanner.Peek();
            var opPrecedence = PrecedenceTable.TryGetInfixPrecedence(opToken.Kind);

            if (opPrecedence is null)
                break;

            var precedenceComparison = left is LeftOperator actualLeft
                ? PrecedenceTable.Compare(actualLeft.Precedence, opPrecedence.Value)
                : PrecedenceComparison.RightBindsTighter;

            // --- Ambiguous?
            if (precedenceComparison is PrecedenceComparison.Ambiguous)
            {
                // For the precedence to be ambiguous, we must be on a tail,
                // so we never drop an ambiguous operator.
                Debug.Assert(left is not null);

                // Ambiguous operators belong to the enclosing loop, which will
                // collect all ambiguous operators in ParseOperandExprTail.
                break;
                
            }
            
            // --- Correct binding power?
            if (precedenceComparison is PrecedenceComparison.LeftBindsTighter)
            {
                // The left side bind tighter, so we stop here and let the enclosing
                // loop handle that operator.
                break;
            }

            // --- Open expression, advance operator
            var expr = _scanner.OpenBefore(lhs);
            
            //TODO: Special case `(` and '.' (which parse specially)
            _scanner.Advance();

            // --- Parse tail
            var wasNonAmbiguousTail = ParseOperandExprTail(new LeftOperator(opPrecedence.Value, opToken));
            
            // --- Close expression
            lhs = _scanner.Close(expr, wasNonAmbiguousTail ? SyntaxKind.BinaryExpr : SyntaxKind.Error);
        }

        return true;
    }

    private MarkClose ParseOperandExprHead()
    {
        Debug.Assert(_scanner.IsAt(FirstSet.OperandExpr));
        
        // --- String
        if (_scanner.IsAt(TokenKind.StringStart))
            return ParseStringExpr();
        
        // For everything else, we can advance a token
        // already and then switch on it.
        var openMark = _scanner.Open();
        var token = _scanner.Advance();
        
        // --- Prefix Operator
        if (PrecedenceTable.TryGetPrefixPrecedence(token.Kind) is Precedence prefixPrecedence)
        {
            var wasNonAmbiguousTail = ParseOperandExprTail(new LeftOperator(prefixPrecedence, token));
            return _scanner.Close(openMark, wasNonAmbiguousTail ? SyntaxKind.UnaryExpr : SyntaxKind.Error);
        }
        
        // Switch on everything else
        switch (token.Kind)
        {
            // --- Group
            case TokenKind.OpenParen:
                TryParseExpr();
                AdvanceOrError(TokenKind.CloseParen);
                return _scanner.Close(openMark, SyntaxKind.GroupExpr);

            // --- Literals
            case TokenKind.NumberLiteral:
                return _scanner.Close(openMark, SyntaxKind.NumberLiteral);

            case TokenKind.Identifier:
                return _scanner.Close(openMark, SyntaxKind.Identifier);
            
            case TokenKind.TrueKw:
                return _scanner.Close(openMark, SyntaxKind.TrueLiteral);
            case TokenKind.FalseKw:
                return _scanner.Close(openMark, SyntaxKind.FalseLiteral);
            
            // --- Native Types
            case TokenKind.I32Kw:
            case TokenKind.I64Kw:
            case TokenKind.F32Kw:
            case TokenKind.F64Kw:
            case TokenKind.NoneKw:
            case TokenKind.StringKw:
                return _scanner.Close(openMark, SyntaxKind.NativeTypeName);
        }

        throw new UnreachableException();
    }
    
    /// <summary>
    /// Parses the right side of any OperandExpr.
    /// Handles ambiguous operators gracefully:
    /// It collects all chained ambiguous operators and reports one
    /// diagnostic for them.
    /// </summary>
    /// <returns>
    /// <c>False</c> iff an ambiguous chain was advanced.
    /// In contrast to <see cref="TryParseOperandExpr"/> this also returns
    /// <c>True</c> even if no expression was parsed.
    /// </returns>
    private bool ParseOperandExprTail(LeftOperator left)
    {
        ImmutableArray<Token>.Builder? ambiguousOperators = null;

        var previousOperatorPrecedence = left.Precedence;

        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            // Parse OperandExpr
            if (!TryParseOperandExpr(left))
                break;

            // Peek and check if next token is
            // an operator with ambiguous comparison.
            var nextOpToken = _scanner.Peek();
            if (PrecedenceTable.TryGetInfixPrecedence(nextOpToken.Kind) 
                is not Precedence nextOpPrecedence)
            {
                break;
            }

            if (PrecedenceTable.Compare(previousOperatorPrecedence, nextOpPrecedence) 
                is not PrecedenceComparison.Ambiguous)
            {
                break;
            }

            previousOperatorPrecedence = nextOpPrecedence;

            // Next operator is ambiguous.
            // Advance it and parse another expression.
            _scanner.Advance();

            // If ambiguous operators was empty before, we need to add
            // the operator that was passed in, because that was already
            // ambiguous.
            if (ambiguousOperators is null)
            {
                ambiguousOperators = ImmutableArray.CreateBuilder<Token>(); 
                ambiguousOperators.Add(left.Token);
            }

            ambiguousOperators.Add(nextOpToken);
        }

        if (ambiguousOperators is not null)
        {
            //Report combined diagnostic.
            _diagnosticBag.ReportError(new Diagnostic.InvalidOperatorChaining(_source,
                ambiguousOperators.DrainToImmutable()));

            return false;
        }

        return true;
    }

    
    private MarkClose ParseStringExpr()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.StringStart));

        var expr = _scanner.Open();
        _scanner.AdvanceKnown(TokenKind.StringStart);

        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            switch (_scanner.Peek().Kind)
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
                    ReportUnclosedString();
                    return _scanner.Close(expr, SyntaxKind.StringExpr);
            }
        }

        // Eof, but string was not closed.
        // We advanced at least the StringStart token, so there must be a 
        // previous token.
        Debug.Assert(_scanner.PreviousToken is not null);
        
        ReportUnclosedString();
        return _scanner.Close(expr, SyntaxKind.StringExpr);

        void ReportUnclosedString()
        {
            // There must be at least a StringStart token which has been advanced.
            Debug.Assert(_scanner.PreviousToken is not null);
            
            _diagnosticBag.ReportError(new Diagnostic.UnclosedString(
                _source,
                LastToken: _scanner.PreviousToken));
        }
    }

    private void ParseStringInterpolation()
    {
        // Grammar is "{" Expr? "}" and allows for multi-line Expr inside this interpolation.
        
        // We need to handle common typing scenarios gracefully and resiliently here. The tokens
        // that come from the Lexer do not directly distinguish between inside-interpolation tokens
        // and normal-code tokens. So we need to reconstruct that based on heuristics in case the
        // interpolation is not valid.
        
        // We start by parsing the expression, which is eager:
        //   "Foo {a.
        //   1 + 2;
        //   fn Test() { }
        // Will result in `a.1 + 2` being parsed inside the interpolation. The blast radius is
        // small: Only one expression. The FnDecl thereafter will be outside of this interpolation.
        
        // The interesting case is what happens after the expression is parsed and the interpolation
        // is not (yet) closed. Parser will go into gobble-mode which has special rules to determine
        // if it gobbles tokens into one error node inside the interpolation or if it leaves the
        // interpolation.
        
        // Finally the closing "}" is special cased as well to catch common typing-cases like
        //    fn Foo()
        //    {
        //        "Bar { 1
        //    }
        // Where we want the last "}" to close the function body instead of the interpolation.
        
        Debug.Assert(_scanner.IsAt(TokenKind.OpenBrace));
        
        // --- Advance `{`
        var interpolationHole = _scanner.Open();
        _scanner.AdvanceKnown(TokenKind.OpenBrace);

        // --- Parse Expression or empty interpolation
        var errorReported = false;

        if (_scanner.IsAt(TokenKind.CloseBrace))
        {
            // Interpolation is `{ }`.
            // Just fall through. Method will see `}` and
            // apply the resilience logic.
        }
        else
        {
            // If scanner is not at an expression, a
            // MissingToken diagnostic is reported. Thus
            // we update errorReported flag.
            var parsed = TryParseExpr();
            errorReported = !parsed;
        }

        // --- Garbage left after the expression?
        if (!_scanner.IsAt(TokenKind.CloseBrace))
        {
            if (!errorReported)
            {
                // Ambiguous between missing and unexpected, because
                // whatever came here may or may not be gobbled later.
                // Missing is the less offensive option, so we go with that.
                ReportMissing(TokenKind.CloseBrace);
                errorReported = true;
            }
            
            // Gobble up any garbage that belongs to this interpolation.
            AdvanceStringInterpolationGarbage();
        }

        // --- Closing brace
        // Garbage gobbling might leave before landing on a closing brace.
        
        if (_scanner.IsAt(TokenKind.CloseBrace))
        {
            // We need to catch common typing-cases here like
            //    fn a()
            //    {
            //       "Hello {
            //    }
            // We want the last `}` to close the function body instead of this
            // interpolation.
            
            // If the closing brace is followed by either StringText, StringEnd or
            // `{` (opening another interpolation directly), the closing brace here
            // must close the interpolation. As in
            //    fn a()
            //    {
            //       "Hello {
            //    } Bar
            // Where `Bar` is lexed as StringText. We must not disturb the world-view
            // of the Lexer, since that will introduce stray StringText/End tokens into
            // the Parser and produce cascading errors.
            
            // If the closing brace is followed by anything else, it doesn't have
            // to close this interpolation. We can choose. As a heuristic, we choose based
            // on line: `}` closes the interpolation iff if is on the same line as the token
            // before.
            
            // Closing brace can only be a valid interpolation close,
            // if it is followed by StringText, StringEnd or another OpenBrace
            // (starts a new interpolation directly). If that is not the case, the
            // string is unclosed. The same heuristic is valid: Take it if its
            // on the same line, otherwise, leave it to the parser. This is easy to
            // check and catches the common typing scenarios. It's not perfect, but good enough.
            
            if (_scanner.Peek(1).Kind is TokenKind.StringText or TokenKind.StringEnd or TokenKind.OpenBrace ||
                !HasNewlineBeforeNextToken())
            {
                _scanner.AdvanceKnown(TokenKind.CloseBrace);
            }
        }
        else if (!errorReported)
        {
            ReportMissing(TokenKind.CloseBrace);
        }

        _scanner.Close(interpolationHole, SyntaxKind.StringInterpolation);
    }

    private void AdvanceStringInterpolationGarbage()
    {
        // The parser is already confused: It sits after and expression with no
        // closing brace, which should close the interpolation. Now the goal is
        // to determine, which garbage belongs to this interpolation.
        //
        // If we see that the string will be continued (by StringText/End),
        // the garbage must belong inside this interpolation.
        // Otherwise, we are free to choose how to handle the garbage. To catch
        // common typing scenarios, we only gobble everything until a newline
        // and then stop.
        
        //    "Hello {1+2 
        //    var a = 3;
        // This is the common typing case: Valid expression inside interpolation,
        // there is no `}` and we enter gobbling. VarDecl will not belong inside
        // this interpolation.
        
        //    "Hello {1+2 fn
        //        var } Bla";
        // Even if nonsensical, `fn var` must be gobbled into the interpolation hole,
        // since the string will be continued.
        
        Debug.Assert(!_scanner.IsAt(TokenKind.CloseBrace));
        
        MarkOpen? errorExpr = null;
        var braceCount = 0;

        // Calculate once before the gobble-loop and recalculate
        // only when the result can change. That is the advancement
        // of StringStart, StringText or StringEnd.
        var willCurrentStringBeContinued = WillCurrentStringBeContinued();
        
        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            // --- Nominal Termination on BraceClose:
            // We must exit this loop, when we see a `}` that closes
            // the interpolation. For that, we must keep track of inner braces.
            // In cases like `"Foo { a {} b`, `b` must be gobbled.
            if (_scanner.IsAt(TokenKind.OpenBrace))
                braceCount++;
            else if (_scanner.IsAt(TokenKind.CloseBrace))
            {
                if (braceCount == 0)
                    break;
                braceCount--;
            }
            
            // --- Belongs outside this interpolation?
            if (!willCurrentStringBeContinued && HasNewlineBeforeNextToken())
                break;
            
            // --- Gobble Gobble Gobble
            // Error has been reported by ParseStringInterpolation already,
            // so we must not report again.
            errorExpr ??= _scanner.Open();
            var advancedToken = _scanner.Advance();
            
            // Recalculate if necessary.
            if (advancedToken.Kind is TokenKind.StringStart or TokenKind.StringText or TokenKind.StringEnd)
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
            
            // The outcome only relies on StringStart, StringText and StringEnd.
            
            // PERF 1: If it ever shows up: Might be possible to calculate total count
            //         of StringStart/Text/End tokens and then calculate continuously
            //         in the gobble loop. This method reads cleaner and the cases where
            //         it matters (nested string interpolation _with_ errors at the end)
            //         should be quite rare.
            // PERF 2: Might be smart to bound the amount of lookahead tokens to 100-200,
            //         if it ever shows up in profiling. Will reject valid strings
            //         in extremely rare cases.
            
            // Note that if the scanner is currently sitting on StringStart, we will
            // regard as being outside (just one before) the string that is opened by 
            // this StringStart.
            
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

    #endregion
}