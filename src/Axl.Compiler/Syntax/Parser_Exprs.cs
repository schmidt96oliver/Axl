using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EnsureExpr(Anchor anchor)
        => EnsureExpr(left: null, anchor);
    
    
    private MarkClose EnsureExpr(LeftOperator? left, Anchor anchor)
    {
        if (!_scanner.IsAt(FirstSet.Expr))
            return EnsureIdName(ExpectedSyntax.Expr);

        var lhs = EatExprHead(anchor);
        return ContinueExpr(lhs, left, anchor);
    }

    private MarkClose EnsureParenthesizedExpr(Anchor anchor, SyntaxKind kind)
    {
        var parenExpr = _scanner.Open();

        EnsureToken(TokenKind.OpenParen);

        var insideAnchor = anchor | TokenKind.CloseParen;
        var expr = EnsureExpr(insideAnchor);

        // --- "=" instead of "==" error production
        if (_scanner.IsAt(TokenKind.Equal))
        {
            var equalsCondition = _scanner.OpenBefore(expr);

            // Eat `=` as error and insert missing `==`
            var equalToken = _scanner.Peek();
            _scanner.EatIntoGarbageAndReport(TokenKind.DoubleEqual);
            _scanner.MakeAndReport(TokenKind.DoubleEqual);

            // Eat rhs
            var leftOperator = new LeftOperator(PrecedenceTable.TryGetInfixPrecedence(TokenKind.DoubleEqual)!.Value,
                equalToken);
            EnsureExprRhs(leftOperator, anchor, out var wasAmbiguous);

            expr = _scanner.Close(equalsCondition, wasAmbiguous ? SyntaxKind.ErrorExpr : SyntaxKind.BinaryExpr);

            // Continue pratt loop to consume operator of lower precedence.
            // Pass in null as left operator, because that is what we passed
            // in originally.
            expr = ContinueExpr(expr, left: null, anchor);

        }

        EnsureToken(TokenKind.CloseParen);
        return _scanner.Close(parenExpr, kind);
    }


    private MarkClose ContinueExpr(MarkClose lhs, LeftOperator? left, Anchor anchor)
    {
        // --- Pratt loop
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // --- Read operator and check precedence
            var opToken = _scanner.Peek();
            var infixPrecedence = PrecedenceTable.TryGetInfixPrecedence(opToken.Kind);
            var postfixPrecedence = PrecedenceTable.TryGetPostfixPrecedence(opToken.Kind);
            if (infixPrecedence is null && postfixPrecedence is null)
                break;
            Debug.Assert(infixPrecedence is null || postfixPrecedence is null, "Operator is both infix and postfix. This might trap something here.");

            var opPrecedence = postfixPrecedence ?? infixPrecedence;
            Debug.Assert(opPrecedence is not null);

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
                // collect all ambiguous operators.
                break;

            }

            // --- Correct binding power?
            if (precedenceComparison is PrecedenceComparison.LeftBindsTighter)
            {
                // The left side bind tighter, so we stop here and let the enclosing
                // loop handle that operator.
                break;
            }

            // --- Advance operator and parse
            if (postfixPrecedence is not null)
                lhs = EatPostfixExpr(lhs, anchor);
            else // Infix expr
            {
                var expr = _scanner.OpenBefore(lhs);
                _scanner.Eat();

                EnsureExprRhs(new LeftOperator(opPrecedence.Value, opToken), anchor, out var wasAmbiguous);
                lhs = _scanner.Close(expr, wasAmbiguous ? SyntaxKind.ErrorExpr : SyntaxKind.BinaryExpr);
            }
        }

        return lhs;
    }

    private MarkClose EatExprHead(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Expr));

        switch (_scanner.Peek().Kind)
        {
            // --- Literals
            case TokenKind.Identifier:
                return EnsureIdName();
            case TokenKind.NumberLiteral:
                return _scanner.EatInto(SyntaxKind.NumberLiteral);

            case TokenKind.TrueKw:
                return _scanner.EatInto(SyntaxKind.TrueLiteral);
            case TokenKind.FalseKw:
                return _scanner.EatInto(SyntaxKind.FalseLiteral);

            // --- Strings
            case TokenKind.StringStart:
                return EnsureStringExpr(anchor);

            // --- Group
            case TokenKind.OpenParen:
                return EatGroupExpr(anchor);

            // --- Blocks, Control Flow
            case TokenKind.OpenBrace:
                return EnsureBlock(anchor);

            case TokenKind.IfKw:
                return EatIf(anchor);
            case TokenKind.BreakKw:
                return _scanner.EatInto(SyntaxKind.BreakExpr);
            case TokenKind.ContinueKw:
                return _scanner.EatInto(SyntaxKind.ContinueExpr);
            case TokenKind.ReturnKw:
                var returnExpr = _scanner.Open();
                _scanner.EatKnown(TokenKind.ReturnKw);
                if (_scanner.IsAt(FirstSet.Expr))
                    EnsureExpr(anchor);
                return _scanner.Close(returnExpr, SyntaxKind.ReturnExpr);
        }

        // --- Prefix operator
        var openMark = _scanner.Open();
        var token = _scanner.Eat();

        if (PrecedenceTable.TryGetPrefixPrecedence(token.Kind) is Precedence prefixPrecedence)
        {
            EnsureExprRhs(new LeftOperator(prefixPrecedence, token), anchor, out var wasAmbiguous);
            return _scanner.Close(openMark, wasAmbiguous ? SyntaxKind.ErrorExpr : SyntaxKind.UnaryExpr);
        }

        throw new UnreachableException($"{nameof(FirstSet.Expr)} was too large");
    }

    private MarkClose EatPostfixExpr(MarkClose lhs, Anchor anchor)
    {
        var expr = _scanner.OpenBefore(lhs);
        switch (_scanner.Peek().Kind)
        {
            // --- GetMember
            case TokenKind.Dot:
                _scanner.EatKnown(TokenKind.Dot);
                EnsureIdName();
                return _scanner.Close(expr, SyntaxKind.GetMemberExpr);

            // --- Call
            case TokenKind.OpenParen:
                EnsureArgList(anchor);
                return _scanner.Close(expr, SyntaxKind.CallExpr);

            default:
                throw new UnreachableException("Not a postfix op.");
        }
    }

    /// <summary>
    /// Ensures the right side of any OperandExpr.
    /// Handles ambiguous operators gracefully:
    /// It collects all chained ambiguous operators and reports one
    /// diagnostic for them.
    /// </summary>
    /// <param name="wasAmbiguous">
    /// <c>True</c> iff an ambiguous chain was advanced.
    /// </param>
    private void EnsureExprRhs(LeftOperator left, Anchor anchor, out bool wasAmbiguous)
    {
        // Short-circuit if at Eof, because the loop below would not
        // enter and in cases like `1 + [EOF]` leave the binary expression
        // unfinished.
        if (_scanner.IsAtEnd)
        {
            EnsureExpr(left, anchor);
            wasAmbiguous = false;
            return;
        }
        
        ImmutableArray<Token>.Builder? ambiguousOperators = null;
        var previousOperatorPrecedence = left.Precedence;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            EnsureExpr(left, anchor);

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
            _scanner.Eat();

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

        wasAmbiguous = ambiguousOperators is not null;
        if (ambiguousOperators is not null)
        {
            _scanner.ReportHere(new Diagnostic.InvalidOperatorChaining(_sourceText,
                ambiguousOperators.DrainToImmutable()));
        }
    }


    private MarkClose EatGroupExpr(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.OpenParen));
        return EnsureParenthesizedExpr(anchor, SyntaxKind.GroupExpr);
    }

    private MarkClose EnsureArgList(Anchor anchor)
    {
        return EnsureDelimitedList(anchor,
            openToken: TokenKind.OpenParen,
            closeToken: TokenKind.CloseParen,
            listKind: SyntaxKind.ArgList,
            itemFirst: FirstSet.Expr,
            ensureItem: EnsureArg, 
            expectedOpenSyntax: null,
            expectedItemSyntax: ExpectedSyntax.Expr);

        MarkClose EnsureArg(Anchor argAnchor)
        {
            var arg = _scanner.Open();
            EnsureExpr(argAnchor);
            return _scanner.Close(arg, SyntaxKind.Arg);
        }
    }
    
    private MarkClose EatIf(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.IfKw));

        var ifExpr = _scanner.Open();
        _scanner.EatKnown(TokenKind.IfKw);

        var ifAnchor = anchor | TokenKind.OpenBrace | TokenKind.ElseKw;
        
        EnsureParenthesizedExpr(ifAnchor, SyntaxKind.ConditionClause);
        EnsureExpr(ifAnchor);
        
        // --- Else
        // Note, that we don't anchor on else anymore here, since
        // we cannot handle it after we've seen it once.
        if (_scanner.IsAt(TokenKind.ElseKw))
        {
            var elseClause = _scanner.Open();
            _scanner.EatKnown(TokenKind.ElseKw);
            EnsureExpr(anchor);
            _scanner.Close(elseClause, SyntaxKind.ElseClause);
        }

        return _scanner.Close(ifExpr, SyntaxKind.IfExpr);
    }
    
    private MarkClose EnsureBlock(Anchor anchor, ExpectedSyntax? expectedSyntax = null)
    {
        var block = _scanner.Open();

        if (!EnsureToken(TokenKind.OpenBrace, expectedSyntax))
        {
            _scanner.MakeAndReport(TokenKind.CloseBrace);
            return _scanner.Close(block, SyntaxKind.BlockExpr);
        }

        var blockAnchor = anchor | FirstSet.NonExprStmt | FirstSet.Member
                          | TokenKind.CloseBrace | TokenKind.Semicolon;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // --- Statement or FnDecl
            if (_scanner.IsAt(FirstSet.Stmt))
                EatStmt(blockAnchor);
            else if (_scanner.IsAt(FirstSet.Member))
                EatMember(blockAnchor);
            
            // --- lone ";" special case
            else if (_scanner.IsAt(TokenKind.Semicolon))
                _scanner.EatIntoGarbageAndReport(ExpectedSyntax.Stmt);
            
            // --- Closing tokens
            else if (_scanner.IsAt(TokenKind.CloseBrace))
                break;
            else if (_scanner.IsAt(anchor))
                break;
            
            // --- Garbage
            else
            {
                // Recover to Expr as well, because they can legitimately start
                // another Stmt.
                var recovered = RecoverToAndReport(blockAnchor | FirstSet.Expr, ExpectedSyntax.Stmt);
                
                // If it's followed by a ';', eat it into an error silently.
                if (recovered && _scanner.IsAt(TokenKind.Semicolon))
                    _scanner.EatInto(SyntaxKind.Garbage);
            }
        }

        EnsureToken(TokenKind.CloseBrace);
        return _scanner.Close(block, SyntaxKind.BlockExpr);
    }
}