using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ClassCannotBeInstantiated

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatMember(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.Member));

        // --- Dispatch
        if (_scanner.IsAt(TokenKind.FunKw))
            return EatFnDecl(anchor);
        if (_scanner.IsAt(TokenKind.NativeKw))
            return EatNativeFnDecl(anchor);

        throw new UnreachableException($"{nameof(FirstSet.Member)} too large");
    }

    private MarkClose EatModuleDecl()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.ModuleKw));

        var moduleDecl = _scanner.Open();
        
        _scanner.EatKnown(TokenKind.ModuleKw);
        EnsurePath(ExpectedSyntax.ModuleName);
        EnsureToken(TokenKind.Semicolon);
        
        return _scanner.Close(moduleDecl, SyntaxKind.ModuleDecl);
    }

    private MarkClose EatFnDecl(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.FunKw));

        var fnDecl = _scanner.Open();
        
        _scanner.EatKnown(TokenKind.FunKw);
        EnsureIdName();

        EnsureParamList(anchor | TokenKind.OpenBrace | TokenKind.Colon |
                        TokenKind.Semicolon | TokenKind.Equal);

        if (_scanner.IsAt(TokenKind.Colon))
            EatReturnTypeAnnotation();

        EnsureFnBody(anchor);
        
        return _scanner.Close(fnDecl, SyntaxKind.FunDecl);
    }

    private MarkClose EnsureFnBody(Anchor anchor)
    {
        var fnBody = _scanner.Open();
        
        if (_scanner.IsAt(TokenKind.Equal))
        {
            _scanner.EatKnown(TokenKind.Equal);
            
            EnsureExpr(anchor);
            EnsureToken(TokenKind.Semicolon);
        }
        else
        {
            EnsureBlock(anchor, ExpectedSyntax.FunBody);

            // Allow a semicolon if it's there for resilience.
            if (_scanner.IsAt(TokenKind.Semicolon))
                _scanner.Eat();
        }

        return _scanner.Close(fnBody, SyntaxKind.FunBody);
    }

    private MarkClose EatNativeFnDecl(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(TokenKind.NativeKw));

        var fnDecl = _scanner.Open();
        
        EatNativeClause(anchor | TokenKind.FunKw | TokenKind.Semicolon);

        if (!_scanner.IsAt(TokenKind.FunKw))
        {
            _scanner.ReportMissingTokenHere(TokenKind.FunKw);

            // Since we anchor on ";" in EatNativeDecl, we need to handle
            // that here. It was probably meant to close a native fun declaration,
            // so just eat it.
            if (_scanner.IsAt(TokenKind.Semicolon))
                _scanner.EatKnown(TokenKind.Semicolon);

            return _scanner.Close(fnDecl, SyntaxKind.Garbage);
        }

        _scanner.EatKnown(TokenKind.FunKw);
        EnsureIdName();

        EnsureParamList(anchor | TokenKind.Colon | TokenKind.Semicolon);

        if (_scanner.IsAt(TokenKind.Colon))
            EatReturnTypeAnnotation();

        EnsureToken(TokenKind.Semicolon);

        return _scanner.Close(fnDecl, SyntaxKind.NativeFunDecl);
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
        Debug.Assert(_scanner.IsAt(TokenKind.Colon));

        var returnTypeAnnotation = _scanner.Open();
        _scanner.EatKnown(TokenKind.Colon);

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
        // cases like 'fun A( : i32)' gracefully.
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
            // parameter. But if it looks like a path, like 'fun A(a a.b)',
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