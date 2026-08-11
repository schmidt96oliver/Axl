using System.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatStmt(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Stmt));

        if (_scanner.IsAt(FirstSet.Expr))
        {
            var exprStmt = _scanner.Open();
            var isBodied = _scanner.IsAt(FirstSet.BodiedExpr);

            EatExpr(anchor | TokenKind.Semicolon);

            var semicolonOmissible = isBodied && _scanner.Last?.Kind is TokenKind.CloseBrace;
            if (semicolonOmissible)
            {
                if (_scanner.IsAt(TokenKind.Semicolon))
                    _scanner.EatToken(TokenKind.Semicolon);
            }
            else
                ExpectToken(TokenKind.Semicolon);

            return _scanner.Close(exprStmt, SyntaxKind.ExprStmt);
        }

        if (_scanner.IsAt(TokenKind.VarKw))
            return EatVarDecl(anchor);

        throw new UnreachableException($"{nameof(FirstSet.Stmt)} too large.");
    }

    private MarkClose EatVarDecl(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.VarKw));

        var varDecl = _scanner.Open();
        _scanner.EatToken(TokenKind.VarKw);

        // --- Name
        ExpectIdName();

        // --- Optional type annotation
        if (_scanner.IsAt(TokenKind.Colon))
        {
            _scanner.EatToken(TokenKind.Colon);
            ExpectTypeName();
        }

        // --- Optional initializer
        if (_scanner.IsAt(TokenKind.Equal))
        {
            _scanner.EatToken(TokenKind.Equal);
            ExpectExpr(anchor);
        }

        // --- Semicolon
        ExpectToken(TokenKind.Semicolon);

        return _scanner.Close(varDecl, SyntaxKind.VarDecl);
    }
}
