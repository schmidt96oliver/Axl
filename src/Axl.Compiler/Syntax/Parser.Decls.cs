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

        EnsureQualifiedName(ExpectedSyntax.ModuleName);

        // --- ";" means it's a global declaration
        if (_scanner.IsAt(TokenKind.Semicolon))
        {
            _scanner.EatKnownToken(TokenKind.Semicolon);
            return _scanner.Close(moduleDecl, SyntaxKind.GlobalModuleDecl);
        }

        // --- Missing { }?
        if (!EnsureToken(TokenKind.OpenBrace))
        {
            _scanner.MakeToken(TokenKind.CloseBrace);
            return _scanner.Close(moduleDecl, SyntaxKind.ModuleDecl);
        }

        // --- Eat members
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

        EnsureToken(TokenKind.CloseBrace);
        return _scanner.Close(moduleDecl, SyntaxKind.ModuleDecl);
    }

    private MarkClose EatUsingDecl()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.UsingKw));

        var usingDecl = _scanner.Open();
        _scanner.EatKnownToken(TokenKind.UsingKw);
        EnsureQualifiedName(ExpectedSyntax.ModuleName);
        EnsureToken(TokenKind.Semicolon);
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

        // --- "fn"
        if (!_scanner.IsAt(TokenKind.FnKw))
        {
            ReportMissing(TokenKind.FnKw);
            
            // Since we anchor on ";" in EatNativeDecl, we need to handle
            // that here. It was probably meant to close a native fn declaration,
            // so just eat it.
            if (_scanner.IsAt(TokenKind.Semicolon))
                _scanner.EatKnownToken(TokenKind.Semicolon);
            
            return _scanner.Close(fnDecl, SyntaxKind.Error);
        }

        _scanner.EatKnownToken(TokenKind.FnKw);
        EnsureIdName();

        // Inside ParamList, we can continue from "{" or "->"
        var paramListAnchor = anchor | TokenKind.OpenBrace | TokenKind.RightArrow | TokenKind.Semicolon;
        EnsureParamList(paramListAnchor);

        // --- Return type
        if (_scanner.IsAt(TokenKind.RightArrow))
        {
            _scanner.EatKnownToken(TokenKind.RightArrow);

            // --- Special case "never" keyword
            if (_scanner.Peek() is IdentifierToken { Identifier: "never" })
                _scanner.EatTokenAs(TokenKind.NeverKw);
            else
                EnsureTypeName();
        }

        if (hasNativeClause)
            EnsureToken(TokenKind.Semicolon);
        else
        {
            EnsureBody(anchor);
            EnsureSemicolonIfRequired(ownsBody: true);
        }

        return _scanner.Close(fnDecl, SyntaxKind.FnDecl);
    }

    private MarkClose EatNativeDecl(Anchor anchor)
    {
        // We can handle ")".
        var nativeClauseAnchor = anchor | TokenKind.CloseParen;

        var nativeClause = _scanner.Open();
        _scanner.EatKnownToken(TokenKind.NativeKw);

        EnsureToken(TokenKind.OpenParen);
        EnsureStringExpr(nativeClauseAnchor);
        EnsureToken(TokenKind.CloseParen);

        return _scanner.Close(nativeClause, SyntaxKind.NativeClause);
    }

    private MarkClose EnsureParamList(Anchor anchor)
    {
        return EnsureDelimitedList(anchor,
            openToken: TokenKind.OpenParen, 
            closeToken: TokenKind.CloseParen,
            listKind: SyntaxKind.ParamList,
            itemFirst: TokenSet.Of(TokenKind.Identifier),
            ensureItem: EnsureParam, 
            expectedOpenSyntax: ExpectedSyntax.ParamList,
            expectedItemSyntax: ExpectedSyntax.Param);
        
        MarkClose EnsureParam(Anchor _)
        {
            var param = _scanner.Open();
            
            EnsureIdName(ExpectedSyntax.Param);
            if (_scanner.IsAt(TokenKind.Colon))
            {
                _scanner.EatKnownToken(TokenKind.Colon);
                EnsureTypeName();
            }

            return _scanner.Close(param, SyntaxKind.Param);
        }
    }
}