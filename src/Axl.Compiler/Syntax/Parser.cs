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

        var globalAnchor = Anchor.Of(TokenKind.FnKw, TokenKind.ModuleKw, 
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
                RecoverTo(Anchor.From(FirstSet.Stmt), null);
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
    private bool RecoverTo(Anchor anchor, TokenKind? expectedToken)
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
    
    private MarkClose? ExpectOperandExpr(LeftOperator? left, Anchor anchor)
    {
        if (!_scanner.IsAt(FirstSet.OperandExpr))
        {
            ReportMissing(expected: SyntaxCategory.Expr);
            return null;
        }

        return EatOperandExpr(left, anchor);
    }

    private MarkClose? ExpectExpr(Anchor anchor)
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

    private MarkClose EatStmt(Anchor anchor)
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
    
    private MarkClose EatExpr(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Expr));

        if (_scanner.IsAt(FirstSet.OperandExpr))
            return EatOperandExpr(left: null, anchor);

        throw new InvalidOperationException($"{nameof(FirstSet.Expr)} was too large");
    }
    
    #endregion

    #region Operand Expressions

    private MarkClose EatOperandExpr(LeftOperator? left, Anchor anchor)
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
    
    private MarkClose EatOperandExprHead(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.OperandExpr));
        
        // --- String
        if (_scanner.IsAt(TokenKind.StringStart))
            return EatStringExpr(anchor);
        
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
    private void AdvanceOperandExprRhs(LeftOperator left, Anchor anchor, out bool ateAmbiguousOperatorChain)
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

    
    private MarkClose EatStringExpr(Anchor anchor)
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
                    // Anchor on StringText, StringEnd and BraceClose. Those are the only valid
                    // continuations after an interpolation the Lexer will produce if it thinks
                    // it's inside a string. Everything else is an unclosed string.

                    EatStringInterpolation(anchor | TokenKind.StringText | TokenKind.StringEnd | TokenKind.OpenBrace);
                    // The next iteration will handle StringText, StringEnd or OpenBrace. If it
                    // finds anything else (e.g. the enclosing anchor), it breaks off.
                    
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

    private MarkClose EatStringInterpolation(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.OpenBrace));
        
        // Grammar is "{" Expr? "}" and allows for multi-line Expr inside this interpolation.
        // --- Advance `{`
        var interpolationHole = _scanner.Open();
        _scanner.EatToken(TokenKind.OpenBrace);

        // --- Parse Expression or empty interpolation
        // `{ `}` will fall through and consume `}` as closing.
        var errorReported = false;
        if (!_scanner.IsAt(TokenKind.CloseBrace))
        {
            errorReported = ExpectExpr(anchor | TokenKind.CloseBrace) is null;
        }

        // --- Recover to anchor or close brace if needed
        if (!_scanner.IsAt(TokenKind.CloseBrace))
        {
            if (!errorReported)
            {
                ReportMissing(TokenKind.CloseBrace);
                errorReported = true;
            }
            
            // Parser is confused now. Recover and pass anchors that we got
            // from the enclosing loop. Note that it will handle {, }, StringStart,
            // StringText and StringEnd itself.
            RecoverFromStringInterpolationGarbage(anchor);
        }
        
        // --- Close brace, valid closing
        if (_scanner.IsAt(TokenKind.CloseBrace))
        {
            // We need to catch common typing-cases here like
            //    fn a()
            //    {
            //       "Hello {
            //    }
            // We want the last `}` to close the function body instead of this
            // interpolation.
            
            // Closing brace can only be a valid interpolation close,
            // if it is followed by StringText, StringEnd or another OpenBrace
            // (starts a new interpolation directly). If that is not the case, the
            // string is unclosed. If } is on the same line, take it as closing the
            // interpolation, otherwise leave it to enclosing loops.
            
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

    /// <summary>
    /// Recovers from garbage inside a string interpolation. It stops on <paramref name="anchor"/>
    /// or based on heuristics to make typing scenarios more resilient.
    /// </summary>
    private MarkClose? RecoverFromStringInterpolationGarbage(Anchor anchor)
    {
        // The parser is confused: It sits after an expression with no
        // closing brace. The goal is to determine, which garbage belongs to
        // this interpolation and what tokens an outer loop should take care of.
        
        // (1) Anchors will be handled outside, except for StringStart/Text/End (see below).
        // (2) If we see that the string will be continued (by StringText/End),
        //     the garbage must belong inside this interpolation.
        // (3) Otherwise, we eat every token on the same line and then stop. This heuristic should
        //     catch most scenarios.
        
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
            else if (_scanner.IsAt(anchor))
            {
                // {, }, StringStart/Text/End will be handled by this loop, so
                // we ignore the anchor if it has them.
                if (_scanner.Peek().Kind is not (TokenKind.StringStart or TokenKind.StringText or TokenKind.StringEnd))
                    break;
            }
            
            // --- Belongs outside this interpolation?
            if (!willCurrentStringBeContinued && HasNewlineBeforeNextToken())
                break;

            // --- Gobble Gobble Gobble
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


    private MarkClose EatGroupExpr(Anchor anchor)
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


    private MarkClose EatArgList(Anchor anchor)
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