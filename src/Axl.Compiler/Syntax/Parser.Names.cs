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

            _scanner.EatKnownToken(TokenKind.Dot);
            var idExpr = ExpectIdName();
            if (idExpr is null)
                break;
        }

        return _scanner.Close(typeExpr, SyntaxKind.QualifiedName);
    }

    private MarkClose EatNativeTypeName()
    {
        Debug.Assert(_scanner.IsAt(FirstSet.NativeTypeName));
        return _scanner.EatTokenIntoNode(SyntaxKind.NativeTypeName);
    }

    private MarkClose EatIdName()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.Identifier));
        return _scanner.EatTokenIntoNode(SyntaxKind.IdName);
    }
}