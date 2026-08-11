using System.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatQualifiedName()
    {
        Debug.Assert(_scanner.IsAt(FirstSet.QualifiedName));

        var typeExpr = _scanner.Open();
        ExpectIdName();

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (!_scanner.IsAt(TokenKind.Dot))
                break;

            _scanner.EatToken(TokenKind.Dot);
            var idExpr = ExpectIdName();
            if (idExpr is null)
                break;
        }

        return _scanner.Close(typeExpr, SyntaxKind.QualifiedName);
    }

    private MarkClose EatNativeTypeName()
    {
        Debug.Assert(_scanner.IsAt(FirstSet.NativeTypeName));
        var expr = _scanner.Open();
        _scanner.EatToken();
        return _scanner.Close(expr, SyntaxKind.NativeTypeName);
    }

    private MarkClose EatIdName()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.Identifier));

        var idName = _scanner.Open();
        _scanner.EatToken(TokenKind.Identifier);
        return _scanner.Close(idName, SyntaxKind.IdName);
    }
}
