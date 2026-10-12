using Axl.Compiler.Diagnostics;

// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EnsureTypeName(ExpectedSyntax? expectedSyntax = null)
    {
        // Parse into a MemberAccessExpr, because it works exactly the same way.
        // It just needs to be a subset, so not all expressions are allowed in
        // type position.

        var lhs = EnsureIdName(expectedSyntax ?? ExpectedSyntax.TypeName);
        
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (!_scanner.IsAt(TokenKind.Dot))
                break;

            var memberAccess = _scanner.OpenBefore(lhs);
            _scanner.EatKnown(TokenKind.Dot);
            EnsureIdName();
            lhs = _scanner.Close(memberAccess, SyntaxKind.MemberAccessExpr);
        }

        return lhs;
    }

    
    private MarkClose EnsureTypeAnnotation()
    {
        var typeAnnotation = _scanner.Open();
        if (!EnsureToken(TokenKind.Colon, ExpectedSyntax.TypeAnnotation))
        {
            // Make an id name
            var idName = _scanner.Open();
            _scanner.MakeAndReport(TokenKind.Identifier);
            _scanner.Close(idName, SyntaxKind.IdName);
            return _scanner.Close(typeAnnotation, SyntaxKind.TypeAnnotationClause);
        }
        
        EnsureTypeName();
        return _scanner.Close(typeAnnotation, SyntaxKind.TypeAnnotationClause);
    }
}