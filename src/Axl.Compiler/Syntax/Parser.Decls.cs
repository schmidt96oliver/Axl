using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ClassCannotBeInstantiated

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatModuleDecl(bool onGlobalScope)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.ModuleKw));

        var moduleDecl = _scanner.Open();
        _scanner.EatKnown(TokenKind.ModuleKw);

        EnsureQualifiedName(ExpectedSyntax.ModuleName);

        // --- ";" means it's a global declaration
        if (_scanner.IsAt(TokenKind.Semicolon))
        {
            _scanner.EatKnown(TokenKind.Semicolon);
            return _scanner.Close(moduleDecl, SyntaxKind.GlobalModuleDecl);
        }

        // --- Missing { }?
        if (!_scanner.IsAt(TokenKind.OpenBrace))
        {
            // Input looks like 'module A' and could be meant to be a global
            // module decl or a bodied module. If we're inside another module
            // it surely will be bodied, since global is only allowed on global
            // scope. On global scope, it could be both, but we just default to
            // completing it as a global declaration as a heuristic.
            
            if (onGlobalScope)
            {
                EnsureToken(TokenKind.Semicolon);
                return _scanner.Close(moduleDecl, SyntaxKind.GlobalModuleDecl);
            }
            else
            {
                _scanner.MakeAndReport(TokenKind.OpenBrace);
                _scanner.MakeAndReport(TokenKind.CloseBrace);
                return _scanner.Close(moduleDecl, SyntaxKind.ModuleDecl);
            }
        }
        
        // --- Eat members
        EnsureToken(TokenKind.OpenBrace);
        var moduleBodyAnchor = Anchor.Forced | FirstSet.MemberDecl | TokenKind.CloseBrace;
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            RecoverToAndReport(moduleBodyAnchor, ExpectedSyntax.Member);

            if (_scanner.IsAt(TokenKind.ModuleKw))
                EatModuleDecl(onGlobalScope: false);
            else if (_scanner.IsAt(FirstSet.FnDecl))
                EatFnDecl(moduleBodyAnchor);
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
        _scanner.EatKnown(TokenKind.UsingKw);
        EnsureQualifiedName(ExpectedSyntax.ModuleName);
        EnsureToken(TokenKind.Semicolon);
        return _scanner.Close(usingDecl, SyntaxKind.UsingDecl);
    }

    private MarkClose EatFnDecl(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.FnDecl));

        var fnDecl = _scanner.Open();

        // --- Modifier List
        while (_scanner.IsAt(FirstSet.Modifier))
            _scanner.Eat();
        
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
            _scanner.ReportMissingTokenHere(TokenKind.FnKw);
            
            // Since we anchor on ";" in EatNativeDecl, we need to handle
            // that here. It was probably meant to close a native fn declaration,
            // so just eat it.
            if (_scanner.IsAt(TokenKind.Semicolon))
                _scanner.EatKnown(TokenKind.Semicolon);
            
            return _scanner.Close(fnDecl, SyntaxKind.Error);
        }

        _scanner.EatKnown(TokenKind.FnKw);
        EnsureIdName();

        // Inside ParamList, we can continue from "{" or "->"
        var paramListAnchor = anchor | TokenKind.OpenBrace | TokenKind.RightArrow | TokenKind.Semicolon;
        EnsureParamList(paramListAnchor);

        // --- Return type
        if (_scanner.IsAt(TokenKind.RightArrow))
        {
            _scanner.EatKnown(TokenKind.RightArrow);

            // --- Special case "never" keyword
            if (_scanner.Peek() is IdentifierToken { Identifier: "never" })
                _scanner.EatAs(TokenKind.NeverKw);
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
        _scanner.EatKnown(TokenKind.NativeKw);

        EnsureToken(TokenKind.OpenParen);
        EnsureStringExpr(nativeClauseAnchor);
        EnsureToken(TokenKind.CloseParen);

        return _scanner.Close(nativeClause, SyntaxKind.NativeClause);
    }

    private MarkClose EnsureParamList(Anchor anchor)
    {
        // Add ':' and type names to the first set, to catch
        // cases like 'fn A( : i32)' gracefully.
        var itemFirst = FirstSet.TypeName | TokenSet.Of(TokenKind.Identifier, TokenKind.Colon);
        
        return EnsureDelimitedList(anchor,
            openToken: TokenKind.OpenParen, 
            closeToken: TokenKind.CloseParen,
            listKind: SyntaxKind.ParamList,
            itemFirst,
            ensureItem: EnsureParam, 
            expectedOpenSyntax: ExpectedSyntax.ParamList,
            expectedItemSyntax: ExpectedSyntax.Param);
        
        MarkClose EnsureParam(Anchor _)
        {
            var param = _scanner.Open();

            EnsureIdName(expectedSyntax: _scanner.IsAt(itemFirst)
                ? TokenKind.Identifier
                : ExpectedSyntax.Param);

            // A plain next identifier must not be eaten, it will become the next
            // parameter. But if it looks like a qualified name, like 'fn A(a a.b)',
            // then eat it as a type name for this parameter.
            if (_scanner.IsAt(TokenKind.Colon)
                || _scanner.IsAt(FirstSet.NativeTypeName)
                || _scanner.IsAt(TokenKind.Identifier) && _scanner.Peek(1).Kind is TokenKind.Dot)
            {
                EnsureToken(TokenKind.Colon);
                EnsureTypeName();
            }

            return _scanner.Close(param, SyntaxKind.Param);
        }
    }
}