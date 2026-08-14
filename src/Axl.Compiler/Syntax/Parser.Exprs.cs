using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose ParseExpr(Anchor anchor)
    {
        //TODO: Maybe move to OperandExpr logic
        if (!_scanner.IsAt(FirstSet.Expr))
        {
            // Synthesize empty identifier expr
            ReportMissing(ExpectedSyntax.Expr);
            var idName = _scanner.Open();
            _scanner.MakeToken(TokenKind.Identifier);
            return _scanner.Close(idName, SyntaxKind.IdName);
        }

        // --- OperandExpr or Assign
        if (_scanner.IsAt(FirstSet.OperandExpr))
        {
            // Ambiguous between plain OperandExpr and
            // Assign = OperandExpr "=" Expr.
            // So eat an OperandExpr and handle "=" thereafter
            // here.
            var operandExpr = EatOperandExpr(left: null, anchor);

            if (_scanner.IsAt(FirstSet.AssignOperator))
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
                _scanner.EatKnownToken(TokenKind.BreakKw);
                if (_scanner.IsAt(FirstSet.Expr))
                    ParseExpr(anchor);
                return _scanner.Close(breakExpr, SyntaxKind.BreakExpr);

            case TokenKind.ReturnKw:
                var returnExpr = _scanner.Open();
                _scanner.EatKnownToken(TokenKind.ReturnKw);
                if (_scanner.IsAt(FirstSet.Expr))
                    ParseExpr(anchor);
                return _scanner.Close(returnExpr, SyntaxKind.ReturnExpr);

            case TokenKind.ContinueKw:
                return _scanner.EatTokenIntoNode(SyntaxKind.ContinueExpr);
        }

        throw new UnreachableException($"{nameof(FirstSet.Expr)} was too large");
    }

    
    private MarkClose EatIf(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.IfKw));

        var ifExpr = _scanner.Open();
        _scanner.EatKnownToken(TokenKind.IfKw);

        // --- Condition and body
        var ifAnchor = anchor | TokenKind.ElseKw;
        ExpectOperandExpr(left: null, ifAnchor);
        ExpectBody(ifAnchor);

        // --- Else
        // Note, that we don't anchor on else anymore here, since
        // we cannot handle it after we've seen it once.
        if (_scanner.IsAt(TokenKind.ElseKw))
        {
            _scanner.EatKnownToken(TokenKind.ElseKw);

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
        _scanner.EatKnownToken(TokenKind.LoopKw);
        ExpectBody(anchor);
        return _scanner.Close(loopExpr, SyntaxKind.LoopExpr);
    }



    private MarkClose EatBlock(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.OpenBrace));

        var block = _scanner.Open();
        _scanner.EatKnownToken(TokenKind.OpenBrace);

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
                _scanner.EatTokenIntoNode(SyntaxKind.Error);
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
                    _scanner.EatTokenIntoNode(SyntaxKind.Error);
                }
                
                // Arm ends the block, so break out. After the loop,
                // `}` will be expected.
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
        _scanner.EatKnownToken(TokenKind.RightDoubleArrow);
        ExpectExpr(anchor);
        return _scanner.Close(arm, SyntaxKind.Arm);
    }
}