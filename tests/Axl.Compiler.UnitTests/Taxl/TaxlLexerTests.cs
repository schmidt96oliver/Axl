using Axl.Compiler.Diagnostics;
using Axl.Compiler.Taxl;

namespace Axl.Compiler.UnitTests.Taxl;

public partial class TaxlLexerTests(ITestOutputHelper output)
{
    public static void AssertKindsIgnoreTrivia(string input, params TaxlTokenKind[] expectedKinds)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(input);
        
        var diagnostics = new DiagnosticBag();
        var lex = TaxlLexer.Lex(SourceView.Whole(SourceFile.FromText(input)), diagnostics);
        
        Assert.Equal(expectedKinds,
        [
            .. lex.Select(token => token.Kind)
                .Where(tokenKind =>
                    tokenKind is not (TaxlTokenKind.Whitespace or TaxlTokenKind.Comment or TaxlTokenKind.Newline))
        ]);
    }
}