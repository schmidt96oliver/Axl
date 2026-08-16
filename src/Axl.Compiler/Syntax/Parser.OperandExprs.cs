using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EnsureOperandExpr(LeftOperator? left, Anchor anchor)
    {
        if (!_scanner.IsAt(FirstSet.OperandExpr))
            return EnsureIdName(ExpectedSyntax.Expr);

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
                lhs = EatPostfixOperandExpr(lhs, anchor);
            else // Infix expr
            {
                var expr = _scanner.OpenBefore(lhs);
                _scanner.Eat();

                EnsureOperandExprRhs(new LeftOperator(opPrecedence.Value, opToken), anchor, out var invalidChainingError);
                if (invalidChainingError is not null)
                {
                    _scanner.ReportUnsuppressible(invalidChainingError);
                    lhs = _scanner.CloseAsUnexplainedError(expr);
                }
                else
                    lhs =  _scanner.Close(expr, SyntaxKind.BinaryExpr);
            }
        }

        return lhs;
    }

    private MarkClose EatOperandExprHead(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.OperandExpr));

        // --- String
        if (_scanner.IsAt(TokenKind.StringStart))
            return EnsureStringExpr(anchor);

        // --- Group
        if (_scanner.IsAt(TokenKind.OpenParen))
            return EatGroupExpr(anchor);

        // --- Native Type Name
        if (_scanner.IsAt(FirstSet.NativeTypeName))
            return EatNativeTypeName();

        // --- IdName
        if (_scanner.IsAt(TokenKind.Identifier))
            return EnsureIdName();

        // For everything else, we can advance a token
        // already and then switch on it.
        var openMark = _scanner.Open();
        var token = _scanner.Eat();

        // --- Prefix Operator
        if (PrecedenceTable.TryGetPrefixPrecedence(token.Kind) is Precedence prefixPrecedence)
        {
            EnsureOperandExprRhs(new LeftOperator(prefixPrecedence, token), anchor, out var invalidChainingError);
            if (invalidChainingError is not null)
            {
                _scanner.ReportUnsuppressible(invalidChainingError);
                return _scanner.CloseAsUnexplainedError(openMark);
            }
            else
                return _scanner.Close(openMark, SyntaxKind.UnaryExpr);
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
    /// <param name="ateAmbiguousOperatorChain">
    /// <c>True</c> iff an ambiguous chain was advanced.
    /// </param>
    private void EnsureOperandExprRhs(LeftOperator left, Anchor anchor, out Diagnostic.InvalidOperatorChaining? invalidChainingError)
    {
        ImmutableArray<Token>.Builder? ambiguousOperators = null;

        var previousOperatorPrecedence = left.Precedence;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            EnsureOperandExpr(left, anchor);

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

        if (ambiguousOperators is not null)
        {
            invalidChainingError = new Diagnostic.InvalidOperatorChaining(_source,
                ambiguousOperators!.DrainToImmutable());
            ReportError(invalidChainingError);
        }
        else
            invalidChainingError = null;
    }


    private MarkClose EatGroupExpr(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.OpenParen));

        var expr = _scanner.Open();
        _scanner.EatKnown(TokenKind.OpenParen);

        // --- Expression
        var groupAnchor = anchor | TokenKind.CloseParen;
        EnsureExpr(groupAnchor);

        // --- Recover if confused
        var errorReported = RecoverTo(groupAnchor, expectedSyntax: TokenKind.CloseParen);

        if (_scanner.IsAt(TokenKind.CloseParen))
            _scanner.EatKnown(TokenKind.CloseParen);
        else if (!errorReported)
            ReportMissing(TokenKind.CloseParen);

        return _scanner.Close(expr, SyntaxKind.GroupExpr);
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
}