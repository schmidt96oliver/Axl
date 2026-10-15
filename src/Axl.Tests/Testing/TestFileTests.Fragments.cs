using Axl.Compiler.Testing;
using Axl.Compiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Testing;

public sealed partial class TestFileTests
{
    public sealed class Fragments
    {
        private string Frags(string input)
        {
            var sourceText = SourceText.From(input);
            var testFile = TestFile.From(sourceText);

            return new Dump(sourceText)
                .AddFragments(testFile.Fragments).ToString();
        }

        [Fact]
        public void NoDelimiter()
            => InlineSnapshot.Validate(Frags("line 1\nline 2"), """
                ---------------------
                line 1
                line 2
                """);
        
        [Fact]
        public void Empty()
            => InlineSnapshot.Validate(Frags(""), "---------------------");
        
        [Fact]
        public void InSuccession()
            => InlineSnapshot.Validate(Frags("//---\n//---\n//---"), """
                ---------------------
                //---
                ---------------------
                //---
                ---------------------
                //---
                """);
        
        [Fact]
        public void AtStart()
            => InlineSnapshot.Validate(Frags("""
                                             //---
                                             line 1
                                             line 2
                                             """), """
                ---------------------
                //---
                line 1
                line 2
                """);
        [Fact]
        public void AtEnd()
            => InlineSnapshot.Validate(Frags("""
                                             line 1
                                             //---
                                             """), """
                ---------------------
                line 1
                ---------------------
                //---
                """);
        [Fact]
        public void SingleInMiddle()
            => InlineSnapshot.Validate(Frags("""
                                             line 1
                                             //---
                                             line 1
                                             """), """
                ---------------------
                line 1
                ---------------------
                //---
                line 1
                """);
        [Fact]
        public void Multiple()
            => InlineSnapshot.Validate(Frags("""
                                             line 1
                                             line 2
                                             //---
                                             line 1
                                             //---
                                             line 1 
                                             line 2
                                             //---
                                             line 1
                                             """), """
                ---------------------
                line 1
                line 2
                ---------------------
                //---
                line 1
                ---------------------
                //---
                line 1 
                line 2
                ---------------------
                //---
                line 1
                """);
    }
}