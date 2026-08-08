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

    public static SyntaxTree Parse(SourceFileView source)
    {
        var diagnosticBag = new DiagnosticBag();
        var tokens = Lexer.Lex(source, diagnosticBag);

        var scanner = new Scanner(tokens);
        var parser = new Parser(source, scanner, diagnosticBag);
        parser.Parse();
        return parser.BuildTree();
    }

    private void Parse()
    {
        var file = _scanner.Open();

        var globalAnchor = TokenSet.Of(TokenKind.FnKw, TokenKind.ModuleKw, 
            TokenKind.VarKw, TokenKind.UsingKw, TokenKind.PublicKw, TokenKind.PrivateKw, 
            TokenKind.NativeKw);

        // Eof must be anchored, because every loop stops implicitly at Eof
        // and will assert that it's at a known position.
        globalAnchor |= TokenKind.Eof;
        
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(FirstSet.Stmt))
                EatStmt(globalAnchor);
            else
            {
                ReportUnexpected(expected: SyntaxCategory.Stmt);
                RecoverTo(FirstSet.Stmt | TokenKind.Eof, null);
            }
        }

        _scanner.EatToken(TokenKind.Eof);
        _scanner.Close(file, SyntaxKind.TreeRoot);
    }
    
    
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
                        Debug.Assert(nextToken == tokens.Length, "TreeRoot did not eat all tokens.");
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
    
    
    #region Helpers

    /// <summary>
    /// If scanner is not at <paramref name="anchor"/>, collects garbage into
    /// a <see cref="SyntaxKind.Error"/> node and reports <see cref="Diagnostic.UnexpectedToken"/>.
    /// Always leaves the scanner on <paramref name="anchor"/>.
    /// </summary>
    /// <returns><c>True</c> iff garbage was collected and an error node added.</returns>
    private bool RecoverTo(TokenSet anchor, TokenKind? expectedToken)
    {
        if (_scanner.IsAt(anchor))
            return false;
        
        if (expectedToken is TokenKind kind)
            ReportUnexpected(kind);

        // Eat garbage into an error node
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
    
    #endregion
    
    #region Expects

    /// <summary>
    /// Eats and returns next token, if it has <paramref name="expectedKind"/>.
    /// Otherwise, reports <see cref="Diagnostic.MissingToken"/> and returns <c>null</c>.
    /// </summary>
    private Token? ExpectToken(TokenKind expectedKind)
    {
        if (!_scanner.IsAt(expectedKind))
        {
            ReportMissing(expectedKind);
            return null;
        }

        return _scanner.EatToken(expectedKind);
    }
    
    private MarkClose? ExpectOperandExpr(LeftOperator? left, TokenSet anchor)
    {
        if (!_scanner.IsAt(FirstSet.OperandExpr))
        {
            ReportMissing(expected: SyntaxCategory.Expr);
            return null;
        }

        return EatOperandExpr(left, anchor);
    }

    private MarkClose? ExpectExpr(TokenSet anchor)
    {
        if (!_scanner.IsAt(FirstSet.Expr))
        {
            ReportMissing(expected: SyntaxCategory.Expr);
            return null;
        }

        return EatExpr(anchor);
    }
    
    #endregion
    
    
    #region Statements and Declarations

    private MarkClose EatStmt(TokenSet anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Stmt));
        
        var stmt = _scanner.Open();
        
        if (_scanner.IsAt(FirstSet.OperandExpr))
        {
            EatOperandExpr(left: null, 
                anchor: anchor | TokenKind.Semicolon);
            ExpectToken(TokenKind.Semicolon);
            
            return _scanner.Close(stmt, SyntaxKind.ExprStmt);;
        }

        throw new UnreachableException($"{nameof(FirstSet.Stmt)} too large.");
    }
    
    #endregion

    #region Expr, TailExpr, BodiedExpr
    
    private MarkClose EatExpr(TokenSet anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Expr));

        if (_scanner.IsAt(FirstSet.OperandExpr))
            return EatOperandExpr(left: null, anchor);

        throw new InvalidOperationException($"{nameof(FirstSet.Expr)} was too large");
    }
    
    #endregion

    #region Operand Expressions

    private MarkClose EatOperandExpr(LeftOperator? left, TokenSet anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.OperandExpr));
        
        // --- Head
        var lhs = EatOperandExprHead(anchor);

        // --- Pratt loop
        foreach (var _ in _scanner.MustEatEachIteration())
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
            
            // --- Advance operator
            switch (opToken.Kind)
            {
                // --- GetMember
                case TokenKind.Dot:
                    _scanner.EatToken(TokenKind.Dot);
                    ExpectToken(TokenKind.Identifier);
                    lhs = _scanner.Close(expr, SyntaxKind.GetMemberExpr);
                    break;
                
                // --- Call
                case TokenKind.OpenParen:
                    
                    // We know how to handle another operator.
                    EatArgList(anchor);
                    lhs = _scanner.Close(expr, SyntaxKind.CallExpr);
                    break;
                
                // --- Binary, everything else
                default:
                    _scanner.EatToken();
                    
                    // --- Parse RHS
                    AdvanceOperandExprRhs(new LeftOperator(opPrecedence.Value, opToken), anchor,
                        out var ateAmbiguousOperatorChain);
            
                    // --- Close expression
                    lhs = _scanner.Close(expr,
                        ateAmbiguousOperatorChain
                            ? SyntaxKind.Error
                            : SyntaxKind.BinaryExpr);
                    break;
            }
        }

        return lhs;
    }
    
    private MarkClose EatOperandExprHead(TokenSet anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.OperandExpr));
        
        // --- String
        if (_scanner.IsAt(TokenKind.StringStart))
            return EatStringExpr();
        
        // --- Group
        if (_scanner.IsAt(TokenKind.OpenParen))
            return EatGroupExpr(anchor);
        
        // For everything else, we can advance a token
        // already and then switch on it.
        var openMark = _scanner.Open();
        var token = _scanner.EatToken();
        
        // --- Prefix Operator
        if (PrecedenceTable.TryGetPrefixPrecedence(token.Kind) is Precedence prefixPrecedence)
        {
            AdvanceOperandExprRhs(new LeftOperator(prefixPrecedence, token), anchor, out var ateAmbiguousOperatorChain);
            return _scanner.Close(openMark,
                ateAmbiguousOperatorChain
                    ? SyntaxKind.Error
                    : SyntaxKind.UnaryExpr);
        }
        
        // Switch on everything else
        switch (token.Kind)
        {
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

        throw new UnreachableException($"{nameof(FirstSet.OperandExpr)} was too large");
    }
    
    /// <summary>
    /// Parses the right side of any OperandExpr.
    /// Handles ambiguous operators gracefully:
    /// It collects all chained ambiguous operators and reports one
    /// diagnostic for them.
    /// </summary>
    /// <param name="ateAmbiguousOperatorChain">
    /// <c>False</c> iff an ambiguous chain was advanced.
    /// </param>
    private void AdvanceOperandExprRhs(LeftOperator left, TokenSet anchor, out bool ateAmbiguousOperatorChain)
    {
        ImmutableArray<Token>.Builder? ambiguousOperators = null;

        var previousOperatorPrecedence = left.Precedence;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // Eat OperandExpr
            if (ExpectOperandExpr(left, anchor) is null)
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
            _scanner.EatToken();

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

        ateAmbiguousOperatorChain = ambiguousOperators is not null;
        
        if (ateAmbiguousOperatorChain)
        {
            //Report combined diagnostic.
            _diagnosticBag.ReportError(new Diagnostic.InvalidOperatorChaining(_source,
                ambiguousOperators!.DrainToImmutable()));
        }
    }

    
    private MarkClose EatStringExpr()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.StringStart));

        var expr = _scanner.Open();
        _scanner.EatToken(TokenKind.StringStart);

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            switch (_scanner.Peek().Kind)
            {
                // --- StringText: Just add
                case TokenKind.StringText:
                    var text = _scanner.Open();
                    _scanner.EatToken(TokenKind.StringText);
                    _scanner.Close(text, SyntaxKind.StringText);
                    break;
                
                // --- StringEnd: Finish
                case TokenKind.StringEnd:
                    _scanner.EatToken(TokenKind.StringEnd);
                    return _scanner.Close(expr, SyntaxKind.StringExpr);
                
                // --- Interpolation
                case TokenKind.OpenBrace:
                    EatStringInterpolation();
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
        Debug.Assert(_scanner.Last is not null);
        
        ReportUnclosedString();
        return _scanner.Close(expr, SyntaxKind.StringExpr);

        void ReportUnclosedString()
        {
            // There must be at least a StringStart token which has been advanced.
            Debug.Assert(_scanner.Last is not null);
            
            _diagnosticBag.ReportError(new Diagnostic.UnclosedString(
                _source,
                LastToken: _scanner.Last));
        }
    }

    private MarkClose EatStringInterpolation()
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
        _scanner.EatToken(TokenKind.OpenBrace);

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
            // MissingToken diagnostic is reported. Thus,
            // we update errorReported flag.
            var expr = ExpectExpr(anchor: TokenSet.Of(
                TokenKind.StringStart, TokenKind.StringText, TokenKind.StringEnd, TokenKind.CloseBrace, TokenKind.Eof));
            errorReported = expr is null;
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
            MaybeEatStringInterpolationGarbage();
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
                _scanner.EatToken(TokenKind.CloseBrace);
            }
        }
        else if (!errorReported)
        {
            ReportMissing(TokenKind.CloseBrace);
        }

        return _scanner.Close(interpolationHole, SyntaxKind.StringInterpolation);
    }

    private MarkClose? MaybeEatStringInterpolationGarbage()
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
        
        foreach (var _ in _scanner.MustEatEachIteration())
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
            var advancedToken = _scanner.EatToken();
            
            // Recalculate if necessary.
            if (advancedToken.Kind is TokenKind.StringStart or TokenKind.StringText or TokenKind.StringEnd)
                willCurrentStringBeContinued = WillCurrentStringBeContinued();
        }

        if (errorExpr is MarkOpen openedErrorExpr)
            return _scanner.Close(openedErrorExpr, SyntaxKind.Error);
        return null;
        
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


    private MarkClose EatGroupExpr(TokenSet anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.OpenParen));

        var expr = _scanner.Open();
        _scanner.EatToken(TokenKind.OpenParen);

        // --- Expression
        var groupAnchor = anchor | TokenKind.CloseParen;
        ExpectExpr(groupAnchor);

        // --- Recover if confused
        var errorReported = RecoverTo(groupAnchor, expectedToken: TokenKind.CloseParen);

        if (_scanner.IsAt(TokenKind.CloseParen))
            _scanner.EatToken(TokenKind.CloseParen);
        else if (!errorReported)
            ReportMissing(TokenKind.CloseParen);
        
        return _scanner.Close(expr, SyntaxKind.GroupExpr);
    }


    private MarkClose EatArgList(TokenSet anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.OpenParen));

        var argList = _scanner.Open();
        _scanner.EatToken(TokenKind.OpenParen);

        // --- Special-case `( )`
        if (_scanner.IsAt(TokenKind.CloseParen))
        {
            _scanner.EatToken(TokenKind.CloseParen);
            return _scanner.Close(argList, SyntaxKind.ArgList);
        }
        
        // --- Expect arguments
        var argAnchor = anchor | TokenSet.Of(TokenKind.CloseParen, TokenKind.Comma);
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // Each iteration expects an argument, i.e. an expression.
            
            // --- Expr
            var expr = ExpectExpr(argAnchor);
            if (expr is MarkClose argExpr)
            {
                // Wrap into SyntaxKind.Arg
                var arg = _scanner.OpenBefore(argExpr);
                _scanner.Close(arg, SyntaxKind.Arg);
            }

            // --- Confused?
            RecoverTo(argAnchor, expectedToken: expr is not null ? TokenKind.Comma : null);
            
            // --- Next token
            if (_scanner.IsAt(TokenKind.Comma))
            {
                _scanner.EatToken(TokenKind.Comma);
                
                // Expect another expression
                continue;
            }

            if (_scanner.IsAt(TokenKind.CloseParen) ||
                _scanner.IsAt(anchor))
            {
                break;
            }

            // Every branch continues or breaks.
            throw new UnreachableException();
        }
        
        // --- Expect `)`
        ExpectToken(TokenKind.CloseParen);
        return _scanner.Close(argList, SyntaxKind.ArgList);
    }
    
    #endregion
}