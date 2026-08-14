using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EnsureExpr(Anchor anchor)
    {
        switch (_scanner.Peek().Kind)
        {
            // --- BodiedExprs
            case TokenKind.IfKw:
                return EatIf(anchor);

            case TokenKind.LoopKw:
                return EatLoop(anchor);

            case TokenKind.OpenBrace:
                return EnsureBlock(anchor);

            // --- TailExprs
            case TokenKind.BreakKw:
                var breakExpr = _scanner.Open();
                _scanner.EatKnown(TokenKind.BreakKw);
                if (_scanner.IsAt(FirstSet.Expr))
                    EnsureExpr(anchor);
                return _scanner.Close(breakExpr, SyntaxKind.BreakExpr);

            case TokenKind.ReturnKw:
                var returnExpr = _scanner.Open();
                _scanner.EatKnown(TokenKind.ReturnKw);
                if (_scanner.IsAt(FirstSet.Expr))
                    EnsureExpr(anchor);
                return _scanner.Close(returnExpr, SyntaxKind.ReturnExpr);

            case TokenKind.ContinueKw:
                return _scanner.EatIntoNode(SyntaxKind.ContinueExpr);
            
            // --- OperandExprs or Assign
            // Ambiguous between plain OperandExpr and Assign(OperandExpr "=" Expr).
            // So ensure an OperandExpr and handle "=" thereafter.
            default:
            {
                Debug.Assert(_scanner.IsAt(FirstSet.OperandExpr) || !_scanner.IsAt(FirstSet.Expr),
                    $"{nameof(FirstSet.Expr)} is larger than the switch above.");
                var operandExpr = EnsureOperandExpr(left: null, anchor);

                if (_scanner.IsAt(FirstSet.AssignOperator))
                {
                    // We have assign.
                    var assignExpr = _scanner.OpenBefore(operandExpr);
                    _scanner.Eat();
                    EnsureExpr(anchor);
                    return _scanner.Close(assignExpr, SyntaxKind.AssignExpr);
                }

                return operandExpr;
            }
                
        }
    }


    private MarkClose EatIf(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.IfKw));

        var ifExpr = _scanner.Open();
        _scanner.EatKnown(TokenKind.IfKw);

        // --- Condition and body
        var ifAnchor = anchor | TokenKind.ElseKw;
        EnsureOperandExpr(left: null, ifAnchor);
        EnsureBody(ifAnchor);

        // --- Else
        // Note, that we don't anchor on else anymore here, since
        // we cannot handle it after we've seen it once.
        if (_scanner.IsAt(TokenKind.ElseKw))
        {
            _scanner.EatKnown(TokenKind.ElseKw);

            if (_scanner.IsAt(TokenKind.IfKw))
                EatIf(anchor);
            else
                EnsureBody(anchor);
        }

        return _scanner.Close(ifExpr, SyntaxKind.IfExpr);
    }

    private MarkClose EatLoop(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.LoopKw));

        var loopExpr = _scanner.Open();
        _scanner.EatKnown(TokenKind.LoopKw);
        EnsureBody(anchor);
        return _scanner.Close(loopExpr, SyntaxKind.LoopExpr);
    }



    private MarkClose EnsureBlock(Anchor anchor, ExpectedSyntax? expectedSyntax = null)
    {
        var block = _scanner.Open();

        if (!EnsureToken(TokenKind.OpenBrace, expectedSyntax))
        {
            _scanner.Make(TokenKind.CloseBrace);
            return _scanner.Close(block, SyntaxKind.BlockExpr);
        }

        // Stmt can start from Expr or Var. Anchor only on
        // VarKw, because Expr would be too permissive.
        // FnDecl, `=>`, `}` is what we handle here. `;` is
        // a natural boundary for statements.
        var blockAnchor = anchor |
                          TokenKind.VarKw | FirstSet.FnDecl |
                          TokenKind.RightDoubleArrow | TokenKind.CloseBrace |
                          TokenKind.Semicolon;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // --- Statement or FnDecl
            if (_scanner.IsAt(FirstSet.Stmt))
                EatStmt(blockAnchor);
            else if (_scanner.IsAt(FirstSet.FnDecl))
                EatMemberDecl(blockAnchor);
            
            // --- lone ";" special case
            else if (_scanner.IsAt(TokenKind.Semicolon))
            {
                ReportUnexpected(ExpectedSyntax.Stmt);
                _scanner.EatIntoErrorNode(ExpectedSyntax.Stmt);
            }
            
            // --- Closing tokens
            else if (_scanner.IsAt(TokenKind.CloseBrace))
                break;
            else if (_scanner.IsAt(TokenKind.RightDoubleArrow))
            {
                EatArm(blockAnchor);

                // --- Catch common `=> expr; }` error, where arm is closed with semicolon
                if (_scanner.IsAt(TokenKind.Semicolon) && _scanner.Peek(1).Kind is TokenKind.CloseBrace)
                {
                    ReportUnexpected(TokenKind.CloseBrace);
                    _scanner.EatIntoErrorNode(TokenKind.CloseBrace);
                }
                
                // Arm ends the block, so break out. After the loop,
                // `}` will be ensured.
                break;
            }
            else if (_scanner.IsAt(anchor))
                break;
            
            // --- Garbage
            else
            {
                // Recover to Expr as well, because they can legitimately start
                // another Stmt.
                RecoverTo(blockAnchor | FirstSet.Expr, ExpectedSyntax.Stmt);
            }
        }

        EnsureToken(TokenKind.CloseBrace);
        return _scanner.Close(block, SyntaxKind.BlockExpr);
    }

    private MarkClose EatArm(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.RightDoubleArrow));

        var arm = _scanner.Open();
        _scanner.EatKnown(TokenKind.RightDoubleArrow);
        EnsureExpr(anchor);
        return _scanner.Close(arm, SyntaxKind.Arm);
    }
}