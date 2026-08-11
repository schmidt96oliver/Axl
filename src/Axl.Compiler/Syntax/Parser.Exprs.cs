using System.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatExpr(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Expr));

        // --- OperandExpr or Assign
        if (_scanner.IsAt(FirstSet.OperandExpr))
        {
            // Ambiguous between plain OperandExpr and
            // Assign = OperandExpr "=" Expr.
            // So eat an OperandExpr and handle "=" thereafter
            // here.
            var operandExpr = EatOperandExpr(left: null, anchor);

            if (_scanner.IsAt(TokenKind.Equal)
                || _scanner.IsAt(TokenKind.PlusEqual)
                || _scanner.IsAt(TokenKind.MinusEqual))
            {
                // We have assign.
                var assignExpr = _scanner.OpenBefore(operandExpr);
                _scanner.EatToken();
                ExpectExpr(anchor);
                return _scanner.Close(assignExpr, SyntaxKind.AssignExpr);
            }

            return operandExpr;
        }

        switch (_scanner.Peek().Kind)
        {
            // --- BodiedExprs
            case TokenKind.IfKw:
                return EatIf(anchor);

            case TokenKind.LoopKw:
                return EatLoop(anchor);

            case TokenKind.OpenBrace:
                return EatBlock(anchor);

            // --- TailExprs
            case TokenKind.BreakKw:
                var breakExpr = _scanner.Open();
                _scanner.EatToken(TokenKind.BreakKw);
                if (_scanner.IsAt(FirstSet.Expr))
                    EatExpr(anchor);
                return _scanner.Close(breakExpr, SyntaxKind.BreakExpr);

            case TokenKind.ReturnKw:
                var returnExpr = _scanner.Open();
                _scanner.EatToken(TokenKind.ReturnKw);
                if (_scanner.IsAt(FirstSet.Expr))
                    EatExpr(anchor);
                return _scanner.Close(returnExpr, SyntaxKind.ReturnExpr);

            case TokenKind.ContinueKw:
                var continueExpr = _scanner.Open();
                _scanner.EatToken(TokenKind.ContinueKw);
                return _scanner.Close(continueExpr, SyntaxKind.ContinueExpr);
        }

        throw new UnreachableException($"{nameof(FirstSet.Expr)} was too large");
    }

    
    private MarkClose EatIf(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.IfKw));

        var ifExpr = _scanner.Open();
        _scanner.EatToken(TokenKind.IfKw);

        // --- Condition and body
        var ifAnchor = anchor | TokenKind.ElseKw;
        ExpectOperandExpr(left: null, ifAnchor);
        ExpectBody(ifAnchor);

        // --- Else
        // Note, that we don't anchor on else anymore here, since
        // we cannot handle it after we've seen it once.
        if (_scanner.IsAt(TokenKind.ElseKw))
        {
            _scanner.EatToken(TokenKind.ElseKw);

            if (_scanner.IsAt(TokenKind.IfKw))
                EatIf(anchor);
            else
                ExpectBody(anchor);
        }

        return _scanner.Close(ifExpr, SyntaxKind.IfExpr);
    }

    private MarkClose EatLoop(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.LoopKw));

        var loopExpr = _scanner.Open();
        _scanner.EatToken(TokenKind.LoopKw);
        ExpectBody(anchor);
        return _scanner.Close(loopExpr, SyntaxKind.LoopExpr);
    }



    private MarkClose EatBlock(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.OpenBrace));

        var block = _scanner.Open();
        _scanner.EatToken(TokenKind.OpenBrace);

        // Stmt can start from Expr or Decl. Anchor only on
        // Decl, because Expr would be too permissive.
        // Also anchor on `=>` and `}`, because we can handle those.
        var blockAnchor = anchor |
                          TokenKind.VarKw | FirstSet.FnDecl |
                          TokenKind.RightDoubleArrow | TokenKind.CloseBrace |
                          TokenKind.Semicolon;

        var hadArm = false;
        MarkOpen? errorAfterArm = null;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(TokenKind.CloseBrace))
                break;

            if (_scanner.IsAt(FirstSet.Stmt))
            {
                if (hadArm) errorAfterArm ??= _scanner.Open();

                EatStmt(blockAnchor);
            }
            else if (_scanner.IsAt(TokenKind.RightDoubleArrow))
            {
                if (hadArm) errorAfterArm ??= _scanner.Open();

                EatArm(blockAnchor);
                // No semicolon!

                if (!hadArm && !_scanner.IsAt(TokenKind.CloseBrace))
                    ReportMissing(TokenKind.CloseBrace);
                hadArm = true;
            }
            else if (_scanner.IsAt(FirstSet.FnDecl))
                EatMemberDecl(blockAnchor);
            else if (_scanner.IsAt(TokenKind.Semicolon))
            {
                if (!hadArm)
                {
                    ReportUnexpected(expected: SyntaxCategory.Stmt);
                    var error = _scanner.Open();
                    _scanner.EatToken(TokenKind.Semicolon);
                    _scanner.Close(error, SyntaxKind.Error);
                }
                else
                {
                    // If we had an arm, it will be on an error node anyway.
                    // Just eat and don't report further errors
                    errorAfterArm ??= _scanner.Open();
                    _scanner.EatToken(TokenKind.Semicolon);
                }
            }
            else if (_scanner.IsAt(anchor))
            {
                if (!hadArm)
                    ReportMissing(expected: SyntaxCategory.Stmt);
                break;
            }
            else
            {
                if (!hadArm)
                    ReportUnexpected(expected: SyntaxCategory.Stmt);

                if (hadArm) errorAfterArm ??= _scanner.Open();
                // Recover to Expr as well, because they can legitimately start
                // another Stmt.
                RecoverTo(blockAnchor | FirstSet.Expr, expectedKind: null);
            }
        }

        if (hadArm)
        {
            if (errorAfterArm is MarkOpen actualErrorAfterArm)
                _scanner.Close(actualErrorAfterArm, SyntaxKind.Error);
            if (_scanner.IsAt(TokenKind.CloseBrace))
                _scanner.EatToken(TokenKind.CloseBrace);
        }
        else
            ExpectToken(TokenKind.CloseBrace);

        return _scanner.Close(block, SyntaxKind.BlockExpr);
    }

    private MarkClose EatArm(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.RightDoubleArrow));

        var arm = _scanner.Open();
        _scanner.EatToken(TokenKind.RightDoubleArrow);
        ExpectExpr(anchor);
        return _scanner.Close(arm, SyntaxKind.Arm);
    }
}