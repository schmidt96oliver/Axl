using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    /// <summary>
    /// If the scanner is on <paramref name="expectedKind"/>, eats it and returns
    /// <c>true</c>. Otherwise, creates a missing token of <paramref name="expectedKind"/>,
    /// reports <see cref="Diagnostic.MissingToken"/> and returns <c>false</c>.
    /// </summary>
    /// <param name="expectedSyntaxName">The <see cref="ExpectedSyntax"/> a missing token will be reported with. <c>null</c> reports
    /// <paramref name="expectedKind"/>.</param>
    /// <returns>If scanner was at <paramref name="expectedKind"/>.</returns>
    private bool EnsureToken(TokenKind expectedKind, ExpectedSyntax? expectedSyntaxName = null)
    {
        if (!_scanner.IsAt(expectedKind))
        {
            _scanner.MakeToken(expectedKind);
            ReportMissing(expectedSyntaxName ?? expectedKind);
            return false;
        }

        _scanner.EatKnownToken(expectedKind);
        return true;
    }

    private MarkClose? ExpectOperandExpr(LeftOperator? left, Anchor anchor)
    {
        if (!_scanner.IsAt(FirstSet.OperandExpr))
        {
            ReportMissing(ExpectedSyntax.Expr);
            return null;
        }

        return EatOperandExpr(left, anchor);
    }

    private MarkClose? ExpectExpr(Anchor anchor)
    {
        if (!_scanner.IsAt(FirstSet.Expr))
        {
            ReportMissing(ExpectedSyntax.Expr);
            return null;
        }

        return EnsureExpr(anchor);
    }

    private MarkClose EnsureBody(Anchor anchor)
    {
        if (_scanner.IsAt(TokenKind.RightDoubleArrow))
            return EatArm(anchor);
        
        // Report better missing message
        if (!_scanner.IsAt(TokenKind.OpenBrace))
            ReportMissing(ExpectedSyntax.Body);

        return EnsureBlock(anchor);
    }

    private MarkClose? ExpectTypeName()
    {
        if (_scanner.IsAt(FirstSet.NativeTypeName))
            return EatNativeTypeName();
        if (_scanner.IsAt(TokenKind.Identifier))
            return EnsureQualifiedName();

        Debug.Assert(!_scanner.IsAt(FirstSet.TypeName), $"{FirstSet.TypeName} is too large.");
        ReportMissing(ExpectedSyntax.TypeName);
        return null;
    }

    private MarkClose? ExpectIdName()
    {
        if (!_scanner.IsAt(TokenKind.Identifier))
        {
            ReportMissing(TokenKind.Identifier);
            return null;
        }

        return EnsureIdName();
    }

    private MarkClose? ExpectQualifiedName()
    {
        if (!_scanner.IsAt(FirstSet.QualifiedName))
        {
            ReportMissing(ExpectedSyntax.TypeName);
            return null;
        }

        return EnsureQualifiedName();
    }
}