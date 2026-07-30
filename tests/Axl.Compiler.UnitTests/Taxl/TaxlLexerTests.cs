using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Taxl;

namespace Axl.Compiler.UnitTests.Taxl;

public partial class TaxlLexerTests(ITestOutputHelper output)
{
    public static ImmutableArray<TaxlToken> RunLexer(string input)
        => TaxlLexer.Lex(SourceFileView.FromText(input), new DiagnosticBag());
    
    
    public static void AssertKindsIgnoreTrivia(string input, params TaxlTokenKind[] expectedKinds)
    {
        TestContext.Current.TestOutputHelper?.WriteLine("--- Input ---");
        TestContext.Current.TestOutputHelper?.WriteLine(input);
        TestContext.Current.TestOutputHelper?.WriteLine("--- Expected Tokens (without trivia) ---");
        TestContext.Current.TestOutputHelper?.WriteLine(string.Join(", ", expectedKinds));
        
        var lex = RunLexer(input);
        
        Assert.Equal(expectedKinds,
        [
            .. lex.Select(token => token.Kind)
                .Where(tokenKind =>
                    tokenKind is not (TaxlTokenKind.Whitespace or TaxlTokenKind.Comment or TaxlTokenKind.Newline))
        ]);
    }
    public static void AssertKinds(string input, params TaxlTokenKind[] expectedKinds)
    {
        TestContext.Current.TestOutputHelper?.WriteLine("--- Input ---");
        TestContext.Current.TestOutputHelper?.WriteLine(input);
        TestContext.Current.TestOutputHelper?.WriteLine("--- Expected Tokens ---");
        TestContext.Current.TestOutputHelper?.WriteLine(string.Join(", ", expectedKinds));
        
        var lex = RunLexer(input);
        
        Assert.Equal(expectedKinds, lex.Select(token => token.Kind));
    }
}