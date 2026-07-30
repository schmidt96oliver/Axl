using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Taxl;

namespace Axl.Compiler.UnitTests.Taxl;

public partial class TaxlLexerTests(ITestOutputHelper output)
{
    public static ImmutableArray<TaxlToken> RunLexer(string input)
        => TaxlLexer.Lex(SourceFileView.FromText(input), new DiagnosticBag());
    
    
    private static void AssertKindsIgnoreTrivia(string input, params TaxlTokenKind[] expectedKinds)
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
    private static void AssertKinds(string input, params TaxlTokenKind[] expectedKinds)
    {
        TestContext.Current.TestOutputHelper?.WriteLine("--- Input ---");
        TestContext.Current.TestOutputHelper?.WriteLine(input);
        TestContext.Current.TestOutputHelper?.WriteLine("--- Expected Tokens ---");
        TestContext.Current.TestOutputHelper?.WriteLine(string.Join(", ", expectedKinds));
        
        var lex = RunLexer(input);
        
        Assert.Equal(expectedKinds, lex.Select(token => token.Kind));
    }


    [Theory]
    [InlineData("\"\"", TaxlTokenKind.String)]
    [InlineData("\"Hello\"", TaxlTokenKind.String)]
    public void String_EmitsStringToken(string input, params TaxlTokenKind[] expectedKinds)
        => AssertKindsIgnoreTrivia(input, expectedKinds);
    
    [Theory]
    [InlineData("\"", TaxlTokenKind.String)]
    [InlineData("\"Hello", TaxlTokenKind.String)]
    public void UnclosedString_EmitsStringToken(string input, params TaxlTokenKind[] expectedKinds)
        => AssertKindsIgnoreTrivia(input, expectedKinds);
    
    
    [Theory]
    [InlineData("//", TaxlTokenKind.Comment)]
    [InlineData("//hello", TaxlTokenKind.Comment)]
    [InlineData("#try //hello", 
        TaxlTokenKind.Directive, 
        TaxlTokenKind.Whitespace,
        TaxlTokenKind.Comment)]
    [InlineData("#try ID //hello", 
        TaxlTokenKind.Directive, 
        TaxlTokenKind.Whitespace,
        TaxlTokenKind.Identifier,
        TaxlTokenKind.Whitespace,
        TaxlTokenKind.Comment)]
    
    public void Comment_EmitsCommentToken(string input, params TaxlTokenKind[] expectedKinds)
        => AssertKinds(input, expectedKinds);
    
    [Theory]
    [InlineData("//#hello", TaxlTokenKind.Comment)]
    [InlineData("//#", TaxlTokenKind.Comment)]
    public void CommentedDirective_EmitsCommentToken(string input, params TaxlTokenKind[] expectedKinds)
        => AssertKinds(input, expectedKinds);
    
    [Theory]
    [InlineData("// \"", TaxlTokenKind.Comment)]
    [InlineData("// \" a \"", TaxlTokenKind.Comment)]
    public void CommentedString_EmitsCommentToken(string input, params TaxlTokenKind[] expectedKinds)
        => AssertKinds(input, expectedKinds);
    
    [Theory]
    [InlineData("// bla // asd", TaxlTokenKind.Comment)]
    [InlineData("// \" a \"", TaxlTokenKind.Comment)]
    public void CommentedComment_EmitsOneCommentToken(string input, params TaxlTokenKind[] expectedKinds)
        => AssertKinds(input, expectedKinds);
}