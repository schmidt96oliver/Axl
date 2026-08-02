using System.Runtime.CompilerServices;

namespace Axl.Compiler.UnitTests;

using Shouldly;

public class SourceFileTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ", " ")]
    [InlineData("\n", "\n")]
    [InlineData(" \n", " \n")]
    [InlineData(" \n ", " \n", " ")]
    [InlineData("a\n", "a\n")]
    [InlineData("a \n", "a \n")]
    [InlineData("a \n ", "a \n", " ")]
    [InlineData("a\n\n", "a\n", "\n")]
    [InlineData("\n\n", "\n", "\n")]
    [InlineData("a\nbc\n\nde", "a\n", "bc\n", "\n", "de")]
    private void Lines_CorrectLineTextsAndNumbers(string text, params string[] expectedLineTexts)
    {
        var sourceFile = SourceFile.FromText(text);
        
        Assert.False(sourceFile.Lines.IsDefault);
        
        // Line contents
        sourceFile.Lines
            .Select(line => sourceFile.GetText(line.Span).ToString())
            .ShouldBe(expectedLineTexts);
        
        // Line numbers
        sourceFile.Lines
            .Select(line => line.LineNumber)
            .ShouldBe(Enumerable.Range(0, expectedLineTexts.Length));
    }

    [Theory]
    [InlineData("\n", 0, 0)]
    
    [InlineData("\n\n", 0, 0)]
    [InlineData("\n\n", 1, 1)]
    
    [InlineData("012\n45\n7", 0, 0)]
    [InlineData("012\n45\n7", 1, 0)]
    [InlineData("012\n45\n7", 2, 0)]
    [InlineData("012\n45\n7", 3, 0)]
    [InlineData("012\n45\n7", 4, 1)]
    [InlineData("012\n45\n7", 5, 1)]
    [InlineData("012\n45\n7", 6, 1)]
    [InlineData("012\n45\n7", 7, 2)]
    private void GetLine_CorrectLineNumber(string text, int index, int expectedLineIndex)
    {
        var sourceFile = SourceFile.FromText(text);
        
        sourceFile.Lines.IsDefault.ShouldBeFalse();

        sourceFile.GetLineAt(index).LineNumber
            .ShouldBe(expectedLineIndex);
    }

    [Theory]
    [InlineData("\n", 0, 0, 0)]
    
    [InlineData("\n\n", 0, 0, 0)]
    [InlineData("\n\n", 1, 1, 0)]
    
    [InlineData("012\n45\n7", 0, 0, 0)]
    [InlineData("012\n45\n7", 1, 0, 1)]
    [InlineData("012\n45\n7", 2, 0, 2)]
    [InlineData("012\n45\n7", 3, 0, 3)]
    [InlineData("012\n45\n7", 4, 1, 0)]
    [InlineData("012\n45\n7", 5, 1, 1)]
    [InlineData("012\n45\n7", 6, 1, 2)]
    [InlineData("012\n45\n7", 7, 2, 0)]
    private void GetLinePosition_CorrectLineAndColumn(string text, int index, int expectedLine, int expectedColumn)
    {
        var sourceFile = SourceFile.FromText(text);
        
        sourceFile.Lines.IsDefault.ShouldBeFalse();

        var linePos = sourceFile.GetLinePosition(index);
        linePos.Line.ShouldBe(expectedLine);
        linePos.Column.ShouldBe(expectedColumn);
    }

    [Fact]
    private void Lines_EvaluatedOnce()
    {
        var text = "012\n45\n7";
        var sourceFile = SourceFile.FromText(text);

        var array1 = sourceFile.Lines;
        var array2 = sourceFile.Lines;
        
        array1.IsDefault.ShouldBeFalse();
        array2.IsDefault.ShouldBeFalse();
        
        // ImmutableArray uses reference equality on its internal array.
        // So if a new one had been constructed, this will fail.
        (array1 == array2).ShouldBeTrue();
    }
}