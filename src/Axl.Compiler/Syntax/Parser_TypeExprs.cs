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

    /// <param name="expectedSyntax">
    /// The <see cref="ExpectedSyntax"/> a missing identifier token will be reported with.
    /// <c>null</c> reports <see cref="TokenKind.Identifier"/>.
    /// </param>
    private MarkClose EnsureIdName(ExpectedSyntax? expectedSyntax = null)
    {
        var idName = _scanner.Open();
        EnsureToken(TokenKind.Identifier, expectedSyntax);
        return _scanner.Close(idName, SyntaxKind.IdName);
    }
    
    private MarkClose EatTypeAnnotation()
    {
        var typeAnnotation = _scanner.Open();
        _scanner.EatKnown(TokenKind.Colon);
        EnsureTypeName();
        return _scanner.Close(typeAnnotation, SyntaxKind.TypeAnnotationClause);
    }
}