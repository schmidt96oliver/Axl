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
        foreach (var diag in diagnostics)
        {
            _builder.AppendLine(
                $"{diag.DefaultSeverity.ToString().ToUpper()} {diag.Id}@{diag.Location.Span}: {diag.Message.ToLiteralString()}");
            foreach (var related in diag.Related)
                _builder.AppendLine($"   related@{related.Location.Span}: {related.Label}");
        }

        return this;
    }

    public Dump Add(IEnumerable<Token> tokens, bool filterTrivia)
    {
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
            AddLiteralString(source.GetText(token.Span));
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

    private void AddPartOfSyntaxTree(SyntaxElement element, bool filterTrivia, string prefix)
    {
        switch (element)
        {
            case Token token:
            {
                if (filterTrivia && token.Kind.IsTrivia)
                    return;

                _builder.Append(prefix);
                _builder.Append('\'');
                AddLiteralString(source.GetText(token.Span));
                _builder.AppendLine("\'");
                break;
            }
                
            case SyntaxNode node:
            {
                _builder.Append(prefix);
                _builder.Append($"{node.Kind}");

                var children = filterTrivia
                    ? [.. node.Children.Where(t => t is Token { Kind.IsTrivia: false } or SyntaxNode)]
                    : node.Children;
                
                if (children is [Token token])
                {
                    _builder.Append(": \'");
                    AddLiteralString(source.GetText(token.Span));
                    _builder.AppendLine("\'");
                }
                else
                {
                    _builder.AppendLine();
                    foreach (var child in children)
                        AddPartOfSyntaxTree(child, filterTrivia, prefix + "| ");
                }
                break;
            }
        }
    }
    
    public Dump Add(SyntaxNode node, bool filterTrivia)
    {
        AddPartOfSyntaxTree(node, filterTrivia, "");

        return this;
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