using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ClassCannotBeInstantiated

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatModuleDecl()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.ModuleKw));

        var moduleDecl = _scanner.Open();
        _scanner.EatToken(TokenKind.ModuleKw);

        ExpectQualifiedName();

        // --- ";" means its a global declaration
        if (_scanner.IsAt(TokenKind.Semicolon))
        {
            _scanner.EatToken(TokenKind.Semicolon);
            return _scanner.Close(moduleDecl, SyntaxKind.GlobalModuleDecl);
        }

        // --- "}" means we parse the entire block
        if (_scanner.IsAt(TokenKind.OpenBrace))
        {
            _scanner.EatToken(TokenKind.OpenBrace);

            var moduleBodyAnchor = Anchor.Forced | FirstSet.MemberDecl | TokenKind.CloseBrace;
            foreach (var _ in _scanner.MustEatEachIteration())
            {
                RecoverTo(moduleBodyAnchor, ExpectedSyntax.Member);

                if (_scanner.IsAt(TokenKind.ModuleKw))
                    EatModuleDecl();
                else if (_scanner.IsAt(FirstSet.FnDecl))
                    EatMemberDecl(moduleBodyAnchor);
                else
                {
                    // Could be `}` or Eof
                    break;
                }
            }

            ExpectToken(TokenKind.CloseBrace);
            return _scanner.Close(moduleDecl, SyntaxKind.ModuleDecl);
        }

        // --- Anything else is garbage
        ReportMissing(TokenKind.OpenBrace);
        return _scanner.Close(moduleDecl, SyntaxKind.ModuleDecl);
    }

    private MarkClose EatUsingDecl()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.UsingKw));

        var usingDecl = _scanner.Open();
        _scanner.EatToken(TokenKind.UsingKw);
        ExpectQualifiedName();
        ExpectToken(TokenKind.Semicolon);
        return _scanner.Close(usingDecl, SyntaxKind.UsingDecl);
    }

    private MarkClose EatMemberDecl(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.MemberDecl));

        var decl = _scanner.Open();

        // --- Modifier List
        while (_scanner.IsAt(FirstSet.Modifier))
            _scanner.EatToken();

        // --- Actual declaration
        if (_scanner.IsAt(FirstSet.FnDeclAfterModifiers))
            return EatFnDecl(anchor, decl);

        // --- No actual declaration found.
        // Wrap modifiers into an error.
        ReportMissing(TokenKind.FnKw);
        return _scanner.Close(decl, SyntaxKind.Error);
    }

    private MarkClose EatFnDecl(Anchor anchor, MarkOpen fnDecl)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.FnDeclAfterModifiers));

        var fnDeclAnchor = anchor | TokenKind.FnKw;

        // --- Native Clause
        var hasNativeClause = false;
        if (_scanner.IsAt(TokenKind.NativeKw))
        {
            var nativeClause = _scanner.Open();
            _scanner.EatToken(TokenKind.NativeKw);
            
            ExpectToken(TokenKind.OpenParen);
            if (_scanner.IsAt(TokenKind.StringStart))
                EatStringExpr(fnDeclAnchor | TokenKind.CloseParen);
            else
                ReportMissing(expected: ExpectedSyntax.String);
            
            ExpectToken(TokenKind.CloseParen);

            hasNativeClause = true;
            _scanner.Close(nativeClause, SyntaxKind.NativeClause);
        }

        // --- FnDecl
        ExpectToken(TokenKind.FnKw);
        ExpectIdName();
        ExpectParamList(anchor);

        // --- Return type
        if (_scanner.IsAt(TokenKind.RightArrow))
        {
            _scanner.EatToken(TokenKind.RightArrow);

            // --- Special case "never" keyword
            if (_scanner.Peek() is IdentifierToken { Id.Text: "never" })
                _scanner.EatTokenAs(TokenKind.NeverKw);
            else
                ExpectTypeName();
        }

        if (hasNativeClause)
            ExpectToken(TokenKind.Semicolon);
        else
        {
            ExpectBody(anchor);
            EatSemicolonIfRequired(ownsBody: true);
        }

        return _scanner.Close(fnDecl, SyntaxKind.FnDecl);
    }

    private MarkClose EatParamList(Anchor anchor)
    {
        return EatDelimitedList(anchor,
            openToken: TokenKind.OpenParen, 
            closeToken: TokenKind.CloseParen,
            listKind: SyntaxKind.ParamList,
            itemFirst: TokenSet.Of(TokenKind.Identifier),
            expectedItemSyntax: ExpectedSyntax.Param,
            eatItem: EatParam);
        
        MarkClose EatParam(Anchor _)
        {
            var param = _scanner.Open();
            EatIdName();
            if (_scanner.IsAt(TokenKind.Colon))
            {
                _scanner.EatToken(TokenKind.Colon);
                ExpectTypeName();
            }

            return _scanner.Close(param, SyntaxKind.Param);
        }
    }
}