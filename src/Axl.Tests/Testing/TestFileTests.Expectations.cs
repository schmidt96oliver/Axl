using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Testing;

public sealed partial class TestFileTests
{
    public sealed class Expectations
    {
        [Fact]
        public void Empty()
            => InlineSnapshot.Validate(Structure("//@run\n//="), """
                Directive: Run
                Expectation: ""
                """);
        
        [Fact]
        public void WithText()
            => InlineSnapshot.Validate(Structure("//@run\n//= Hello World"), """
                Directive: Run
                Expectation: " Hello World"
                """);
        
        [Fact]
        public void MultiLine()
            => InlineSnapshot.Validate(Structure("""
                                                 //@run
                                                 //= A
                                                 //= B
                                                 //= C
                                                 """), """
                Directive: Run
                Expectation: " A
                 B
                 C"
                """);
        [Fact]
        public void MultiLine_StoppedByNewline()
            => InlineSnapshot.Validate(Structure("""
                                                 //@run
                                                 //= A
                                                 
                                                 //= Nope, just a comment
                                                 """), """
                Directive: Run
                Expectation: " A"
                """);
        [Fact]
        public void MultiLine_StoppedByText()
            => InlineSnapshot.Validate(Structure("""
                                                 //@run
                                                 //= A
                                                 bla
                                                 //= Nope, just a comment
                                                 """), """
                Directive: Run
                Expectation: " A"
                """);
        
        [Fact]
        public void AfterText_Ignored()
            => InlineSnapshot.Validate(Structure("//@run\nblupp //= Hello World"), "Directive: Run");
    }
}