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
                return _scanner.EatInto(SyntaxKind.ContinueExpr);
            
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
        var ifAnchor = anchor | FirstSet.Body | TokenKind.ElseKw;
        var predicate = EnsureOperandExpr(left: null, ifAnchor);

        if (_scanner.IsAt(TokenKind.Equal))
        {
            // Two error production collide. `if a = 1` could be read as
            //   1. Arm: `if a => 1`
            //   2. Equality: `if a == 1`
            // Number 2 is probably more often what is meant. So check, if the
            // next token can start an OperandExpr. If so, take 2. Otherwise,
            // take 1.
            if (FirstSet.OperandExpr.Contains(_scanner.Peek(1).Kind))
            {
                var equalsPredicate = _scanner.OpenBefore(predicate);
                
                // Eat `=` as error and insert missing `==`
                var equalToken = _scanner.Peek();
                _scanner.EatIntoGarbageAndReport(TokenKind.DoubleEqual);
                _scanner.MakeAndReport(TokenKind.DoubleEqual);

                // Eat rhs
                var leftOperator = new LeftOperator(PrecedenceTable.TryGetInfixPrecedence(TokenKind.DoubleEqual)!.Value,
                    equalToken);
                EnsureOperandExprRhs(leftOperator, anchor, out var wasAmbiguous);

                predicate = _scanner.Close(equalsPredicate, wasAmbiguous ? SyntaxKind.ErrorExpr : SyntaxKind.BinaryExpr);
                
                // Continue pratt loop to consume operator of lower precedence.
                // Pass in null as left operator, because that is what we passed
                // in originally.
                predicate = ContinueOperandExpr(predicate, left: null, anchor);
            }
        }
        
        EnsureBody(ifAnchor);

        // --- Else
        // Note, that we don't anchor on else anymore here, since
        // we cannot handle it after we've seen it once.
        if (_scanner.IsAt(TokenKind.ElseKw))
        {
            var elseClause = _scanner.Open();
            _scanner.EatKnown(TokenKind.ElseKw);

            if (_scanner.IsAt(TokenKind.IfKw))
                EatIf(anchor);
            else
                EnsureBody(anchor);
            _scanner.Close(elseClause, SyntaxKind.ElseClause);
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
            _scanner.MakeAndReport(TokenKind.CloseBrace);
            return _scanner.Close(block, SyntaxKind.BlockExpr);
        }

        // Stmt can start from Expr or Var. Anchor only on
        // VarKw, because Expr would be too permissive.
        // FnDecl, `=>`, `}` is what we handle here. `;` is
        // a natural boundary for statements.
        var blockAnchor = anchor | FirstSet.NonExprStmt | FirstSet.Member |
                          FirstSet.Arm | TokenKind.CloseBrace |
                          TokenKind.Semicolon;

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // --- Statement or FnDecl
            if (_scanner.IsAt(FirstSet.Stmt | TokenKind.UsingKw))
                EatStmtOrUsing(blockAnchor);
            else if (_scanner.IsAt(FirstSet.Member))
                EatMember(blockAnchor, onGlobalScope: false);
            
            // --- lone ";" special case
            else if (_scanner.IsAt(TokenKind.Semicolon))
                _scanner.EatIntoGarbageAndReport(ExpectedSyntax.Stmt);
            
            // --- Closing tokens
            else if (_scanner.IsAt(TokenKind.CloseBrace))
                break;
            else if (_scanner.IsAt(FirstSet.Arm))
            {
                // Arm contains `=` as error production. It's safe because any
                // assign expression has already been eaten.
                EatArm(blockAnchor);

                // --- Catch common `=> expr; }` error, where arm is closed with semicolon
                if (_scanner.IsAt(TokenKind.Semicolon) && _scanner.Peek(1).Kind is TokenKind.CloseBrace)
                    _scanner.EatIntoGarbageAndReport(TokenKind.CloseBrace);
                
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
                var recovered = RecoverToAndReport(blockAnchor | FirstSet.Expr, ExpectedSyntax.Stmt);
                
                // If it's followed by a ';', eat it into an error silently.
                if (recovered && _scanner.IsAt(TokenKind.Semicolon))
                    _scanner.EatInto(SyntaxKind.Garbage);
            }
        }

        EnsureToken(TokenKind.CloseBrace);
        return _scanner.Close(block, SyntaxKind.BlockExpr);
    }

    private MarkClose EatArm(Anchor anchor)
    {
        // Accept '=' as error production.
        Debug.Assert(_scanner.IsAt(FirstSet.Arm));

        var arm = _scanner.Open();

        if (_scanner.IsAt(TokenKind.Equal))
        {
            // Eat the `=` as an error to be honest and construct a missing
            // `=>`, so the grammar still reads correctly.
            _scanner.EatIntoGarbageAndReport(TokenKind.RightDoubleArrow);
            _scanner.MakeAndReport(TokenKind.RightDoubleArrow);
        }
        else
            _scanner.EatKnown(TokenKind.RightDoubleArrow);
        
        EnsureExpr(anchor);
        
        return _scanner.Close(arm, SyntaxKind.Arm);
    }
}