using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatUsingDirective()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.UsingKw));

        var usingDirective = _scanner.Open();
        
        _scanner.EatKnown(TokenKind.UsingKw);
        EnsurePath(ExpectedSyntax.ModuleName);
        EnsureToken(TokenKind.Semicolon);
        
        return _scanner.Close(usingDirective, SyntaxKind.UsingDirective);
    }
}