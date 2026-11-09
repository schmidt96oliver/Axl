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
            .Select(grp => string.Join(" ", grp.Select(item => item.Label)))
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
            .Select(grp => string.Join(" ", grp.Select(item => item.Label)))
            .Order();
        return string.Join("\n", itemTexts);
    }

    [Fact]
    public void Empty()
        => InlineSnapshot.Validate(Items("|"), "fun var let namespace return if while using struct pub true false");

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
    public void Nother_AfterLet()
        => InlineSnapshot.Validate(ItemsWithBase("let a = 1 |"), "");

    [Fact]
    public void InStruct()
        => InlineSnapshot.Validate(ItemsWithBase("struct S { | }"), "fun pub static mut");
    
    [Fact]
    public void AfterPub1()
        => InlineSnapshot.Validate(ItemsWithBase("pub |"), "fun struct");
    [Fact]
    public void AfterPub2()
        => InlineSnapshot.Validate(ItemsWithBase("namespace Test; pub |"), "fun struct");

    [Fact]
    public void Members1()
        => InlineSnapshot.Validate(ItemsWithBase("""
                                                 struct Test
                                                 { 
                                                    pub Yes1: I32; Nope1: F64;
                                                    pub fun Yes2() { }
                                                    fun Nope2() { }
                                                    pub static fun Nope3() { }
                                                    static fun Nope4() { }
                                                 }
                                                 
                                                 let t: Test;   // Never mind the missing initializer
                                                 t.|
                                                 """), """
            Yes1
            Yes2
            """);
    [Fact]
    public void Members2()
        => InlineSnapshot.Validate(ItemsWithBase("""
                                                 struct Test
                                                 { 
                                                    pub Nope0: I32; Nope1: F64;
                                                    pub fun Nop2() { }
                                                    fun Nope3() { }
                                                    pub static fun Yes() { }
                                                    static fun Nope4() { }
                                                 }
                                                 
                                                 Test.|
                                                 """), "Yes");
    [Fact]
    public void Members_ColorColor()
        => InlineSnapshot.Validate(ItemsWithBase("""
                                                 struct Test
                                                 {
                                                    pub Yes1: I32; Nope1: F64;
                                                    pub fun Yes2() { }
                                                    fun Nope2() { }
                                                    pub static fun Yes3() { }
                                                    static fun Nope3() { }
                                                 }
                                                 
                                                 struct Outer 
                                                 {
                                                    pub Test: Test;
                                                    pub fun Do()
                                                    { Test.| }
                                                 }
                                                 """), """
            Yes1
            Yes2
            Yes3
            """);
    
    [Fact]
    public void AfterIf1()
        => InlineSnapshot.Validate(Items("if (c) {} |"), "fun var let return if else while struct pub true false");
    
    [Fact]
    public void AfterIf2()
        => InlineSnapshot.Validate(Items("if (c) {}; |"), "fun var let return if else while struct pub true false");

    [Fact]
    public void StringInterpolation1()
        => InlineSnapshot.Validate(Items("let a = 10; \"te{|\";"), """
            a
            return if true false
            """);
    
    [Fact]
    public void StringInterpolation2()
        => InlineSnapshot.Validate(Items("let a = 10; \"te{|"), """
            a
            return if true false
            """);

    [Theory]
    [InlineData("|", true)]
    [InlineData("namespace Test; |", false)]
    [InlineData("using Test; |", false)]
    [InlineData("let a = 1; |", false)]
    [InlineData("{ | }", false)]
    public void Namespace(string input, bool available)
    {
        if (available) ItemsWithBase(input).ShouldContain("namespace");
        else ItemsWithBase(input).ShouldNotContain("namespace");
    }

    [Fact]
    public void NamespaceFile()
        => InlineSnapshot.Validate(ItemsWithBase("namespace Test; |"), "fun using struct pub");
    
    [Theory]
    [InlineData("|", true)]
    [InlineData("namespace Test; |", true)]
    [InlineData("using Test; |", true)]
    [InlineData("using Test; let a = 1; |", false)]
    [InlineData("let a = 1; |", false)]
    [InlineData("{ | }", false)]
    public void Using(string input, bool available)
    {
        if (available) ItemsWithBase(input).ShouldContain("using");
        else ItemsWithBase(input).ShouldNotContain("using");
    }
    
    [Theory]
    [InlineData("|", false)]
    [InlineData("namespace Test; |", false)]
    [InlineData("struct Test { | }", true)]
    [InlineData("fun A(|", true)]
    [InlineData("fun A(m|", true)]
    [InlineData("fun A(m|u", true)]
    [InlineData("fun A(mut |", false)]
    [InlineData("fun A()|", false)]
    [InlineData("fun A(): |", true)]
    [InlineData("fun A(): m|", true)]
    [InlineData("fun A(): m|u", true)]
    [InlineData("fun A(): mut |", false)]
    public void Mut_Declaration(string input, bool available)
    {
        if (available) ItemsWithBase(input).ShouldContain("mut");
        else ItemsWithBase(input).ShouldNotContain("mut");
    }
    
    [Theory]
    [InlineData("let a = 1; |", false)]
    [InlineData("let a = |", false)]
    [InlineData("{ | }", false)]
    [InlineData("Call(|", true)]
    [InlineData("Call(m|", true)]
    [InlineData("Call(m|u", true)]
    [InlineData("Call(mut |", false)]
    [InlineData("Call(a, |", true)]
    [InlineData("Call(a, m|", true)]
    [InlineData("Call(a, mut |", false)]
    [InlineData("return |", false)]
    [InlineData("fun A() { return | }", true)]
    [InlineData("fun A() { return m| }", true)]
    [InlineData("fun A() { return m|u }", true)]
    [InlineData("fun A() { return mut | }", false)]
    [InlineData("fun A() = |", true)]
    [InlineData("fun A() = m|", true)]
    [InlineData("fun A() = m|u", true)]
    [InlineData("fun A() = mut |", false)]
    public void Mut_Stmt(string input, bool available)
    {
        if (available) ItemsWithBase(input).ShouldContain("mut");
        else ItemsWithBase(input).ShouldNotContain("mut");
    }
    
    [Theory]
    [InlineData("|", false)]
    [InlineData("| fun A(){}", false)]
    [InlineData("namespace A; |", false)]
    [InlineData("struct A { | }", true)]
    [InlineData("struct A { | fun A(){} }", true)]
    public void Static(string input, bool available)
    {
        if (available) ItemsWithBase(input).ShouldContain("static");
        else ItemsWithBase(input).ShouldNotContain("static");
    }
    
    [Theory]
    [InlineData("|", false)] 
    [InlineData("fun A() { | }", false)]
    [InlineData("namespace Test; fun A() { | }", false)]
    [InlineData("fun A() { let a = | }", false)]
    [InlineData("struct A { | }", false)]
    [InlineData("struct A { fun T() { | } }", true)]
    [InlineData("struct A { static fun T() { | } }", false)]
    public void Self(string input, bool available)
    {
        if (available) ItemsWithBase(input).ShouldContain("self");
        else ItemsWithBase(input).ShouldNotContain("self");
    }
    
    [Theory]
    [InlineData("|", false)]
    [InlineData("{ | }", false)]
    [InlineData("let a = |", false)]
    [InlineData("while (true) { | }", true)]
    [InlineData("while (true) { let a = | }", true)]
    [InlineData("fun A() { | }", false)]
    [InlineData("fun A() { let a = | }", false)]
    [InlineData("fun A() { return | }", false)]
    [InlineData("fun A() { while (true) { | }", true)]
    public void BreakContinue(string input, bool available)
    {
        if (available)
        {
            ItemsWithBase(input).ShouldSatisfyAllConditions(
                i => i.ShouldContain("break"), i => i.ShouldContain("continue"));
        }
        else
        {
            ItemsWithBase(input).ShouldSatisfyAllConditions(
                i => i.ShouldNotContain("break"), i => i.ShouldNotContain("continue"));
        }
    }
}