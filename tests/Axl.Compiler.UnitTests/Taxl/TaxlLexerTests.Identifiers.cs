using Axl.Compiler.Taxl;

namespace Axl.Compiler.UnitTests.Taxl;

public partial class TaxlLexerTests
{
    public class Identifiers
    {
        private static void AssertSingleIdentifier(string input, string expectedText)
        {
            TestContext.Current.TestOutputHelper?.WriteLine("--- Input ---");
            TestContext.Current.TestOutputHelper?.WriteLine(input);
            TestContext.Current.TestOutputHelper?.WriteLine("--- Expected Identifier ---");
            TestContext.Current.TestOutputHelper?.WriteLine(expectedText);

            var lex = RunLexer(input);
            var identifier = Assert.Single(lex.Where(token => token.Kind is TaxlTokenKind.Identifier));

            Assert.Equal(expectedText, identifier.Text);
        }


        [Theory]
        [InlineData("a")]
        [InlineData("Z")]
        [InlineData("abc")]
        [InlineData("_")]
        [InlineData("_abc")]
        [InlineData("a1")]
        [InlineData("a_b")]
        [InlineData("a-1_B2")]
        public void Identifier_IsLexedWhole(string input)
            => AssertSingleIdentifier(input, input);

        // Hyphens continue an identifier, so a-b is one identifier and not a subtraction.
        [Theory]
        [InlineData("a-b")]
        [InlineData("a-b-c")]
        [InlineData("abc-")]
        public void HyphenatedIdentifier_IsOneIdentifier(string input)
            => AssertSingleIdentifier(input, input);

        [Theory]
        [InlineData("a b", TaxlTokenKind.Identifier, TaxlTokenKind.Whitespace, TaxlTokenKind.Identifier)]
        [InlineData("a\nb", TaxlTokenKind.Identifier, TaxlTokenKind.Newline, TaxlTokenKind.Identifier)]
        [InlineData("a#b", TaxlTokenKind.Identifier, TaxlTokenKind.Directive)]
        [InlineData("a\"b\"", TaxlTokenKind.Identifier, TaxlTokenKind.String)]
        [InlineData("a//c", TaxlTokenKind.Identifier, TaxlTokenKind.Comment)]
        [InlineData("a+", TaxlTokenKind.Identifier, TaxlTokenKind.Error)]
        public void Identifier_StopsAtNonIdentifierCharacter(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKinds(input, expectedKinds);

        // Digits continue an identifier, but cannot start one.
        [Theory]
        [InlineData("1", TaxlTokenKind.Error)]
        [InlineData("1a", TaxlTokenKind.Error, TaxlTokenKind.Identifier)]
        [InlineData("_1", TaxlTokenKind.Identifier)]
        public void DigitStart_IsNoIdentifier(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKinds(input, expectedKinds);

        // Unlike identifiers, directives do not take digits.
        [Theory]
        [InlineData("#a-b_c", TaxlTokenKind.Directive)]
        [InlineData("#a1", TaxlTokenKind.Directive, TaxlTokenKind.Error)]
        public void Directive_TakesNoDigits(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKinds(input, expectedKinds);
    }
}
