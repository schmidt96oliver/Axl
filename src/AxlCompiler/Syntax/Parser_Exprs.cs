using System.Collections.Immutable;
using System.Diagnostics;
using AxlCompiler.Diagnostics;

// ReSharper disable UnusedMethodReturnValue.Local

namespace AxlCompiler.Syntax;

public partial class Parser
{
    /// <summary>
    /// The operator the current operand expression is nested under. The token is
    /// carried along, so diagnostics can point at both sides of a conflict. The
    /// precedence cannot be derived from the token alone, since prefix and infix
    /// operators share spellings.
    /// </summary>
    private readonly record struct LeftOperator(Precedence Precedence, Token Token);
    
    private MarkClose EnsureExpr(Anchor anchor)
        => EnsureExpr(left: null, anchor);
    
    
    private MarkClose EnsureExpr(LeftOperator? left, Anchor anchor)
    {
        if (!_scanner.IsAt(FirstSet.Expr))
            return EnsureIdName(ExpectedSyntax.Expr);

        var lhs = EatExprHead(anchor);
        
        // --- Pratt loop
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (!_scanner.IsAt(SyntaxFacts.PostfixOperators | SyntaxFacts.InfixOperators))
                break;
            Debug.Assert(!_scanner.IsAt(SyntaxFacts.PostfixOperators) || !_scanner.IsAt(SyntaxFacts.InfixOperators), 
                "Operator is both infix and postfix. This might trap something here.");
            
            // --- Read operator and check precedence
            var precedence = _scanner.IsAt(SyntaxFacts.PostfixOperators)
                ? SyntaxFacts.GetPostfixPrecedence(_scanner.Peek().Kind)
                : SyntaxFacts.GetInfixPrecedence(_scanner.Peek().Kind);
            
            var precedenceComparison = left is LeftOperator actualLeft
                ? PrecedenceComparison.Compare(actualLeft.Precedence, precedence)
                : PrecedenceComparisonResult.RightBindsTighter;

            // --- Ambiguous?
            if (precedenceComparison is PrecedenceComparisonResult.Ambiguous)
            {
                // For the precedence to be ambiguous, we must be on a tail,
                // so we never drop an ambiguous operator.
                Debug.Assert(left is not null);

                // Ambiguous operators belong to the enclosing loop, which will
                // collect all ambiguous operators.
                break;

            }

            // --- Correct binding power?
            if (precedenceComparison is PrecedenceComparisonResult.LeftBindsTighter)
            {
                // The left side bind tighter, so we stop here and let the enclosing
                // loop handle that operator.
                break;
            }

            // --- Advance operator and parse
            if (_scanner.IsAt(SyntaxFacts.PostfixOperators))
                lhs = EatPostfixExpr(lhs, anchor);
            else // Infix expr
            {
                var expr = _scanner.OpenBefore(lhs);
                var opToken = _scanner.Eat();

                EnsureExprRhs(new LeftOperator(precedence, opToken), anchor, out var wasAmbiguous);
                lhs = _scanner.Close(expr, wasAmbiguous ? SyntaxKind.ErrorExpr : SyntaxKind.BinaryExpr);
            }
        }

        return lhs;
    }

    private MarkClose EnsureParenthesizedExpr(Anchor anchor, SyntaxKind kind)
    {
        var parenExpr = _scanner.Open();

        EnsureToken(TokenKind.OpenParen);

        var insideAnchor = anchor | TokenKind.CloseParen;
        EnsureExpr(insideAnchor);

        EnsureToken(TokenKind.CloseParen);
        return _scanner.Close(parenExpr, kind);
    }


    private MarkClose EatExprHead(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Expr));

        // --- Literals
        if (_scanner.IsAt(TokenKind.Identifier))
            return EnsureIdName();

        if (_scanner.IsAt(TokenKind.NumberLiteral))
            return _scanner.EatInto(SyntaxKind.NumberLiteral);

        if (_scanner.IsAt(TokenKind.SelfKw))
            return _scanner.EatInto(SyntaxKind.Self);

        if (_scanner.IsAt(TokenKind.TrueKw))
            return _scanner.EatInto(SyntaxKind.TrueLiteral);

        if (_scanner.IsAt(TokenKind.FalseKw))
            return _scanner.EatInto(SyntaxKind.FalseLiteral);
        
        if (_scanner.IsAt(TokenKind.StringStart))
            return EnsureStringExpr(anchor);
        
        // --- Group
        if (_scanner.IsAt(TokenKind.OpenParen))
            return EatGroupExpr(anchor);
        
        // --- Blocks, Control Flow
        if (_scanner.IsAt(TokenKind.OpenBrace))
            return EnsureBlock(anchor);

        if (_scanner.IsAt(TokenKind.IfKw))
            return EatIf(anchor);

        if (_scanner.IsAt(TokenKind.BreakKw))
            return _scanner.EatInto(SyntaxKind.BreakExpr);

        if (_scanner.IsAt(TokenKind.ContinueKw))
            return _scanner.EatInto(SyntaxKind.ContinueExpr);

        if (_scanner.IsAt(TokenKind.ReturnKw))
        {
            var returnExpr = _scanner.Open();
            _scanner.EatKnown(TokenKind.ReturnKw);
            _scanner.EatIfPresent(TokenKind.MutKw);
            if (_scanner.IsAt(FirstSet.Expr))
                EnsureExpr(anchor);
            return _scanner.Close(returnExpr, SyntaxKind.ReturnExpr);
        }

        // --- Prefix operator
        var openMark = _scanner.Open();
        var token = _scanner.Eat();

        if (SyntaxFacts.TryGetPrefixPrecedence(token.Kind) is Precedence prefixPrecedence)
        {
            EnsureExprRhs(new LeftOperator(prefixPrecedence, token), anchor, out var wasAmbiguous);
            return _scanner.Close(openMark, wasAmbiguous ? SyntaxKind.ErrorExpr : SyntaxKind.UnaryExpr);
        }

        throw new UnreachableException($"{nameof(FirstSet.Expr)} was too large");
    }

    private MarkClose EatPostfixExpr(MarkClose lhs, Anchor anchor)
    {
        // --- MemberAccess        
        var expr = _scanner.OpenBefore(lhs);
        if (_scanner.IsAt(TokenKind.Dot))
        {
            _scanner.EatKnown(TokenKind.Dot);
            EnsureIdName();
            return _scanner.Close(expr, SyntaxKind.MemberAccessExpr);
        }
        
        // --- Call, Subscript
        if (_scanner.IsAt(TokenKind.OpenParen))
        {
            EnsureArgList(anchor);
            return _scanner.Close(expr, SyntaxKind.CallExpr);
        }

        if (_scanner.IsAt(TokenKind.OpenBracket))
        {
            EnsureSubscriptArgList(anchor);
            return _scanner.Close(expr, SyntaxKind.SubscriptExpr);
        }

        throw new UnreachableException("Not a postfix op.");
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
        var previousPrecedence = left.Precedence;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            EnsureExpr(left, anchor);

            // Peek and check if next token is
            // an operator with ambiguous comparison.
            if (!_scanner.IsAt(SyntaxFacts.InfixOperators))
                break;

            var nextPrecedence = SyntaxFacts.GetInfixPrecedence(_scanner.Peek().Kind);
            if (PrecedenceComparison.Compare(previousPrecedence, nextPrecedence)
                is not PrecedenceComparisonResult.Ambiguous)
            {
                break;
            }

            previousPrecedence = nextPrecedence;

            // Next operator is ambiguous.
            // Advance it and parse another expression.
            var opToken = _scanner.Eat();

            // If ambiguous operators was empty before, we need to add
            // the operator that was passed in, because that was already
            // ambiguous.
            if (ambiguousOperators is null)
            {
                ambiguousOperators = ImmutableArray.CreateBuilder<Token>();
                ambiguousOperators.Add(left.Token);
            }

            ambiguousOperators.Add(opToken);
        }

        wasAmbiguous = ambiguousOperators is not null;
        if (ambiguousOperators is not null)
        {
            _scanner.ReportHere(new Diagnostic.InvalidOperatorChaining(_sourceText,
                ambiguousOperators.DrainToImmutable()));
        }
    }


    private MarkClose EnsureIdName(ExpectedSyntax? expectedSyntax = null)
    {
        var idName = _scanner.Open();
        EnsureToken(TokenKind.Identifier, expectedSyntax);
        return _scanner.Close(idName, SyntaxKind.IdName);
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
            itemFirst: FirstSet.Expr | TokenKind.MutKw,
            allowEmpty: true,
            ensureItem: EnsureArg, 
            expectedOpenSyntax: null,
            expectedItemSyntax: ExpectedSyntax.Expr);

        MarkClose EnsureArg(Anchor argAnchor)
        {
            // (IdName "=")? "mut"? Expr
            
            var arg = _scanner.Open();
            
            if (_scanner.IsAt(TokenKind.Identifier) && _scanner.IsAt(TokenKind.Equal, skip: 1))
            {
                // Named argument
                _scanner.EatInto(SyntaxKind.IdName);
                _scanner.EatKnown(TokenKind.Equal);
            }
            
            _scanner.EatIfPresent(TokenKind.MutKw);
            
            EnsureExpr(argAnchor);
            return _scanner.Close(arg, SyntaxKind.Arg);
        }
    }
    
    private MarkClose EnsureSubscriptArgList(Anchor anchor)
    {
        return EnsureDelimitedList(anchor,
            openToken: TokenKind.OpenBracket,
            closeToken: TokenKind.CloseBracket,
            listKind: SyntaxKind.SubscriptArgList,
            itemFirst: FirstSet.Expr,
            allowEmpty: false,
            ensureItem: EnsureExpr, 
            expectedOpenSyntax: null,
            expectedItemSyntax: ExpectedSyntax.Expr);
    }
    
    private MarkClose EatIf(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.IfKw));

        var ifExpr = _scanner.Open();
        _scanner.EatKnown(TokenKind.IfKw);

        var ifAnchor = anchor | TokenKind.OpenBrace | TokenKind.ElseKw | TokenKind.Semicolon;
        
        EnsureParenthesizedExpr(ifAnchor, SyntaxKind.ConditionClause);
        EnsureExpr(ifAnchor);

        if (_scanner.IsAt(TokenKind.Semicolon) && _scanner.IsAt(TokenKind.ElseKw, skip: 1))
            _scanner.Eat();
        
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

        var blockAnchor = anchor | FirstSet.NonExprStmt | TokenKind.CloseBrace | TokenKind.Semicolon;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // --- Statement
            if (_scanner.IsAt(FirstSet.Stmt))
                EatStmt(blockAnchor);
            
            // --- lone ";" special case
            else if (_scanner.IsAt(TokenKind.Semicolon, isGrammarCheck: false))
                _scanner.EatIntoGarbageAndReport(ExpectedSyntax.Stmt);
            
            // --- Closing tokens
            else if (_scanner.IsAt(TokenKind.CloseBrace))
                break;
            else if (_scanner.IsAt(anchor, isGrammarCheck: false))
                break;
            
            // --- Garbage
            else
            {
                // Recover to Expr as well, because they can legitimately start
                // another Stmt.
                var recovered = RecoverToAndReport(blockAnchor | FirstSet.Expr, ExpectedSyntax.Stmt);
                
                // If it's followed by a ';', eat it into an error silently.
                if (recovered && _scanner.IsAt(TokenKind.Semicolon, isGrammarCheck: false))
                    _scanner.EatInto(SyntaxKind.Garbage);
            }
        }

        EnsureToken(TokenKind.CloseBrace);
        return _scanner.Close(block, SyntaxKind.BlockExpr);
    }
}