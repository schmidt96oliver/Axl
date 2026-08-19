
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EnsureStringExpr(Anchor anchor)
    {
        var expr = _scanner.Open();

        if (!EnsureToken(TokenKind.StringStart, expectedSyntax: ExpectedSyntax.String))
        {
            _scanner.MakeAndReport(TokenKind.StringEnd);
            return _scanner.Close(expr, SyntaxKind.StringExpr);
        }

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // A String can be terminated by a whitespace token. Since we
            // cannot check that here directly, we check if the next token
            // comes after a newline. This is the same, since Lexer will
            // terminate the string early only if it hit a newline.
            if (HasNewlineBeforeNextToken())
                goto breakLoop;

            switch (_scanner.Peek().Kind)
            {
                // --- StringText: Just add
                case TokenKind.StringText:
                    _scanner.EatInto(SyntaxKind.StringText);
                    break;

                // --- StringEnd: Finish
                case TokenKind.StringEnd:
                    goto breakLoop;

                // --- Interpolation
                case TokenKind.OpenBrace:
                    // Anchor on StringText, StringEnd and OpenBrace. Those are the only valid
                    // continuations after an interpolation the Lexer will produce if it thinks
                    // it's inside a string. Everything else is an unclosed string.

                    EatStringInterpolation(anchor | FirstSet.StringContinuation);
                    // The next iteration will handle StringText, StringEnd or OpenBrace. If it
                    // finds anything else (e.g. the enclosing anchor), it breaks off.

                    break;

                // --- Anything else is an unclosed string
                // Note: Whitespace token will terminate the string as well. The
                // check is done above, since the scanner does not see whitespace.
                default:
                    goto breakLoop;
            }
        }

        breakLoop:

        EnsureToken(TokenKind.StringEnd);
        return _scanner.Close(expr, SyntaxKind.StringExpr);
    }

    private MarkClose EatStringInterpolation(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.OpenBrace));

        // Grammar is "{" Expr? "}" and allows for multi-line Expr inside this interpolation.
        // --- Advance `{`
        var interpolationHole = _scanner.Open();
        _scanner.EatKnown(TokenKind.OpenBrace);

        // --- Parse Expression
        // `{ `}` will fall through and consume `}` as closing.
        if (_scanner.IsAt(FirstSet.Expr))
            EnsureExpr(anchor | TokenKind.CloseBrace);

        // --- Recover to anchor or close brace if needed
        if (!_scanner.IsAt(TokenKind.CloseBrace))
        {
            // Parser is confused now. Recover and pass anchors that we got
            // from the enclosing loop. Note that it will handle {, }, StringStart,
            // StringText and StringEnd itself.
            RecoverFromStringInterpolationGarbage(anchor);
        }

        // --- Close brace, valid closing
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
        if (_scanner.IsAt(TokenKind.CloseBrace) && (FirstSet.StringContinuation.Contains(_scanner.Peek(1).Kind) ||
                                                    !HasNewlineBeforeNextToken()))
        {
            _scanner.EatKnown(TokenKind.CloseBrace);
        }
        else
        {
            _scanner.MakeAndReport(TokenKind.CloseBrace);
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

        var firstPosition = _scanner.Position;
        
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
                if (!_scanner.IsAt(FirstSet.StringPart))
                    break;
            }

            // --- Belongs outside this interpolation?
            if (!willCurrentStringBeContinued && HasNewlineBeforeNextToken())
                break;

            // --- Gobble Gobble Gobble
            errorExpr ??= _scanner.Open();
            var advancedToken = _scanner.Eat();

            // Recalculate if necessary.
            if (FirstSet.StringPart.Contains(advancedToken.Kind))
                willCurrentStringBeContinued = WillCurrentStringBeContinued();
        }

        if (errorExpr is MarkOpen openedErrorExpr)
        {
            _scanner.ReportUnexpectedTokensUntilHere(firstPosition, TokenKind.CloseBrace);
            return _scanner.Close(openedErrorExpr, SyntaxKind.Error);
        }
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
}