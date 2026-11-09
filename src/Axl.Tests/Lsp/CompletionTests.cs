using AxlCompiler;
using AxlCompiler.Text;
using AxlLsp.Handlers;
using Meziantou.Framework.InlineSnapshotTesting;
using Shouldly;


namespace Axl.Tests.Lsp;

public sealed class CompletionTests
{
    private static string Items(string input)
    {
        input = input.RemoveCaret(out var caret);
        var sourceText = SourceText.From(input);
        var compilation = Compilation.From(sourceText).WithBaseLibrary();

        var itemTexts = CompletionHandler
            .GetCompletionItems(SourceLocation.EmptyAt(sourceText, caret), compilation, excludeReferencedCompilations: true)
            .GroupBy(item => item.Kind)
            .Select(grp => $"{grp.Key}: { string.Join(", ", grp.Select(item => item.Label)) }")
            .Order();
        return string.Join("\n", itemTexts);
    }
    
    private static string ItemsWithBase(string input)
    {
        input = input.RemoveCaret(out var caret);
        var sourceText = SourceText.From(input);
        var compilation = Compilation.From(sourceText).WithBaseLibrary();

        var itemTexts = CompletionHandler
            .GetCompletionItems(SourceLocation.EmptyAt(sourceText, caret), compilation, excludeReferencedCompilations: false)
            .GroupBy(item => item.Kind)
            .Select(grp => $"{grp.Key}: { string.Join(", ", grp.Select(item => item.Label)) }")
            .Order();
        return string.Join("\n", itemTexts);
    }

    [Fact]
    public void Empty()
        => InlineSnapshot.Validate(Items("|"), "Keyword: fun, var, let, namespace, return, if, while, break, continue, using, struct, pub, static, self, mut, true, false");

    [Fact]
    public void Nothing_Comment1()
        => InlineSnapshot.Validate(ItemsWithBase("// p|"), "");

    [Fact]
    public void Nothing_Comment2()
        => InlineSnapshot.Validate(ItemsWithBase("let a = 1; // ab|"), "");

    [Fact]
    public void Nothing_String1()
        => InlineSnapshot.Validate(ItemsWithBase("\"te|xt\";"), "");
    [Fact]
    public void Nothing_String2()
        => InlineSnapshot.Validate(ItemsWithBase("\"|text\";"), "");
    
    [Fact]
    public void Nothing_String3()
        => InlineSnapshot.Validate(ItemsWithBase("\"text|\";"), "");
    
    [Fact]
    public void Nothing_String4()
        => InlineSnapshot.Validate(ItemsWithBase("\"te|{\";"), "");

    [Fact]
    public void Nothing_AfterExpr()
        => InlineSnapshot.Validate(ItemsWithBase("1|;"), "");

    [Fact]
    public void Nothing_LetName()
        => InlineSnapshot.Validate(ItemsWithBase("let |"), "");

    [Fact]
    public void Nothing_FunName()
        => InlineSnapshot.Validate(ItemsWithBase("fun |"), "");

    [Fact]
    public void Nothing_StructName()
        => InlineSnapshot.Validate(ItemsWithBase("struct |"), "");

    [Fact]
    public void Nothing_NamespaceName()
        => InlineSnapshot.Validate(ItemsWithBase("struct |"), "");


    [Fact]
    public void StringInterpolation1()
        => InlineSnapshot.Validate(Items("let a = 10; \"te{|\";"), """
            Function: a
            Keyword: return, if, break, continue, self, mut, true, false
            """);
    
    [Fact]
    public void StringInterpolation2()
        => InlineSnapshot.Validate(Items("let a = 10; \"te{|"), """
            Function: a
            Keyword: return, if, break, continue, self, mut, true, false
            """);

    [Theory]
    [InlineData("|", true)]
    [InlineData("namespace Test; |", false)]
    [InlineData("using Test; |", false)]
    [InlineData("let a = 1; |", false)]
    [InlineData("{ | }", false)]
    public void Namespace(string input, bool namespaceAvailable)
    {
        if (namespaceAvailable) ItemsWithBase(input).ShouldContain("namespace");
        else ItemsWithBase(input).ShouldNotContain("namespace");
    }

    [Fact]
    public void NamespaceFile()
        => InlineSnapshot.Validate(ItemsWithBase("namespace Test; |"), "Keyword: fun, using, struct, pub, static, mut");
    
    [Theory]
    [InlineData("|", true)]
    [InlineData("namespace Test; |", true)]
    [InlineData("using Test; |", true)]
    [InlineData("using Test; let a = 1; |", false)]
    [InlineData("let a = 1; |", false)]
    [InlineData("{ | }", false)]
    public void Using(string input, bool namespaceAvailable)
    {
        if (namespaceAvailable) ItemsWithBase(input).ShouldContain("using");
        else ItemsWithBase(input).ShouldNotContain("using");
    }
}