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
        _scanner.EatKnownToken(TokenKind.ModuleKw);

        ExpectQualifiedName();

        // --- ";" means its a global declaration
        if (_scanner.IsAt(TokenKind.Semicolon))
        {
            _scanner.EatKnownToken(TokenKind.Semicolon);
            return _scanner.Close(moduleDecl, SyntaxKind.GlobalModuleDecl);
        }

        // --- "}" means we parse the entire block
        if (_scanner.IsAt(TokenKind.OpenBrace))
        {
            _scanner.EatKnownToken(TokenKind.OpenBrace);

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
        _scanner.EatKnownToken(TokenKind.UsingKw);
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

        // --- Native Clause
        var hasNativeClause = false;
        if (_scanner.IsAt(TokenKind.NativeKw))
        {
            // We can recover from:
            // - "fn" starts fn
            // - ";" ends fn declaration directly (eaten, if fn was missing)
            EatNativeDecl(anchor | TokenKind.FnKw | TokenKind.Semicolon);
            hasNativeClause = true;
        }

        // --- FnDecl
        if (!ExpectToken(TokenKind.FnKw))
        {
            // Since we anchor on ";" in EatNativeDecl, we need to handle
            // that here. It was probably meant to close a native fn declaration,
            // so just eat it.
            if (_scanner.IsAt(TokenKind.Semicolon))
                _scanner.EatKnownToken(TokenKind.Semicolon);
            
            return _scanner.Close(fnDecl, SyntaxKind.Error);
        }
        ExpectIdName();

        // Inside ParamList, we can continue from "{" or "->"
        var paramListAnchor = anchor | TokenKind.OpenBrace | TokenKind.RightArrow | TokenKind.Semicolon;
        ExpectParamList(paramListAnchor);

        // --- Return type
        if (_scanner.IsAt(TokenKind.RightArrow))
        {
            _scanner.EatKnownToken(TokenKind.RightArrow);

            // --- Special case "never" keyword
            if (_scanner.Peek() is IdentifierToken { Identifier: "never" })
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

    private MarkClose EatNativeDecl(Anchor anchor)
    {
        // We can handle ")".
        var nativeClauseAnchor = anchor | TokenKind.CloseParen;
            
        var nativeClause = _scanner.Open();
        _scanner.EatKnownToken(TokenKind.NativeKw);

        // If we don't get "(", break off and leave the
        // rest to the enclosing function.
        if (_scanner.IsAt(TokenKind.OpenParen))
        {
            _scanner.EatKnownToken(TokenKind.OpenParen);
            if (_scanner.IsAt(TokenKind.StringStart))
                EatStringExpr(nativeClauseAnchor);
            else
            {
                if (!RecoverTo(nativeClauseAnchor, ExpectedSyntax.String))
                    ReportMissing(ExpectedSyntax.String);
            }
            ExpectToken(TokenKind.CloseParen);
        }
        else 
            ReportMissing(TokenKind.OpenParen);
        
        return _scanner.Close(nativeClause, SyntaxKind.NativeClause);
    }
    
    private MarkClose EatParamList(Anchor anchor)
    {
        return EatDelimitedList(anchor,
            openToken: TokenKind.OpenParen, 
            closeToken: TokenKind.CloseParen,
            listKind: SyntaxKind.ParamList,
            itemFirst: TokenSet.Of(TokenKind.Identifier),
            expectedItemSyntax: ExpectedSyntax.Param,
            parseItem: ParseParam);
        
        MarkClose ParseParam(Anchor _)
        {
            var param = _scanner.Open();
            if (!_scanner.IsAt(TokenKind.Identifier))
                ReportMissing(ExpectedSyntax.Param);
            
            ParseIdName();
            if (_scanner.IsAt(TokenKind.Colon))
            {
                _scanner.EatKnownToken(TokenKind.Colon);
                ParseTypeName();
            }

            return _scanner.Close(param, SyntaxKind.Param);
        }
    }
}