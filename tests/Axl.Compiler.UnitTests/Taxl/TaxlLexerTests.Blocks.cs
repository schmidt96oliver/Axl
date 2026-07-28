using Axl.Compiler.Taxl;

namespace Axl.Compiler.UnitTests.Taxl;

public partial class TaxlLexerTests
{
    public class Blocks
    {
        [Theory]
        [InlineData("#begin\n#begin\n#end", TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive)]
        [InlineData("#begin\n#b\n#end", TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive)]
        [InlineData("#begin\n#beginfile\n#end", TaxlTokenKind.Directive, TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#begin\n#asd\n 234dsxf\n#test\nasdf\n#end", TaxlTokenKind.Directive, TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        public void DirectiveAfterBegin_InAxlText(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKindsIgnoreTrivia(input, expectedKinds);

        [Theory]
        [InlineData("#addfile\n#addfile\n#endfile", TaxlTokenKind.Directive, TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#addfile\n#b\n#endfile", TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive)]
        [InlineData("#addfile\n#begin\n#endfile", TaxlTokenKind.Directive, TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#addfile\n#asd\n 234dsxf\n#test\nasdf\n#endfile", TaxlTokenKind.Directive, TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        public void DirectiveAfterAddFile_InAxlText(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKindsIgnoreTrivia(input, expectedKinds);

        [Theory]
        [InlineData("#begin\n#end", TaxlTokenKind.Directive,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#begin\n1234\n#end A", TaxlTokenKind.Directive,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive,
            TaxlTokenKind.Identifier)]
        [InlineData("#begin\n1234\n#end #end", TaxlTokenKind.Directive,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive,
            TaxlTokenKind.Directive)]
        public void EndDirective_AfterAxlText(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKindsIgnoreTrivia(input, expectedKinds);

        [Theory]
        [InlineData("#addfile\n#endfile", TaxlTokenKind.Directive,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#addfile\n1234\n#endfile A", TaxlTokenKind.Directive,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive,
            TaxlTokenKind.Identifier)]
        [InlineData("#addfile\n1234\n#endfile #endfile", TaxlTokenKind.Directive,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive,
            TaxlTokenKind.Directive)]
        public void EndFileDirective_AfterAxlText(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKindsIgnoreTrivia(input, expectedKinds);

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
        public void SameLineBeginAndEnd_ProducesAxlText(string input, params TaxlTokenKind[] expectedKinds)
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
        public void SameLineAddFileAndEndFile_ProducesAxlText(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKinds(input, expectedKinds);

        [Fact]
        public void LooksLikeEndDirectiveInScriptBlock_InAxlText()
            => AssertKindsIgnoreTrivia("#begin\n#enda\n#end", 
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);
        
        [Fact]
        public void LooksLikeEndFileDirectiveInFileBlock_InAxlText()
            => AssertKindsIgnoreTrivia("#addfile\n#endfilea\n#endfile", 
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);
        
        [Fact]
        public void EndDirectiveInFileBlock_InAxlText()
            => AssertKindsIgnoreTrivia("#addfile\n#end\n#endfile", 
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);

        [Fact]
        public void EndFileDirectiveInScriptBlock_InAxlText()
            => AssertKindsIgnoreTrivia("#begin\n#endfile\n#end", 
                TaxlTokenKind.Directive, TaxlTokenKind.AxlText, TaxlTokenKind.Directive);


        [Theory]
        [InlineData("#begin A\n#end",
            TaxlTokenKind.Directive,
            TaxlTokenKind.Identifier,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#begin \"A\"\n#end",
            TaxlTokenKind.Directive,
            TaxlTokenKind.String,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#begin \"A\" A B C  \n#end",
            TaxlTokenKind.Directive,
            TaxlTokenKind.String, TaxlTokenKind.Identifier, TaxlTokenKind.Identifier, TaxlTokenKind.Identifier,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        public void BeginWithSameLineValues_BeforeAxlText(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKindsIgnoreTrivia(input, expectedKinds);
        

        [Theory]
        [InlineData("#addfile A\n#endfile",
            TaxlTokenKind.Directive,
            TaxlTokenKind.Identifier,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#addfile \"A\"\n#endfile",
            TaxlTokenKind.Directive,
            TaxlTokenKind.String,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        [InlineData("#addfile \"A\" A B C  \n#endfile",
            TaxlTokenKind.Directive,
            TaxlTokenKind.String, TaxlTokenKind.Identifier, TaxlTokenKind.Identifier, TaxlTokenKind.Identifier,
            TaxlTokenKind.AxlText,
            TaxlTokenKind.Directive)]
        public void AddFileWithSameLineValues_BeforeAxlText(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKindsIgnoreTrivia(input, expectedKinds);


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

    }


}