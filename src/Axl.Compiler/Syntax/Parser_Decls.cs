using System.Diagnostics;
using Axl.Compiler.Diagnostics;
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ClassCannotBeInstantiated

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private MarkClose EatMember(Anchor anchor, TokenSet allowedAfterHeader)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.StructDecl | FirstSet.FunDecl | FirstSet.FieldDecl));

        var common = _scanner.Open();

        // Annotations
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(FirstSet.Annotation))
                EatAnnotation();
            else
                break;
        }
        
        // Pub
        _scanner.EatIfPresent(TokenKind.PubKw);

        if (!_scanner.IsAt(allowedAfterHeader))
        {
            _scanner.ReportMissingTokenHere(ExpectedSyntax.Member);
            return _scanner.Close(common, SyntaxKind.Garbage);
        }
        
        if (_scanner.IsAt(FirstSet.FunDeclAfterHeader))
            return ContinueEatFunDecl(anchor, common);
        if (_scanner.IsAt(FirstSet.StructDeclAfterHeader))
            return ContinueEatStructDecl(anchor, common);
        if (_scanner.IsAt(FirstSet.FieldDeclAfterHeader))
            return ContinueEatFieldDecl(common);
        
        _scanner.ReportMissingTokenHere(ExpectedSyntax.Member);
        return _scanner.Close(common, SyntaxKind.Garbage);
    }

    private MarkClose EatAnnotation()
    {
        var annotation = _scanner.Open();
        _scanner.EatKnown(TokenKind.At);
        EnsureIdName();
        return _scanner.Close(annotation, SyntaxKind.Annotation);
    }


    private MarkClose ContinueEatStructDecl(Anchor anchor, MarkOpen commonHeader)
    {
        _scanner.EatKnown(TokenKind.StructKw);
        
        EnsureIdName();

        if (_scanner.IsAt(TokenKind.Semicolon))
            _scanner.Eat();
        else
            EnsureStructBody(anchor);

        return _scanner.Close(commonHeader, SyntaxKind.StructDecl);
    }

    private MarkClose EnsureStructBody(Anchor anchor)
    {
        var body = _scanner.Open();
        EnsureToken(TokenKind.OpenBrace);

        var bodyAnchor = anchor | TokenKind.PubKw | TokenKind.CloseBrace | TokenKind.Semicolon;
        
        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(FirstSet.FunDecl | FirstSet.FieldDecl))
                EatMember(bodyAnchor, allowedAfterHeader: FirstSet.FunDeclAfterHeader | FirstSet.FieldDeclAfterHeader);
            
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

    private MarkClose ContinueEatFieldDecl(MarkOpen commonHeader)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.FieldDeclAfterHeader));

        EnsureIdName(ExpectedSyntax.FieldName);
        EnsureTypeAnnotation();
        EnsureToken(TokenKind.Semicolon);
        return _scanner.Close(commonHeader, SyntaxKind.FieldDecl);
    }

    private MarkClose EnsureNamespaceName()
    {
        var namespaceName = _scanner.Open();
        EnsureIdName(ExpectedSyntax.NamespaceName);

        foreach (var _ in _scanner.MustEatEachIteration())
        {
            if (_scanner.IsAt(TokenKind.Dot))
            {
                _scanner.Eat();
                EnsureIdName();
            }
            else
                break;
        }

        return _scanner.Close(namespaceName, SyntaxKind.NamespaceName);
    }

    private MarkClose EatNamespaceDecl()
    {
        Debug.Assert(_scanner.IsAt(TokenKind.NamespaceKw));

        var namespaceDecl = _scanner.Open();

        _scanner.EatKnown(TokenKind.NamespaceKw);
        EnsureNamespaceName();
        EnsureToken(TokenKind.Semicolon);

        return _scanner.Close(namespaceDecl, SyntaxKind.NamespaceDecl);
    }

    private MarkClose ContinueEatFunDecl(Anchor anchor, MarkOpen commonHeader)
    {
        Debug.Assert(_scanner.IsAt(FirstSet.FunDeclAfterHeader));

        _scanner.EatIfPresent(TokenKind.StaticKw);
        _scanner.EatIfPresent(TokenKind.VarKw);

        if (!_scanner.IsAt(TokenKind.FunKw))
        {
            _scanner.ReportMissingTokenHere(TokenKind.FunKw);
            return _scanner.Close(commonHeader, SyntaxKind.Garbage);
        }
        
        _scanner.EatKnown(TokenKind.FunKw);
        EnsureFunName();

        EnsureParamList(anchor | TokenKind.OpenBrace | TokenKind.Colon |
                        TokenKind.Semicolon | TokenKind.Equal);

        if (_scanner.IsAt(TokenKind.Colon))
            EnsureTypeAnnotation();

        EnsureFunBody(anchor);

        return _scanner.Close(commonHeader, SyntaxKind.FunDecl);
    }

    private MarkClose EnsureFunName()
    {
        var funName = _scanner.Open();
        if (_scanner.IsAt(FirstSet.FunName))
            _scanner.Eat();
        else
            EnsureToken(TokenKind.Identifier, ExpectedSyntax.FunName);
        return _scanner.Close(funName, SyntaxKind.FunName);
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