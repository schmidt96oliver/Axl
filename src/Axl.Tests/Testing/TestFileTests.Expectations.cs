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
        public void Second_Ignored()
            => InlineSnapshot.Validate(Structure("//@run\n//= Hello World\n//= Nope"), """
                Directive: Run
                Expectation: " Hello World"
                """);
        
        [Fact]
        public void AfterText_Ignored()
            => InlineSnapshot.Validate(Structure("//@run\nblupp //= Hello World"), "Directive: Run");
    }
}