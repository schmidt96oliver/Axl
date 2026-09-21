using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Testing;

public sealed partial class TestFileTests
{
    public sealed class Directives
    {
        [Fact]
        public void Check_Valid()
            => InlineSnapshot.Validate(Structure("""
                                            //@check  
                                            """), "Directive: Check");
        [Fact]
        public void Run_Valid()
            => InlineSnapshot.Validate(Structure("""
                                            //@run  
                                            """), "Directive: Run");
        [Fact]
        public void AfterWhitespace_Accepted()
            => InlineSnapshot.Validate(Structure("""
                                               //@run
                                            """), "Directive: Run");

        [Fact]
        public void Multiples_Ignored()
            => InlineSnapshot.Validate(Structure("""
                                            //@run
                                            //@check
                                            """), "Directive: Run");
        [Fact]
        public void AfterText_Ignored()
            => InlineSnapshot.Validate(Structure("""
                                            bla
                                            //@run
                                            """), """
                ERROR MissingTaxlDirective@[0, 3): Test directive missing.
                Directive: ???
                """);

        [Fact]
        public void AfterComments_Accepted()
            => InlineSnapshot.Validate(Structure("""
                                            // just a comment
                                            //@check
                                            """), "Directive: Check");

        [Fact]
        public void Empty()
            => InlineSnapshot.Validate(Structure("//@"), """
                ERROR UnknownTaxlDirective@[0, 3): Directive '//@' is not known.
                Directive: ???
                """);
        
        [Fact]
        public void Unknown()
            => InlineSnapshot.Validate(Structure("//@bla"), """
                ERROR UnknownTaxlDirective@[0, 6): Directive '//@bla' is not known.
                Directive: ???
                """);
    }
}