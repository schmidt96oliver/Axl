using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

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
            => InlineSnapshot.Validate(TokenKinds("abc| abc"), "Identifier ContinuationProbe Whitespace Identifier Eof");
        
        [Fact]
        public void InsertProbe_End()
            => InlineSnapshot.Validate(TokenKinds("abc abc gef abc|"), "Identifier Whitespace Identifier Whitespace Identifier Whitespace Identifier ContinuationProbe Eof");
        
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
    }
}