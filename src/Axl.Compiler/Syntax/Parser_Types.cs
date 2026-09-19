using System.Diagnostics;
using Axl.Compiler.Diagnostics;

// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EnsureTypeName(ExpectedSyntax? expectedSyntax = null)
    {
        var typeExpr = _scanner.Open();
        EnsureIdName(expectedSyntax ?? ExpectedSyntax.TypeName);

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (!_scanner.IsAt(TokenKind.Dot))
                break;

            _scanner.EatKnown(TokenKind.Dot);
            EnsureIdName();
        }

        return _scanner.Close(typeExpr, SyntaxKind.TypeName);
    }

    
    private MarkClose EnsureTypeAnnotation()
    {
        var typeAnnotation = _scanner.Open();
        if (!EnsureToken(TokenKind.Colon, ExpectedSyntax.TypeAnnotation))
        {
            // Make a type annotation
            var typeName = _scanner.Open();
            var idName = _scanner.Open();
            _scanner.MakeAndReport(TokenKind.Identifier);
            _scanner.Close(idName, SyntaxKind.IdName);
            _scanner.Close(typeName, SyntaxKind.TypeName);
            return _scanner.Close(typeAnnotation, SyntaxKind.TypeAnnotationClause);
        }
        
        EnsureTypeName();
        return _scanner.Close(typeAnnotation, SyntaxKind.TypeAnnotationClause);
    }
}