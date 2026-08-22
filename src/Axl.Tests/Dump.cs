using System.Text;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Tests;

public class Dump(SourceFileView source)
{
    private readonly StringBuilder _builder = new();
    
    public Dump Add(IEnumerable<Diagnostic> diagnostics)
    {
        if (_builder.Length > 0)
            _builder.AppendLine();
        
        foreach (var diag in diagnostics)
        {
            var spans = string.Join(", ", diag.Locations.Select(location => location.Span));
            _builder.AppendLine(
                $"{diag.DefaultSeverity.ToString().ToUpper()} {diag.Id}@{spans}: {diag.Message.ToLiteralString()}");
            foreach (var related in diag.Related)
                _builder.AppendLine($"   related@{related.Location.Span}: {related.Label}");
        }

        return this;
    }

    public Dump Add(IEnumerable<Token> tokens, bool filterTrivia)
    {
        if (_builder.Length > 0)
            _builder.AppendLine();
        
        if (filterTrivia)
            tokens = tokens.Where(t => !t.Kind.IsTrivia);
        
        foreach (var token in tokens)
        {
            if (token.Kind is TokenKind.Eof)
            {
                _builder.AppendLine("- Eof");
                continue;
            }
            
            _builder.Append($"- {token.Kind}: \"");
            AddLiteralString(source.GetText(token.FullSpan));
            _builder.Append('"');

            switch (token)
            {
                case NumberLiteralToken numberLiteral:
                    _builder.Append($" body=\"{numberLiteral.Body}\" suffix={numberLiteral.Suffix}");
                    break;
                case StringTextToken stringText:
                    _builder.Append($" processed=\"{stringText.ProcessedText}\"");
                    break;
            }

            _builder.AppendLine();
        }

        return this;
    }

    private void AddPartOfSyntaxTree(SyntaxElement element, bool filterTrivia, bool filterEof, int depth, bool raw)
    {
        string prefix;
        if (depth == 0)
            prefix = "";
        else if (depth == 1)
            prefix = "· ";
        else
        {
            prefix = string.Join("", Enumerable.Repeat("· ", depth - 1));
            prefix += "· ";
        }
        
        switch (element)
        {
            case Token token:
            {
                if (filterTrivia && token.Kind.IsTrivia)
                    return;
                if (filterEof && token.Kind is TokenKind.Eof)
                    return;

                _builder.Append(prefix);
                _builder.AppendLine(token.IsMissing
                    ? $"{GetMissingDisplayText(token)}"
                    : $"\'{source.GetText(token.FullSpan)}\'");
                break;

                string GetMissingDisplayText(Token tkn)
                    => tkn.Kind is TokenKind.Identifier ? "??ID" : $"??{tkn.Kind.DisplayName}";
            }

            case SyntaxNode node:
            {
                if (!raw)
                {
                    _builder.Append(prefix);
                    _builder.Append($"{node.Kind}");
                }

                var children = filterTrivia
                    ? [.. node.Children.Where(t => t is Token { Kind.IsTrivia: false } or SyntaxNode)]
                    : node.Children;

                if (children.All(e => e is Token { IsMissing: false }))
                {
                    foreach (var child in children.OfType<Token>())
                    {
                        _builder.Append(" \'");
                        AddLiteralString(source.GetText(child.FullSpan));
                        _builder.Append("\'");
                    }

                    _builder.AppendLine();
                }
                else
                {
                    _builder.AppendLine();
                    foreach (var child in children)
                        AddPartOfSyntaxTree(child, filterTrivia, filterEof, raw ? 0: depth + 1, raw: false);
                }
                break;
            }
        }
    }

    public Dump AddChildren(SyntaxNode node, bool filterTrivia, bool filterEof)
    {
        if (_builder.Length > 0)
            _builder.AppendLine();
        
        AddPartOfSyntaxTree(node, filterTrivia, filterEof, depth: 0, raw: true);

        return this;
    }
    
    public Dump Add(SyntaxNode node, bool filterTrivia, bool filterEof)
    {
        if (_builder.Length > 0)
            _builder.AppendLine();
        
        AddPartOfSyntaxTree(node, filterTrivia, filterEof, depth: 0, raw: false);

        return this;
    }

    
    public Dump AddSExpr(SyntaxNode node)
    {
        if (_builder.Length > 0)
            _builder.AppendLine();

        AddSExprInner(node);
        return this;
    }

    private void AddSExprInner(SyntaxElement element)
    {
        switch (element)
        {
            case Token { Kind.IsTrivia: true }:
            case Token { Kind: TokenKind.Eof }:
                return;

            case Token token:
                AddLiteralString(source.GetText(token.FullSpan));
                _builder.Append(' ');
                break;

            case SyntaxNode node:
                var nonTriviaChildren = node.Children
                    .Where(t => t is SyntaxNode or Token { Kind.IsTrivia: false })
                    .ToList();
                
                // --- GroupExpr: Print only inner node
                if (node.Kind is SyntaxKind.GroupExpr)
                {
                    if (nonTriviaChildren is [Token { Kind: TokenKind.OpenParen }, SyntaxNode innerNode, ..])
                    {
                        AddSExprInner(innerNode);
                        return;
                    }
                }

                // --- Single Token
                if (nonTriviaChildren is [Token singleToken])
                {
                    AddSExprInner(singleToken);
                    return;
                }

                // --- Node
                _builder.Append('(');
                    
                foreach (var child in nonTriviaChildren)
                    AddSExprInner(child);
                
                if (_builder.Length > 0 && _builder[^1] is ' ')
                    _builder.Remove(_builder.Length - 1, 1);
                _builder.Append(") ");
                break;

        }
    }
    
    
    
    
    private void AddLiteralString(ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            switch (c)
            {
                case '\0': _builder.Append(@"\0");  break;
                case '\n': _builder.Append(@"\n");  break;
                case '\r': _builder.Append(@"\r");  break;
                case '\t': _builder.Append(@"\t");  break;
                default:
                    if (c is < ' ' or > '~')
                        _builder.Append("\\u").Append(((int)c).ToString("X4"));
                    else
                        _builder.Append(c);
                    break;
            }
        }
    }
    

    public override string ToString()
        => _builder.ToString().Trim();
}