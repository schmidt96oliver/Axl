using System.Diagnostics;
using Axl.Compiler.Diagnostics;

// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatStmt(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Stmt));

        if (_scanner.IsAt(FirstSet.Expr))
            return EatExprStmt(anchor);

        if (_scanner.IsAt(TokenKind.VarKw))
            return EatVarDecl(anchor);
        
        if (_scanner.IsAt(TokenKind.WhileKw))
            return EatWhile(anchor);

        throw new UnreachableException($"{nameof(FirstSet.Stmt)} too large.");
    }

    private MarkClose EatExprStmt(Anchor anchor)
    {
        var stmt = _scanner.Open();

        var exprHasBody = _scanner.IsAt(TokenKind.IfKw) || _scanner.IsAt(TokenKind.OpenBrace);
            
        EnsureExpr(anchor | TokenKind.Semicolon);
            
        var semicolonOmissible = exprHasBody && _scanner.Last?.Kind is TokenKind.CloseBrace;
        if (semicolonOmissible && _scanner.IsAt(TokenKind.Semicolon))
            _scanner.EatKnown(TokenKind.Semicolon);
        else if (!semicolonOmissible)
            EnsureToken(TokenKind.Semicolon);
            
        return _scanner.Close(stmt, SyntaxKind.ExprStmt);
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
            var typeAnnotation = _scanner.Open();
            _scanner.EatKnown(TokenKind.Colon);
            EnsureTypeName();
            _scanner.Close(typeAnnotation, SyntaxKind.TypeAnnotationClause);
        }

        // --- Optional initializer
        if (_scanner.IsAt(TokenKind.Equal))
        {
            var initializer = _scanner.Open();
            _scanner.EatKnown(TokenKind.Equal);
            EnsureExpr(anchor);
            _scanner.Close(initializer, SyntaxKind.InitializerClause);
        }

        // --- Semicolon
        EnsureToken(TokenKind.Semicolon);

        return _scanner.Close(varDecl, SyntaxKind.VarDecl);
    }

    private MarkClose EatWhile(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.WhileKw));

        var whileStmt = _scanner.Open();
        _scanner.EatKnown(TokenKind.WhileKw);

        EnsureParenthesizedExpr(anchor, SyntaxKind.ConditionClause);

        EnsureBlock(anchor, ExpectedSyntax.Block);
        return _scanner.Close(whileStmt, SyntaxKind.WhileStmt);
    }
}