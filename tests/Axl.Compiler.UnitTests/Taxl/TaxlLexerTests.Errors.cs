using Axl.Compiler.Diagnostics;
using Axl.Compiler.Taxl;

namespace Axl.Compiler.UnitTests.Taxl;

public partial class TaxlLexerTests
{
    public class Errors
    {
        private static void AssertErrorTexts(string input, params string[] expectedTexts)
        {
            TestContext.Current.TestOutputHelper?.WriteLine("--- Input ---");
            TestContext.Current.TestOutputHelper?.WriteLine(input);
            TestContext.Current.TestOutputHelper?.WriteLine("--- Expected Error Texts ---");
            TestContext.Current.TestOutputHelper?.WriteLine(string.Join(", ", expectedTexts));

            var lex = RunLexer(input);

            Assert.Equal(expectedTexts,
                lex.Where(token => token.Kind is TaxlTokenKind.Error).Select(token => token.Text));
        }

        private static DiagnosticBag LexWithDiagnostics(string input)
        {
            var diagnosticBag = new DiagnosticBag();
            RunLexer(input, diagnosticBag);
            return diagnosticBag;
        }


        [Theory]
        [InlineData("+", "+")]
        [InlineData("+-", "+-")]
        [InlineData("123", "123")]
        [InlineData("€€", "€€")]
        [InlineData("+-*/", "+-*/")]
        public void InvalidCharacters_EmitOneErrorToken(string input, string expectedText)
            => AssertErrorTexts(input, expectedText);

        [Theory]
        [InlineData("+a+", "+", "+")]
        [InlineData("+ +", "+", "+")]
        [InlineData("1 2", "1", "2")]
        public void InvalidCharactersAroundToken_EmitSeparateErrorTokens(string input, params string[] expectedTexts)
            => AssertErrorTexts(input, expectedTexts);


        [Theory]
        [InlineData("+a", TaxlTokenKind.Error, TaxlTokenKind.Identifier)]
        [InlineData("+_", TaxlTokenKind.Error, TaxlTokenKind.Identifier)]
        [InlineData("+ ", TaxlTokenKind.Error, TaxlTokenKind.Whitespace)]
        [InlineData("+\n", TaxlTokenKind.Error, TaxlTokenKind.Newline)]
        [InlineData("+#a", TaxlTokenKind.Error, TaxlTokenKind.Directive)]
        [InlineData("+//c", TaxlTokenKind.Error, TaxlTokenKind.Comment)]
        [InlineData("+\"s\"", TaxlTokenKind.Error, TaxlTokenKind.String)]
        public void ErrorRun_StopsAtNextTokenStart(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKinds(input, expectedKinds);

        // A single slash starts no token, only "//" does. So the error run must look ahead
        // two characters and must not stop on a lone slash.
        [Theory]
        [InlineData("/", TaxlTokenKind.Error)]
        [InlineData("/x", TaxlTokenKind.Error, TaxlTokenKind.Identifier)]
        [InlineData("/ ", TaxlTokenKind.Error, TaxlTokenKind.Whitespace)]
        [InlineData("x/", TaxlTokenKind.Identifier, TaxlTokenKind.Error)]
        public void LoneSlash_IsError(string input, params TaxlTokenKind[] expectedKinds)
            => AssertKinds(input, expectedKinds);

        [Theory]
        [InlineData("/", "/")]
        [InlineData("/x", "/")]
        [InlineData("+/-", "+/-")]
        public void LoneSlash_DoesNotSplitErrorRun(string input, string expectedText)
            => AssertErrorTexts(input, expectedText);


        [Theory]
        [InlineData("+", 1)]
        [InlineData("+-", 1)]
        [InlineData("123", 1)]
        [InlineData("+ +", 2)]
        [InlineData("+a+", 2)]
        public void ErrorRun_ReportsOneDiagnosticPerErrorToken(string input, int expectedCount)
            => Assert.Equal(expectedCount,
                LexWithDiagnostics(input).Diagnostics.OfType<Diagnostic.InvalidCharacters>().Count());

        // Regression: the error run used to find its end by lexing the following token and
        // throwing it away, which reported that token's diagnostics a second time.
        [Theory]
        [InlineData("+\"abc")]
        [InlineData("+\"abc\n")]
        public void InvalidCharactersBeforeUnclosedString_ReportsEachDiagnosticOnce(string input)
        {
            var diagnostics = LexWithDiagnostics(input).Diagnostics;

            Assert.Single(diagnostics.OfType<Diagnostic.InvalidCharacters>());
            Assert.Single(diagnostics.OfType<Diagnostic.StringNotClosed>());
        }


        // Guards the character set in CanStartToken against the case labels in LexSingle:
        // a kind that CanStartToken does not know about gets swallowed into an error run.
        [Theory]
        [InlineData("\n")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\r")]
        [InlineData("//c")]
        [InlineData("#d")]
        [InlineData("abc")]
        [InlineData("_")]
        [InlineData("\"s\"")]
        public void MinimalSampleOfEachKind_EmitsNoErrorToken(string input)
            => AssertErrorTexts(input);
    }
}
