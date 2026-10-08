using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ClassCannotBeInstantiated

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatStructDecl(Anchor anchor)
    {
        var structDecl = _scanner.Open();
        _scanner.EatKnown(TokenKind.StructKw);

        EnsureIdName();

        if (_scanner.IsAt(TokenKind.Semicolon))
            _scanner.Eat();
        else
            EnsureStructBody(anchor);

        return _scanner.Close(structDecl, SyntaxKind.StructDecl);
    }

    private MarkClose EnsureStructBody(Anchor anchor)
    {
        var body = _scanner.Open();
        EnsureToken(TokenKind.OpenBrace);

        var bodyAnchor = anchor | TokenKind.PubKw | TokenKind.CloseBrace | TokenKind.Semicolon;
        
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(TokenKind.PubKw))
            {
                if (_scanner.Peek(1).Kind is TokenKind.VarKw or TokenKind.StaticKw or TokenKind.FunKw)
                    EatFunDecl(bodyAnchor);
                else
                    EatFieldDecl();
            }
            
            // --- Members
            else if (_scanner.IsAt(FirstSet.FieldDecl))
                EatFieldDecl();
            else if (_scanner.IsAt(FirstSet.FunDecl))
                EatFunDecl(bodyAnchor);

            // --- lone ";" special case
            else if (_scanner.IsAt(TokenKind.Semicolon))
                _scanner.EatIntoGarbageAndReport(ExpectedSyntax.Stmt);
            
            // --- Closing tokens
            else if (_scanner.IsAt(TokenKind.CloseBrace))
                break;
            else if (_scanner.IsAt(anchor))
                break;

            // --- Garbage
            else
            {
                var recovered = RecoverToAndReport(bodyAnchor | FirstSet.FieldDecl, ExpectedSyntax.Stmt);

                // If it's followed by a ';', eat it into an error silently.
                if (recovered && _scanner.IsAt(TokenKind.Semicolon))
                    _scanner.EatInto(SyntaxKind.Garbage);
            }
        }
        
        EnsureToken(TokenKind.CloseBrace);
        return _scanner.Close(body, SyntaxKind.StructBody);
    }

    private MarkClose EatFieldDecl()
    {
        Debug.Assert(_scanner.IsAt(FirstSet.FieldDecl));

        var field = _scanner.Open();
        
        if (_scanner.IsAt(TokenKind.PubKw))
            _scanner.Eat();

        EnsureIdName(ExpectedSyntax.FieldName);
        EnsureTypeAnnotation();
        EnsureToken(TokenKind.Semicolon);
        return _scanner.Close(field, SyntaxKind.FieldDecl);
    }

    private MarkClose EatNamespaceDecl()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.NamespaceKw));

        var namespaceDecl = _scanner.Open();

        _scanner.EatKnown(TokenKind.NamespaceKw);
        EnsureTypeName(ExpectedSyntax.NamespaceName);
        EnsureToken(TokenKind.Semicolon);

        return _scanner.Close(namespaceDecl, SyntaxKind.NamespaceDecl);
    }

    private MarkClose EatFunDecl(Anchor anchor)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.FunDecl));

        var fnDecl = _scanner.Open();

        _scanner.EatIfPresent(TokenKind.PubKw);
        _scanner.EatIfPresent(TokenKind.StaticKw);
        _scanner.EatIfPresent(TokenKind.VarKw);

        if (!_scanner.IsAt(TokenKind.FunKw))
        {
            _scanner.ReportMissingTokenHere(TokenKind.FunKw);
            return _scanner.Close(fnDecl, SyntaxKind.Garbage);
        }
        
        _scanner.EatKnown(TokenKind.FunKw);
        EnsureIdName();

        EnsureParamList(anchor | TokenKind.OpenBrace | TokenKind.Colon |
                        TokenKind.Semicolon | TokenKind.Equal);

        if (_scanner.IsAt(TokenKind.Colon))
            EnsureTypeAnnotation();

        EnsureFunBody(anchor);

        return _scanner.Close(fnDecl, SyntaxKind.FunDecl);
    }

    private MarkClose EnsureFunBody(Anchor anchor)
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

    private MarkClose EnsureParamList(Anchor anchor)
    {
        // Add ':' and type names to the first set, to catch
        // cases like 'fun A( : i32)' gracefully.
        var itemFirst = TokenSet.Of(TokenKind.Identifier, TokenKind.Colon);

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

            EnsureTypeAnnotation();
            return _scanner.Close(param, SyntaxKind.Param);
        }
    }
}