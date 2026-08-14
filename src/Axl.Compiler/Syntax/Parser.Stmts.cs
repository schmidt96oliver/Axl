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

            EnsureExpr(anchor | TokenKind.Semicolon);
            EnsureSemicolonIfRequired(ownsBody: isBodied);
            
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
        _scanner.EatKnown(TokenKind.VarKw);

        // --- Name
        EnsureIdName();

        // --- Optional type annotation
        if (_scanner.IsAt(TokenKind.Colon))
        {
            _scanner.EatKnown(TokenKind.Colon);
            EnsureTypeName();
        }

        // --- Optional initializer
        if (_scanner.IsAt(TokenKind.Equal))
        {
            _scanner.EatKnown(TokenKind.Equal);
            EnsureExpr(anchor);
        }

        // --- Semicolon
        EnsureToken(TokenKind.Semicolon);

        return _scanner.Close(varDecl, SyntaxKind.VarDecl);
    }
}