using AxlCompiler.Syntax;
using AxlCompiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;
using Shouldly;

namespace Axl.Tests.Compiler.Syntax;

public class SyntaxElementTests
{
    [Theory]
    [Corpus]
    public void CorpusTraverse_ChildIsSublist(string path)
    {
        var tree = Parser.Parse(SourceText.LoadFile(path));
        var elements = tree.FileSyntax.Traverse().ToList();

        for (var i = 0; i < elements.Count; i++)
        {
            if (elements[i] is not SyntaxNode node) continue;
            var nodeTraversed = node.Traverse().ToList();

            var start = i;
            var end = start + nodeTraversed.Count;

            elements.Count.ShouldBeGreaterThanOrEqualTo(i + nodeTraversed.Count);
            elements[start..end].SequenceEqual(nodeTraversed).ShouldBeTrue();
        }
    }
    
    [Theory]
    [Corpus]
    public void CorpusTraverse_InSourceOrder(string path)
    {
        var tree = Parser.Parse(SourceText.LoadFile(path));
        var elements = tree.FileSyntax.Traverse().ToList();

        for (var i = 1; i < elements.Count; i++)
            elements[i].FullRange.Start.ShouldBeGreaterThanOrEqualTo(elements[i - 1].FullRange.Start);
    }
    
    [Fact]
    public void Traverse()
    {
        var elements = Parser.Parse(SourceText.From("F(a + b, c);")).FileSyntax.Traverse();
        var text = string.Join('\n', elements.Select(el => $"{el.GetType().Name} \'{el.Text.ToString()}\'"));
        InlineSnapshot.Validate(text, """
            FileSyntax 'F(a + b, c);'
            ExprStmtSyntax 'F(a + b, c);'
            CallExprSyntax 'F(a + b, c)'
            NameRefSyntax 'F'
            IdentifierToken 'F'
            ArgListSyntax '(a + b, c)'
            Token '('
            ArgSyntax 'a + b'
            BinaryExprSyntax 'a + b'
            NameRefSyntax 'a'
            IdentifierToken 'a'
            Token ' '
            Token '+'
            NameRefSyntax 'b'
            Token ' '
            IdentifierToken 'b'
            Token ','
            ArgSyntax 'c'
            NameRefSyntax 'c'
            Token ' '
            IdentifierToken 'c'
            Token ')'
            Token ';'
            Token ''
            """);
    }

    [Fact]
    public void TraverseTokens()
    {
        var tokens = Parser.Parse(SourceText.From("F(a + b, c);")).FileSyntax.TraverseTokens();
        var text = string.Join('\n', tokens.Select(token => $"{token.Kind} \'{token.Text.ToString()}\'"));
        InlineSnapshot.Validate(text, """
            Identifier 'F'
            OpenParen '('
            Identifier 'a'
            Whitespace ' '
            Plus '+'
            Whitespace ' '
            Identifier 'b'
            Comma ','
            Whitespace ' '
            Identifier 'c'
            CloseParen ')'
            Semicolon ';'
            Eof ''
            """);
    }
}