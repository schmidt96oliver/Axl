using Axl.Compiler.Text;
using Shouldly;

namespace Axl.Compiler.UnitTests;

public class SourceTextTests
{
    [Fact]
    public void Empty()
    {
        var lines = SourceText.From("").Lines;
        var singleLine = lines.ShouldHaveSingleItem();
        singleLine.Length.ShouldBe(0);
        singleLine.LengthWithLineBreak.ShouldBe(0);
    }

    [Theory]
    [InlineData("",
                "")]
    [InlineData(" ",
                " ")]
    [InlineData("abc",
                "abc")]
    [InlineData("\n",
                "", "")]
    [InlineData("\r\n",
                "", "")]
    [InlineData("a\n", 
                "a", "")]
    [InlineData("a\na", 
                "a", "a")]
    [InlineData("a\nbc\n\nde", 
                "a", "bc", "", "de")]
    public void LineText(string input, params string[] expectedLines)
    {
        var sourceText = SourceText.From(input);
        var lines = sourceText.Lines;
        
        lines.Select(line => sourceText.GetText(line.Range).ToString())
            .ShouldBe(expectedLines);
    }
    
    [Theory]
    [InlineData("",
                "")]
    [InlineData(" ",
                " ")]
    [InlineData("abc",
                "abc")]
    [InlineData("\n",
                "\n", "")]
    [InlineData("\r\n",
                "\r\n", "")]
    [InlineData("a\n", 
                "a\n", "")]
    [InlineData("a\na", 
                "a\n", "a")]
    [InlineData("a\nbc\n\nde", 
                "a\n", "bc\n", "\n", "de")]
    public void CorrectLineText_WithLineBreak(string input, params string[] expectedLines)
    {
        var sourceText = SourceText.From(input);
        var lines = sourceText.Lines;
        
        lines.Select(line => sourceText.GetText(line.RangeWithLineBreak).ToString())
            .ShouldBe(expectedLines);
    }

    [Fact]
    public void LineIndex()
    {
        var sourceText = SourceText.From("""
                                         abc
                                         ad

                                         ad // bla

                                         """);

        for (var lineIndex = 0; lineIndex < sourceText.Lines.Length; lineIndex++)
        {
            var line = sourceText.Lines[lineIndex];
            Enumerable.Range(line.First, line.LengthWithLineBreak)
                .Select(sourceText.GetLineIndex)
                .ShouldAllBe(actualLineIndex => actualLineIndex == lineIndex);
        }
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 0)]
    [InlineData("abc\n", 1)]
    public void LineIndex_Eof(string text, int expectedEofLineIndex)
    {
        var sourceText = SourceText.From(text);
        
        sourceText.GetLineIndex(sourceText.Length).ShouldBe(expectedEofLineIndex);
    }
}