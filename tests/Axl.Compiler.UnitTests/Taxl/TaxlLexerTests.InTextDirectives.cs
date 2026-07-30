using Axl.Compiler.Taxl;

namespace Axl.Compiler.UnitTests.Taxl;

public partial class TaxlLexerTests
{
    public class InTextDirectives
    {
        private void AssertInTextTokenIgnoreTrivia(string input, params TaxlTokenKind[] expectedKinds)
        {
            TestContext.Current.TestOutputHelper?.WriteLine("--- Input ---");
            TestContext.Current.TestOutputHelper?.WriteLine(input);
            TestContext.Current.TestOutputHelper?.WriteLine("--- Expected Tokens (without trivia) ---");
            TestContext.Current.TestOutputHelper?.WriteLine(string.Join(", ", expectedKinds));
            
            var lex = RunLexer(input);
            var textToken = Assert.IsType<TaxlToken.AxlTextToken>(
                Assert.Single(lex.Where(token => token.Kind is TaxlTokenKind.AxlText)));
            
            Assert.Equal(expectedKinds,
            [
                .. textToken.InTextTokens.Select(token => token.Kind)
                    .Where(tokenKind =>
                        tokenKind is not (TaxlTokenKind.Whitespace or TaxlTokenKind.Comment or TaxlTokenKind.Newline))
            ]);
        }
        private void AssertInTextTokens(string input, params TaxlTokenKind[] expectedKinds)
        {
            TestContext.Current.TestOutputHelper?.WriteLine("--- Input ---");
            TestContext.Current.TestOutputHelper?.WriteLine(input);
            TestContext.Current.TestOutputHelper?.WriteLine("--- Expected Tokens (without trivia) ---");
            TestContext.Current.TestOutputHelper?.WriteLine(string.Join(", ", expectedKinds));
            
            var lex = RunLexer(input);
            var textToken = Assert.IsType<TaxlToken.AxlTextToken>(
                Assert.Single(lex.Where(token => token.Kind is TaxlTokenKind.AxlText)));
            
            Assert.Equal(expectedKinds,
            [
                .. textToken.InTextTokens.Select(token => token.Kind)
            ]);
        }

        
        [Theory]
        [InlineData("#begin\n//#\n#end", 
            TaxlTokenKind.Directive)]
        [InlineData("#begin\na //#\n#end", 
            TaxlTokenKind.Directive)]
        [InlineData("#begin\na //#", 
            TaxlTokenKind.Directive)]
        [InlineData("#begin\na //#a BC \"\"\n#end", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.Identifier,
            TaxlTokenKind.String)]
        [InlineData("#begin\na //#a BC \"\"", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.Identifier,
            TaxlTokenKind.String)]
        public void InTextToken_LexedCorrectly(string input, params TaxlTokenKind[] expectedKinds)
            => AssertInTextTokenIgnoreTrivia(input, expectedKinds);
        
        [Theory]
        [InlineData("#begin\na ///#\n#end")]
        [InlineData("#begin\na // //#a BC \"\"\n#end")]
        public void CommentedInTextToken_Ignored(string input, params TaxlTokenKind[] expectedKinds)
            => AssertInTextTokenIgnoreTrivia(input, expectedKinds);
        
        [Theory]
        [InlineData("#begin\na \"//#\"\n#end")]
        [InlineData("#begin\na \"//#a\" BC \"\"\n#end")]
        public void StringedInTextToken_Ignored(string input, params TaxlTokenKind[] expectedKinds)
            => AssertInTextTokenIgnoreTrivia(input, expectedKinds);

        [Theory]
        [InlineData("#begin\n//#\n", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.Newline)]
        [InlineData("#begin\n//#a\n", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.Newline)]
        [InlineData("#begin\n//#a\n#end", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.Newline)]
        [InlineData("#begin\n//#\n#end", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.Newline)]
        [InlineData("#begin\n//# ABC\n#end", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier,
            TaxlTokenKind.Newline)]
        [InlineData("#begin\n//# ABC\n", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier,
            TaxlTokenKind.Newline)]
        [InlineData("#begin\n//#ab ABC\n", 
            TaxlTokenKind.Directive,
            TaxlTokenKind.Whitespace,
            TaxlTokenKind.Identifier,
            TaxlTokenKind.Newline)]
        public void InTextToken_EmitsNewline(string input, params TaxlTokenKind[] expectedKinds)
            => AssertInTextTokens(input, expectedKinds);
    }
}