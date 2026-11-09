using AxlCompiler.Diagnostics;
using AxlCompiler.Syntax;
using AxlCompiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

public partial class ParserTests
{
    public sealed class ContinuationProbing
    {
        private static string TokenKinds(string input)
        {
            input = input.RemoveCaret(out var caret);
            var tokens = Parser.InsertContinuationProbe(Lexer.Lex(SourceText.From(input), DiagnosticBag.Discard), caret);
            return string.Join(' ', tokens.Select(t => t.Kind));
        }

        private static string Continuations(string input)
        {
            var text = SourceText.From(input.RemoveCaret(out var caret));
            var continuations = Parser.ContinuationsAt(text, caret);
            return string.Join(" ", continuations.GetKinds()
                .OrderByDescending(SyntaxFacts.IsKeyword)
                .Select(kind => SyntaxFacts.GetText(kind) ?? kind.ToString()));
        }

        [Fact]
        public void InsertProbe_Empty()
            => InlineSnapshot.Validate(TokenKinds("|"), "ContinuationProbe Eof");
        
        [Fact]
        public void InsertProbe_Start()
            => InlineSnapshot.Validate(TokenKinds("|abc abc abc"), "ContinuationProbe Identifier Whitespace Identifier Whitespace Identifier Eof");
        
        [Fact]
        public void InsertProbe_Between_1()
            => InlineSnapshot.Validate(TokenKinds("abc |abc"), "Identifier Whitespace ContinuationProbe Identifier Eof");
        
        [Fact]
        public void InsertProbe_Between_2()
            => InlineSnapshot.Validate(TokenKinds("abc| abc"), "ContinuationProbe Identifier Whitespace Identifier Eof");
        
        [Fact]
        public void InsertProbe_End()
            => InlineSnapshot.Validate(TokenKinds("abc (|"), "Identifier Whitespace OpenParen ContinuationProbe Eof");
        
        [Fact]
        public void InsertProbe_AfterId()
            => InlineSnapshot.Validate(TokenKinds("abc abc|"), "Identifier Whitespace ContinuationProbe Identifier Eof");
        
        [Fact]
        public void InsertProbe_AfterKeyword()
            => InlineSnapshot.Validate(TokenKinds("abc true|"), "Identifier Whitespace ContinuationProbe TrueKw Eof");
        
        [Fact]
        public void InsertProbe_Inside_1()
            => InlineSnapshot.Validate(TokenKinds("abc abc gef ab|c"), "Identifier Whitespace Identifier Whitespace Identifier Whitespace ContinuationProbe Identifier Eof");
        
        [Fact]
        public void InsertProbe_Inside_2()
            => InlineSnapshot.Validate(TokenKinds("a|bc abc"), "ContinuationProbe Identifier Whitespace Identifier Eof");
        
        [Fact]
        public void InsertProbe_Inside_3()
            => InlineSnapshot.Validate(TokenKinds("a|bc"), "ContinuationProbe Identifier Eof");
        
        [Fact]
        public void InsertProbe_Inside_4()
            => InlineSnapshot.Validate(TokenKinds("abc a|bc"), "Identifier Whitespace ContinuationProbe Identifier Eof");


        [Fact]
        public void Continuations_Empty()
            => InlineSnapshot.Validate(Continuations("|"), """
                fun var let namespace return if while break continue using struct pub static self mut true false Identifier NumberLiteral " ! @ ( { -
                """);

        [Fact]
        public void Continuations_Var1()
            => InlineSnapshot.Validate(Continuations("let |"), "Identifier");

        [Fact]
        public void Continuations_Var2()
            => InlineSnapshot.Validate(Continuations("var |"), "Identifier");
        
        [Fact]
        public void Continuations_Var3()
            => InlineSnapshot.Validate(Continuations("let a|"), "Identifier");
        
        [Fact]
        public void Continuations_Var32()
            => InlineSnapshot.Validate(Continuations("let a |"), "; : =");
        
        [Fact]
        public void Continuations_Var4()
            => InlineSnapshot.Validate(Continuations("let a: |"), "Identifier");
        
        [Fact]
        public void Continuations_Var5()
            => InlineSnapshot.Validate(Continuations("let a = |"), """
                return if break continue self mut true false Identifier NumberLiteral " ! ( { -
                """);

        [Fact]
        public void Continuations_Members1()
            => InlineSnapshot.Validate(Continuations("pub |"), "fun struct static mut");

        [Fact]
        public void Continuations_Members2()
            => InlineSnapshot.Validate(Continuations("static |"), "fun mut");

        [Fact]
        public void Continuations_Members3()
            => InlineSnapshot.Validate(Continuations("@foo |"), "fun struct pub static mut @");

        [Fact]
        public void Continuations_Members4()
            => InlineSnapshot.Validate(Continuations("using |"), "Identifier");

        [Fact]
        public void Continuations_Members5()
            => InlineSnapshot.Validate(Continuations("namespace |"), "Identifier");

        [Fact]
        public void Continuations_Fun1()
            => InlineSnapshot.Validate(Continuations("fun |"), "Identifier ! + - * / == != < <= > >=");

        [Fact]
        public void Continuations_Fun2()
            => InlineSnapshot.Validate(Continuations("fun foo(|"), "mut Identifier : )");

        [Fact]
        public void Continuations_Fun3()
            => InlineSnapshot.Validate(Continuations("fun foo(mut |"), "mut Identifier :");

        [Fact]
        public void Continuations_Fun4()
            => InlineSnapshot.Validate(Continuations("fun foo(a: |"), "Identifier");

        [Fact]
        public void Continuations_Fun5()
            => InlineSnapshot.Validate(Continuations("fun foo(a: i32) |"), "; : = {");

        [Fact]
        public void Continuations_Fun6()
            => InlineSnapshot.Validate(Continuations("fun foo(a: i32): |"), "mut Identifier");

        [Fact]
        public void Continuations_Fun7()
            => InlineSnapshot.Validate(Continuations("fun S[|"), "Identifier");

        [Fact]
        public void Continuations_Fun8()
            => InlineSnapshot.Validate(Continuations("fun S[| () { }"), "Identifier");

        [Fact]
        public void Continuations_Struct1()
            => InlineSnapshot.Validate(Continuations("struct S[|"), "Identifier");

        [Fact]
        public void Continuations_Struct2()
            => InlineSnapshot.Validate(Continuations("struct S { | }"), "fun pub static mut Identifier @ }");

        [Fact]
        public void Continuations_If1()
            => InlineSnapshot.Validate(Continuations("fun main() { if (x) {} | }"), """
                var let return if else while break continue self mut true false Identifier NumberLiteral " . ; && || ! = ( { } [ + - * / == != < <= > >=
                """);

        [Fact]
        public void Continuations_If2()
            => InlineSnapshot.Validate(Continuations("fun main() { if (| }"), """
                return if break continue self mut true false Identifier NumberLiteral " ! ( { -
                """);

        [Fact]
        public void Continuations_If3()
            => InlineSnapshot.Validate(Continuations("fun main() { if (x) {} el| }"), """
                var let return if else while break continue self mut true false Identifier NumberLiteral " . ; && || ! = ( { } [ + - * / == != < <= > >=
                """);

        [Fact]
        public void Continuations_AfterError1()
            => InlineSnapshot.Validate(Continuations("## |"), """
                fun var let return if while break continue struct pub static self mut true false Identifier NumberLiteral " ! @ ( { -
                """);
        [Fact]
        public void Continuations_AfterError2()
            => InlineSnapshot.Validate(Continuations("## pu|b"), """
                fun var let return if while break continue struct pub static self mut true false Identifier NumberLiteral " ! @ ( { -
                """);
        [Fact]
        public void Continuations_AfterError3()
            => InlineSnapshot.Validate(Continuations("#|"), """
                fun var let return if while break continue struct pub static self mut true false Identifier NumberLiteral " ! @ ( { -
                """);
        [Fact]
        public void Continuations_AfterError4()
            => InlineSnapshot.Validate(Continuations("## pub |"), "fun struct static mut");
    }
}