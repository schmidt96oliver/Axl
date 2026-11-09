using AxlCompiler.Diagnostics;

// ReSharper disable UnusedMethodReturnValue.Local

namespace AxlCompiler.Syntax;

public partial class Parser
{
    private MarkClose EnsureTypeName(Anchor anchor, ExpectedSyntax? expectedSyntax = null)
    {
        // Parse into a MemberAccessExpr / SubscriptExpr, because it works exactly
        // the same way. It just needs to be a subset, so not all expressions are
        // allowed in type position.

        var lhs = EnsureIdName(expectedSyntax ?? ExpectedSyntax.TypeName);
        
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(TokenKind.Dot))
            {
                var memberAccess = _scanner.OpenBefore(lhs);
                _scanner.EatKnown(TokenKind.Dot);
                EnsureIdName(expectedSyntax ?? ExpectedSyntax.TypeName);
                lhs = _scanner.Close(memberAccess, SyntaxKind.MemberAccessExpr);
            }

            else if (_scanner.IsAt(TokenKind.OpenBracket))
            {
                var subscript = _scanner.OpenBefore(lhs);
                EnsureDelimitedList(anchor,
                    openToken: TokenKind.OpenBracket,
                    closeToken: TokenKind.CloseBracket,
                    listKind: SyntaxKind.SubscriptArgList,
                    itemFirst: TokenSet.Of(TokenKind.Identifier),
                    allowEmpty: false,
                    ensureItem: a => EnsureTypeName(a),
                    expectedOpenSyntax: null,
                    expectedItemSyntax: ExpectedSyntax.TypeName);
                lhs = _scanner.Close(subscript, SyntaxKind.SubscriptExpr);
            }
            
            else
                break;
        }

        return lhs;
    }

    
    private MarkClose EnsureTypeAnnotation(Anchor anchor)
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
        
        EnsureTypeName(anchor);
        return _scanner.Close(typeAnnotation, SyntaxKind.TypeAnnotationClause);
    }
}