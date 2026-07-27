namespace Axl.Compiler.UnitTests;

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
    private void Lines_CorrectTextAndSpan(string text, params string[] expectedLineTexts)
    {
        var sourceFile = SourceFile.FromText(text);
        
        Assert.False(sourceFile.Lines.IsDefault);
        
        // Assert line content
        Assert.Equal(expectedLineTexts, sourceFile.Lines.Select(line => sourceFile.GetText(line.Span)));
        
        // Assert line numbers
        Assert.Equal(Enumerable.Range(0, expectedLineTexts.Length),
            sourceFile.Lines.Select(line => line.LineNumber));
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
    private void GetLine_Correct(string text, int index, int expectedLineIndex)
    {
        var sourceFile = SourceFile.FromText(text);
        
        Assert.False(sourceFile.Lines.IsDefault);

        var lineIndex = sourceFile.GetLineAt(index);
        Assert.Equal(expectedLineIndex, lineIndex.LineNumber);
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
    private void GetLinePosition_Correct(string text, int index, int expectedLine, int expectedColumn)
    {
        var sourceFile = SourceFile.FromText(text);
        
        Assert.False(sourceFile.Lines.IsDefault);

        var linePos = sourceFile.GetLinePosition(index);
        Assert.Equal(linePos.Line, expectedLine);
        Assert.Equal(linePos.Column, expectedColumn);
    }

    [Fact]
    private void Lines_EvaluatedOnce()
    {
        var text = "012\n45\n7";
        var sourceFile = SourceFile.FromText(text);

        var array1 = sourceFile.Lines;
        var array2 = sourceFile.Lines;
        
        Assert.False(array1.IsDefault);
        Assert.False(array2.IsDefault);
        
        // ImmutableArray uses reference equality on its internal array.
        // So if a new one had been constructed, this will fail.
        Assert.Equal(array1, array2);
    }
}