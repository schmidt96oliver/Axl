using System.Diagnostics;
using Axl.Compiler.Diagnostics;

// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EnsureTypeName()
    {
        if (_scanner.IsAt(FirstSet.NativeTypeName))
            return EatNativeTypeName();
        
        return EnsureQualifiedName(ExpectedSyntax.TypeName);
    }

    private MarkClose EnsureQualifiedName(ExpectedSyntax? expectedSyntax = null)
    {
        var typeExpr = _scanner.Open();
        EnsureIdName(expectedSyntax);

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (!_scanner.IsAt(TokenKind.Dot))
                break;

            _scanner.EatKnown(TokenKind.Dot);
            EnsureIdName();
        }

        return _scanner.Close(typeExpr, SyntaxKind.QualifiedName);
    }

    private MarkClose EatNativeTypeName()
    {
        Debug.Assert(_scanner.IsAt(FirstSet.NativeTypeName));
        return _scanner.EatInto(SyntaxKind.NativeTypeName);
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
}