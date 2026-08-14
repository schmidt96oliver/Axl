using System.Diagnostics;
using Axl.Compiler.Diagnostics;

// ReSharper disable UnusedMethodReturnValue.Local

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose ParseTypeName()
    {
        if (_scanner.IsAt(FirstSet.NativeTypeName))
            return EatNativeTypeName();
        if (_scanner.IsAt(TokenKind.Identifier))
            return ParseQualifiedName();
        
        // --- Missing
        ReportMissing(ExpectedSyntax.TypeName);
        return ConstructMissingIdName();
    }
    
    private MarkClose ParseQualifiedName()
    {
        if (!_scanner.IsAt(TokenKind.Identifier))
        {
            ReportMissing(ExpectedSyntax.TypeName);
            return ConstructMissingIdName();
        }
        
        var typeExpr = _scanner.Open();
        ParseIdName();

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

    private MarkClose ParseIdName()
    {
        var idName = _scanner.Open();
        ExpectToken(TokenKind.Identifier);
        return _scanner.Close(idName, SyntaxKind.IdName);
    }

    private MarkClose ConstructMissingIdName()
    {
        var idName = _scanner.Open();
        _scanner.AddMissingToken(TokenKind.Identifier);
        return _scanner.Close(idName, SyntaxKind.IdName);
    }
}