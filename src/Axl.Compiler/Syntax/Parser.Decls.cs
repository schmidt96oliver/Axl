using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ClassCannotBeInstantiated

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatMember(Anchor anchor, bool onGlobalScope)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Member));

        var decl = _scanner.Open();
        
        // --- Modifier List
        while (_scanner.IsAt(FirstSet.Modifier))
            _scanner.Eat();
        
        // --- Dispatch
        if (_scanner.IsAt(FirstSet.ModuleDeclAfterModifiers))
            return EatModuleDeclAfterModifiers(decl, anchor, onGlobalScope);
        if (_scanner.IsAt(TokenKind.FnKw))
            return EatFnDeclAfterModifiers(decl, anchor);
        if (_scanner.IsAt(TokenKind.NativeKw))
            return EatNativeFnDeclAfterModifiers(decl, anchor);
        
        // --- Nothing valid
        _scanner.ReportMissingTokenHere(ExpectedSyntax.Member);
        return _scanner.Close(decl, SyntaxKind.Garbage);
    }

    private MarkClose EatModuleDeclAfterModifiers(MarkOpen decl, Anchor anchor, bool onGlobalScope)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.ModuleDeclAfterModifiers));

        _scanner.EatKnown(TokenKind.ModuleKw);

        EnsurePath(ExpectedSyntax.ModuleName);

        // --- ";" means it's a global declaration
        if (_scanner.IsAt(TokenKind.Semicolon))
        {
            _scanner.EatKnown(TokenKind.Semicolon);

            // Eat the entire rest of the file.
            EatModuleMembersInsideBody(anchor);

            return _scanner.Close(decl, SyntaxKind.FileScopedModuleDecl);
        }

        // --- "{" means it's a bodied declaration
        if (_scanner.IsAt(TokenKind.OpenBrace))
        {
            _scanner.EatKnown(TokenKind.OpenBrace);

            EatModuleMembersInsideBody(anchor | TokenKind.CloseBrace);

            EnsureToken(TokenKind.CloseBrace);
            return _scanner.Close(decl, SyntaxKind.ModuleDecl);
        }

        // --- Invalid
        // module A [something unrecognized]
        
        // Input looks like 'module A' and could be meant to be a global
        // module decl or a bodied module. If we're inside another module
        // it surely will be bodied, since global is only allowed on global
        // scope. On global scope, it could be both, but we just default to
        // completing it as a global declaration as a heuristic.

        if (onGlobalScope)
        {
            EnsureToken(TokenKind.Semicolon);
            EatModuleMembersInsideBody(Anchor.Forced);
            return _scanner.Close(decl, SyntaxKind.FileScopedModuleDecl);
        }
        else
        {
            _scanner.MakeAndReport(TokenKind.OpenBrace);
            _scanner.MakeAndReport(TokenKind.CloseBrace);
            return _scanner.Close(decl, SyntaxKind.ModuleDecl);
        }
    }

    private void EatModuleMembersInsideBody(Anchor anchor)
    {
        var moduleBodyAnchor = anchor | TokenKind.UsingKw | FirstSet.Member;
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            RecoverToAndReport(moduleBodyAnchor, ExpectedSyntax.Member);

            if (_scanner.IsAt(FirstSet.Member))
                EatMember(moduleBodyAnchor, onGlobalScope: false);
            else if (_scanner.IsAt(TokenKind.UsingKw))
                EatUsingDirective();
            else
                break;
        }
    }


    private MarkClose EatFnDeclAfterModifiers(MarkOpen decl, Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.FnKw));

        _scanner.EatKnown(TokenKind.FnKw);
        EnsureIdName();

        // Inside ParamList, we can continue from "{" or "->"
        EnsureParamList(anchor | FirstSet.Body | TokenKind.RightArrow | TokenKind.Semicolon);

        if (_scanner.IsAt(TokenKind.RightArrow))
            EatReturnTypeAnnotation();

        EnsureBody(anchor);
        EnsureSemicolonIfRequired(ownsBody: true);

        return _scanner.Close(decl, SyntaxKind.FnDecl);
    }

    private MarkClose EatNativeFnDeclAfterModifiers(MarkOpen decl, Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.NativeKw));

        EatNativeClause(anchor | TokenKind.FnKw | TokenKind.Semicolon);

        if (!_scanner.IsAt(TokenKind.FnKw))
        {
            _scanner.ReportMissingTokenHere(TokenKind.FnKw);

            // Since we anchor on ";" in EatNativeDecl, we need to handle
            // that here. It was probably meant to close a native fn declaration,
            // so just eat it.
            if (_scanner.IsAt(TokenKind.Semicolon))
                _scanner.EatKnown(TokenKind.Semicolon);

            return _scanner.Close(decl, SyntaxKind.Garbage);
        }

        _scanner.EatKnown(TokenKind.FnKw);
        EnsureIdName();

        EnsureParamList(anchor | TokenKind.RightArrow | TokenKind.Semicolon);

        if (_scanner.IsAt(TokenKind.RightArrow))
            EatReturnTypeAnnotation();

        EnsureToken(TokenKind.Semicolon);

        return _scanner.Close(decl, SyntaxKind.NativeFnDecl);
    }

    private MarkClose EatNativeClause(Anchor anchor)
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

    private MarkClose EatReturnTypeAnnotation()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.RightArrow));

        var returnTypeAnnotation = _scanner.Open();
        _scanner.EatKnown(TokenKind.RightArrow);

        // --- Special case "never" keyword
        if (_scanner.Peek() is IdentifierToken { Identifier: "never" })
        {
            var nativeTypeName = _scanner.Open();
            _scanner.EatAs(TokenKind.NeverKw);
            _scanner.Close(nativeTypeName, SyntaxKind.NativeTypeName);
        }
        else
            EnsureTypeName();

        return _scanner.Close(returnTypeAnnotation, SyntaxKind.TypeAnnotationClause);
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
            // parameter. But if it looks like a path, like 'fn A(a a.b)',
            // then eat it as a type name for this parameter.
            if (_scanner.IsAt(TokenKind.Colon)
                || _scanner.IsAt(FirstSet.NativeTypeName)
                || _scanner.IsAt(TokenKind.Identifier) && _scanner.Peek(1).Kind is TokenKind.Dot)
            {
                var typeAnnotation = _scanner.Open();
                EnsureToken(TokenKind.Colon);
                EnsureTypeName();
                _scanner.Close(typeAnnotation, SyntaxKind.TypeAnnotationClause);
            }

            return _scanner.Close(param, SyntaxKind.Param);
        }
    }
}