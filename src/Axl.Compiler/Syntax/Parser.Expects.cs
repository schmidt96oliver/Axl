using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    /// <summary>
    /// Eats and returns next token, if it has <paramref name="expectedKind"/>.
    /// Otherwise, reports <see cref="Diagnostic.MissingToken"/> and returns <c>null</c>.
    /// </summary>
    private Token? ExpectToken(TokenKind expectedKind)
    {
        if (!_scanner.IsAt(expectedKind))
        {
            ReportMissing(expectedKind);
            return null;
        }

        return _scanner.EatToken(expectedKind);
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

        return EatExpr(anchor);
    }

    private MarkClose? ExpectBody(Anchor anchor)
    {
        if (!_scanner.IsAt(TokenKind.OpenBrace) && !_scanner.IsAt(TokenKind.RightDoubleArrow))
        {
            ReportMissing(ExpectedSyntax.Body);
            return null;
        }

        return _scanner.IsAt(TokenKind.OpenBrace)
            ? EatBlock(anchor)
            : EatArm(anchor);
    }

    private MarkClose? ExpectTypeName()
    {
        if (!_scanner.IsAt(FirstSet.TypeName))
        {
            ReportMissing(ExpectedSyntax.TypeName);
            return null;
        }

        if (_scanner.IsAt(FirstSet.NativeTypeName))
            return EatNativeTypeName();
        else
            return EatQualifiedName();
    }

    private MarkClose? ExpectIdName()
    {
        if (!_scanner.IsAt(TokenKind.Identifier))
        {
            ReportMissing(TokenKind.Identifier);
            return null;
        }

        return EatIdName();
    }

    private MarkClose? ExpectQualifiedName()
    {
        if (!_scanner.IsAt(FirstSet.QualifiedName))
        {
            ReportMissing(ExpectedSyntax.TypeName);
            return null;
        }

        return EatQualifiedName();
    }

    private MarkClose? ExpectParamList(Anchor anchor)
    {
        if (!_scanner.IsAt(TokenKind.OpenParen))
        {
            ReportMissing(ExpectedSyntax.ParamList);
            return null;
        }

        return EatParamList(anchor);
    }
}