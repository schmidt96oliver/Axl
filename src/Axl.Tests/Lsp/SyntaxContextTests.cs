using AxlCompiler;
using AxlCompiler.Syntax;
using AxlCompiler.Text;
using AxlLsp;
using Shouldly;

namespace Axl.Tests.Lsp;

public sealed class SyntaxContextTests
{
    private SyntaxContext Make(string input)
    {
        input = input.RemoveCaret(out var caret);
        var tree = Parser.Parse(SourceText.From(input));
        var compilation = Compilation.From([tree]);
        return SyntaxContext.At(caret, tree, compilation);
    }

    [Theory]
    [InlineData("a| b", "a")]
    [InlineData("a |b", "b")]
    [InlineData("true|", "true")]
    [InlineData("var|", "var")]
    [InlineData("|let", "let")]
    [InlineData("wh|ile", "while")]
    [InlineData("Ty|pe", "Type")]
    [InlineData("\"ab|", "")]
    [InlineData("\"{x|", "x")]
    [InlineData("\"{|x", "x")]
    [InlineData("x.|", "")]
    [InlineData("x.Fo|", "Fo")]
    [InlineData("x.|Fo", "Fo")]

    [InlineData("foo|(", "foo")]
    [InlineData("foo|(bar", "foo")]
    [InlineData("foo(|bar", "bar")]
    [InlineData("foo( |bar", "bar")]
    [InlineData("foo(| bar", "")]

    [InlineData("Type( // Fi|", "")]
    public void TouchingWord(string input, string touchingWord)
    {
        (Make(input).TouchingWord?.Text.ToString() ?? "").ShouldBe(touchingWord);
    }

    [Theory]
    [InlineData("( a| b", "(")]
    [InlineData("! a |b", "a")]
    [InlineData("X Ty|pe", "X")]
    [InlineData("X \"ab|", "ab")]
    [InlineData("\"{x|", "{")]
    [InlineData("\"{|x", "{")]
    [InlineData("x.|", ".")]
    [InlineData("x.Fo|", ".")]
    [InlineData("x.|Fo", ".")]

    [InlineData("X foo|(", "X")]
    [InlineData("foo|(bar", "")]
    [InlineData("foo(|bar", "(")]
    [InlineData("foo( |bar", "(")]
    [InlineData("foo(| bar", "(")]

    [InlineData("Type( // Fi|", "(")]
    public void TargetToken(string input, string target)
    {
        (Make(input).TargetToken?.Text.ToString() ?? "").ShouldBe(target);
    }


    [Theory]
    [InlineData("|//", false)]
    [InlineData("/|/", true)]
    [InlineData("//|", true)]
    [InlineData("Type( // Fi|", true)]
    [InlineData("Type( // Fi\n|", false)]
    public void IsInsideComment(string input, bool insideComment)
    {
        Make(input).IsInsideComment.ShouldBe(insideComment);
    }
    
    [Theory]
    [InlineData("\"ab|", true)]
    [InlineData("\"ab\"|", false)]
    [InlineData("\"ab|\"", true)]
    [InlineData("\"|", true)]
    [InlineData("\"|\"", true)]
    [InlineData("\"{x}|ab\"", true)]
    [InlineData("{x}|ab", false)]
    [InlineData("\"ab{|", false)]
    [InlineData("\"ab\\n|cd\"", true)]
    [InlineData("\"ab\n|", false)]
    public void IsInsideString (string input, bool insideComment)
    {
        Make(input).IsInsideString.ShouldBe(insideComment);
    }
}