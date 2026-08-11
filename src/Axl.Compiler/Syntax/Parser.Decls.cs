using System.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local

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
                RecoverTo(moduleBodyAnchor, expectedCategory: SyntaxCategory.Member);

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
        while (_scanner.Peek().Kind is TokenKind.PublicKw or TokenKind.PrivateKw)
            _scanner.EatToken();

        // --- Actual declaration
        if (_scanner.IsAt(TokenKind.NativeKw) || _scanner.IsAt(TokenKind.FnKw))
            return EatFnDecl(anchor, decl);

        // --- No actual declaration found.
        // Wrap modifiers into an error.
        ReportMissing(TokenKind.FnKw);
        return _scanner.Close(decl, SyntaxKind.Error);
    }

    private MarkClose EatFnDecl(Anchor anchor, MarkOpen fnDecl)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.NativeKw) || _scanner.IsAt(TokenKind.FnKw));

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
            {
                // TODO: Expected string?
                ReportMissing(expected: SyntaxCategory.Expr);
            }
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

            // Semicolon rule: If last is BraceClose, ";" can be omitted, but is
            // allowed. Otherwise, it's required.
            if (_scanner.Last?.Kind is TokenKind.CloseBrace)
            {
                if (_scanner.IsAt(TokenKind.Semicolon))
                    _scanner.EatToken(TokenKind.Semicolon);
            }
            else
                ExpectToken(TokenKind.Semicolon);
        }

        return _scanner.Close(fnDecl, SyntaxKind.FnDecl);
    }

    private MarkClose EatParamList(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.OpenParen));

        var paramList = _scanner.Open();
        _scanner.EatToken(TokenKind.OpenParen);

        // --- Special-case `( )`
        if (_scanner.IsAt(TokenKind.CloseParen))
        {
            _scanner.EatToken(TokenKind.CloseParen);
            return _scanner.Close(paramList, SyntaxKind.ParamList);
        }

        // --- Expect parameters
        var paramAnchor = anchor | TokenSet.Of(TokenKind.CloseParen, TokenKind.Comma);
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            // --- Expr
            ExpectParam();

            // --- Confused?
            RecoverTo(paramAnchor, expectedKind: TokenKind.Comma);

            // --- Next token
            if (_scanner.IsAt(TokenKind.Comma))
            {
                _scanner.EatToken(TokenKind.Comma);

                // Expect another parameter
                continue;
            }

            if (_scanner.IsAt(TokenKind.CloseParen) ||
                _scanner.IsAt(anchor))
            {
                break;
            }

            // Every branch continues or breaks.
            throw new UnreachableException();
        }

        // --- Expect `)`
        ExpectToken(TokenKind.CloseParen);
        return _scanner.Close(paramList, SyntaxKind.ParamList);

        MarkClose? ExpectParam()
        {
            if (!_scanner.IsAt(TokenKind.Identifier))
            {
                ReportMissing(TokenKind.Identifier);
                return null;
            }

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
