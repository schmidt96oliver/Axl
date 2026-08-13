using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
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

            // --- Advance operator and parse
            if (postfixPrecedence is not null)
                lhs = EatPostfixOperandExpr(lhs, anchor);
            else // Infix expr
            {
                var expr = _scanner.OpenBefore(lhs);
                _scanner.EatToken();

                AdvanceOperandExprRhs(new LeftOperator(opPrecedence.Value, opToken), anchor,
                    out var ateAmbiguousOperatorChain);

                lhs = _scanner.Close(expr,
                    ateAmbiguousOperatorChain
                        ? SyntaxKind.Error
                        : SyntaxKind.BinaryExpr);
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

        // --- Native Type Name
        if (_scanner.IsAt(FirstSet.NativeTypeName))
            return EatNativeTypeName();

        // --- IdName
        if (_scanner.IsAt(TokenKind.Identifier))
            return EatIdName();

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

            case TokenKind.TrueKw:
                return _scanner.Close(openMark, SyntaxKind.TrueLiteral);
            case TokenKind.FalseKw:
                return _scanner.Close(openMark, SyntaxKind.FalseLiteral);
        }

        throw new UnreachableException($"{nameof(FirstSet.OperandExpr)} was too large");
    }

    private MarkClose EatPostfixOperandExpr(MarkClose lhs, Anchor anchor)
    {
        var expr = _scanner.OpenBefore(lhs);
        switch (_scanner.Peek().Kind)
        {
            // --- GetMember
            case TokenKind.Dot:
                _scanner.EatToken(TokenKind.Dot);
                ExpectIdName();
                return _scanner.Close(expr, SyntaxKind.GetMemberExpr);

            // --- Call
            case TokenKind.OpenParen:
                EatArgList(anchor);
                return _scanner.Close(expr, SyntaxKind.CallExpr);

            default:
                throw new UnreachableException("Not a postfix op.");
        }
    }

    /// <summary>
    /// Parses the right side of any OperandExpr.
    /// Handles ambiguous operators gracefully:
    /// It collects all chained ambiguous operators and reports one
    /// diagnostic for them.
    /// </summary>
    /// <param name="ateAmbiguousOperatorChain">
    /// <c>True</c> iff an ambiguous chain was advanced.
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
            ReportError(new Diagnostic.InvalidOperatorChaining(_source,
                ambiguousOperators!.DrainToImmutable()));
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
        var errorReported = RecoverTo(groupAnchor, expected: TokenKind.CloseParen);

        if (_scanner.IsAt(TokenKind.CloseParen))
            _scanner.EatToken(TokenKind.CloseParen);
        else if (!errorReported)
            ReportMissing(TokenKind.CloseParen);

        return _scanner.Close(expr, SyntaxKind.GroupExpr);
    }


    private MarkClose EatArgList(Anchor anchor)
        => EatDelimitedList(anchor,
            openToken: TokenKind.OpenParen, 
            closeToken: TokenKind.CloseParen,
            listKind: SyntaxKind.ArgList,
            itemFirst: FirstSet.Expr,
            eatItem: ExpectExpr);
}