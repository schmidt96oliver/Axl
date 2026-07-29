using Axl.Compiler.Taxl;

namespace Axl.Compiler.UnitTests.Taxl;

public partial class TaxlLexerTests
{
    public class Blocks
    {
        [Fact]
        public void BeginNotClosed_ProducesAxlText()
            => AssertKindsIgnoreTrivia("#begin\nbla", TaxlTokenKind.Directive, TaxlTokenKind.AxlText);
        [Fact]
        public void AddfileNotClosed_ProducesAxlText()
            => AssertKindsIgnoreTrivia("#addfile\nbla", TaxlTokenKind.Directive, TaxlTokenKind.AxlText);
        
        [Theory]
        [InlineData("#begin\n#begin\n#end")]
        [InlineData("#begin\n#b\n#end")]
        [InlineData("#begin\n#beginfile\n#end")]
        [InlineData("#begin\n#asd\n 234dsxf\n#test\nasdf\n#end")]
        
        [InlineData("#begin\n#end")]
        [InlineData("#begin\n1234\n#end")]
        
        [InlineData("#begin#end")]
        [InlineData("#begin #end")]
        public void BeginWithoutValues_FollowedByAxlTextAndEnd(string input)
            => AssertKindsIgnoreTrivia(input, TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);
        
        [Theory]
        [InlineData("#addfile\n#begin\n#endfile")]
        [InlineData("#addfile\n#b\n#endfile")]
        [InlineData("#addfile\n#addfile\n#endfile")]
        [InlineData("#addfile\n#asd\n 234dsxf\n#test\nasdf\n#endfile")]
        
        [InlineData("#addfile\n#endfile")]
        [InlineData("#addfile\n1234\n#endfile")]
        
        [InlineData("#addfile#endfile")]
        [InlineData("#addfile #endfile")]
        public void AddfileWithoutValues_FollowedByAxlTextAndEndfile(string input)
            => AssertKindsIgnoreTrivia(input, TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);

        
        [Theory]
        [InlineData("#begin A\n#end", TaxlTokenKind.Identifier)]
        [InlineData("#begin \"A\"\n#end", TaxlTokenKind.String)]
        [InlineData("#begin \"A\" A B C  \n#end", 
            TaxlTokenKind.String, 
            TaxlTokenKind.Identifier,
            TaxlTokenKind.Identifier,
            TaxlTokenKind.Identifier)]
        [InlineData("#begin +-\n#end", TaxlTokenKind.Error)]
        public void BeginWithValues_ValuesBeforeAxlText(string input, params TaxlTokenKind[] expectedValues)
            => AssertKindsIgnoreTrivia(input, [TaxlTokenKind.Directive, 
                ..expectedValues,
                TaxlTokenKind.AxlText, TaxlTokenKind.Directive]);
        
        [Theory]
        [InlineData("#addfile A\n#endfile", TaxlTokenKind.Identifier)]
        [InlineData("#addfile \"A\"\n#endfile", TaxlTokenKind.String)]
        [InlineData("#addfile \"A\" A B C  \n#endfile", 
            TaxlTokenKind.String, 
            TaxlTokenKind.Identifier,
            TaxlTokenKind.Identifier,
            TaxlTokenKind.Identifier)]
        [InlineData("#addfile +-\n#endfile", TaxlTokenKind.Error)]
        public void AddfileWithValues_ValuesBeforeAxlText(string input, params TaxlTokenKind[] expectedValues)
            => AssertKindsIgnoreTrivia(input, [TaxlTokenKind.Directive, 
                ..expectedValues,
                TaxlTokenKind.AxlText, TaxlTokenKind.Directive]);


        [Theory]
        [InlineData("#begin\n1234\n#end A", TaxlTokenKind.Identifier)]
        [InlineData("#begin\n1234\n#end \"\"", TaxlTokenKind.String)]
        [InlineData("#begin\n1234\n#end\n#", TaxlTokenKind.Directive)]
        [InlineData("#begin\n1234\n#end\n#a", TaxlTokenKind.Directive)]
        [InlineData("#begin\n1234\n#end #end", TaxlTokenKind.Directive)]
        [InlineData("#begin\n1234\n#end #endfile", TaxlTokenKind.Directive)]
        [InlineData("#begin\n1234\n#end # #a #b", TaxlTokenKind.Directive, TaxlTokenKind.Directive, TaxlTokenKind.Directive)]
        [InlineData("#begin\n1234\n#end\nABC\nCDE", TaxlTokenKind.Identifier, TaxlTokenKind.Identifier)]
        public void TokensAfterBeginBlock_AfterAxlBlock(string input, params TaxlTokenKind[] expectedAfterBlockKinds)
            => AssertKindsIgnoreTrivia(input, [
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive,
                .. expectedAfterBlockKinds
            ]);

        [Theory]
        [InlineData("#addfile\n1234\n#endfile A", TaxlTokenKind.Identifier)]
        [InlineData("#addfile\n1234\n#endfile \"\"", TaxlTokenKind.String)]
        [InlineData("#addfile\n1234\n#endfile\n#", TaxlTokenKind.Directive)]
        [InlineData("#addfile\n1234\n#endfile\n#a", TaxlTokenKind.Directive)]
        [InlineData("#addfile\n1234\n#endfile #end", TaxlTokenKind.Directive)]
        [InlineData("#addfile\n1234\n#endfile #endfile", TaxlTokenKind.Directive)]
        [InlineData("#addfile\n1234\n#endfile # #a #b", TaxlTokenKind.Directive, TaxlTokenKind.Directive, TaxlTokenKind.Directive)]
        [InlineData("#addfile\n1234\n#endfile\nABC\nCDE", TaxlTokenKind.Identifier, TaxlTokenKind.Identifier)]
        public void TokensAfterAddfileBlock_AfterAxlBlock(string input, params TaxlTokenKind[] expectedAfterBlockKinds)
            => AssertKindsIgnoreTrivia(input, [
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive,
                .. expectedAfterBlockKinds
            ]);
        
        
        [Theory]
        [InlineData("#begin#end", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.AxlText, 
            TaxlTokenKind.Directive)]
        [InlineData("#begin #end", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.AxlText, 
            TaxlTokenKind.Directive)]
        [InlineData("#begin \"Name.axl\" #end", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.String,
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.AxlText, 
            TaxlTokenKind.Directive)]
        [InlineData("#begin Test #end", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#begin Test#end", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier, 
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#begin Test Test2 #end", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        public void SameLineBeginAndEnd_ValuesAndTriviaAreBeforeAxlText(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKinds(input, expectedKinds);
        
        [Theory]
        [InlineData("#addfile#endfile", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.AxlText, 
            TaxlTokenKind.Directive)]
        [InlineData("#addfile #endfile", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.AxlText, 
            TaxlTokenKind.Directive)]
        [InlineData("#addfile \"Name.axl\" #endfile", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.String,
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.AxlText, 
            TaxlTokenKind.Directive)]
        [InlineData("#addfile Test #endfile", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#addfile Test#endfile", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier, 
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#addfile Test Test2 #endfile", 
            TaxlTokenKind.Directive, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier, 
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        public void SameLineAddfileAndEndfile_ValuesAndTriviaAreBeforeAxlText(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKinds(input, expectedKinds);

        
        [Fact]
        public void LooksLikeEndDirective_InAxlText()
            => AssertKindsIgnoreTrivia("#begin\n#enda\n#end", 
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);
        
        [Fact]
        public void LooksLikeEndfileDirective_InAxlText()
            => AssertKindsIgnoreTrivia("#addfile\n#endfilea\n#endfile", 
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);
        
        [Fact]
        public void EndInAddfileBlock_InAxlText()
            => AssertKindsIgnoreTrivia("#addfile\n#end\n#endfile", 
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);

        [Fact]
        public void EndfileInBeginBlock_InAxlText()
            => AssertKindsIgnoreTrivia("#begin\n#endfile\n#end", 
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);


        [Theory]
        [InlineData("#begin#end", "")]
        [InlineData("#begin\n#end", "")]
        [InlineData("#begin\n\n#end", "")]
        [InlineData("#begin\n\n\n#end", "\n")]
        [InlineData("#begin\nabc\n#end", "abc")]
        [InlineData("#begin\n\nabc\n\n#end", "\nabc\n")]
        [InlineData("#begin\n abc \n#end", " abc ")]
        [InlineData("#begin\n abc \n  #end", " abc ")]
        public void BeginEnd_ConsumesNewlines(string input, string expectedAxlText)
        {
            var lex = RunLexer(input);
            var textToken = Assert.Single(lex.Where(token => token.Kind is TaxlTokenKind.AxlText));
            Assert.Equal(expectedAxlText, textToken.Text);
        }

        [Theory]
        [InlineData("#addfile#endfile", "")]
        [InlineData("#addfile\n#endfile", "")]
        [InlineData("#addfile\n\n#endfile", "")]
        [InlineData("#addfile\n\n\n#endfile", "\n")]
        [InlineData("#addfile\nabc\n#endfile", "abc")]
        [InlineData("#addfile\n\nabc\n\n#endfile", "\nabc\n")]
        [InlineData("#addfile\n abc \n#endfile", " abc ")]
        [InlineData("#addfile\n abc \n  #endfile", " abc ")]
        public void AddfileEndfile_ConsumesNewlines(string input, string expectedAxlText)
        {
            var lex = RunLexer(input);
            var textToken = Assert.Single(lex.Where(token => token.Kind is TaxlTokenKind.AxlText));
            Assert.Equal(expectedAxlText, textToken.Text);
        }


        [Theory]
        [InlineData("#begin\na#end\n#end")]
        [InlineData("#begin\n//#end\n#end")]
        [InlineData("#begin\n\"#end\"\n#end")]
        [InlineData("#begin\nasd asd asd #end\"\n#end")]
        [InlineData("#begin\nasd asd asd //#end\"\n#end")]
        public void EndAfterSameLineCharacters_InAxlText(string input)
            => AssertKindsIgnoreTrivia(input, TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);

        [Theory]
        [InlineData("#addfile\na#endfile\n#endfile")]
        [InlineData("#addfile\n//#endfile\n#endfile")]
        [InlineData("#addfile\n\"#endfile\"\n#endfile")]
        [InlineData("#addfile\nasd asd asd #endfile\"\n#endfile")]
        [InlineData("#addfile\nasd asd asd //#endfile\"\n#endfile")]
        public void EndfileAfterSameLineCharacters_InAxlText(string input)
            => AssertKindsIgnoreTrivia(input, TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);

        [Fact]
        public void EndAfterSameLineWhitespace_StopsAxlText()
            => AssertKindsIgnoreTrivia("#begin\n    #end", TaxlTokenKind.Directive, TaxlTokenKind.AxlText,
                TaxlTokenKind.Directive);
        [Fact]
        public void EndfileAfterSameLineWhitespace_StopsAxlText()
            => AssertKindsIgnoreTrivia("#addfile\n    #endfile", TaxlTokenKind.Directive, TaxlTokenKind.AxlText,
                TaxlTokenKind.Directive);
    }


}