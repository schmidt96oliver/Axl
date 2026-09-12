using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TestFileTests
{
    public sealed class Directives
    {
        [Fact]
        public void Check_Valid()
            => InlineSnapshot.Validate(Structure("""
                                            //@check  
                                            """), """
                --> Directive: Check
                --- Code "" ---
                """);
        [Fact]
        public void RunPass_Valid()
            => InlineSnapshot.Validate(Structure("""
                                            //@run-pass  
                                            """), """
                --> Directive: RunPass
                --- Code "" ---
                """);
        [Fact]
        public void RunPanic_Valid()
            => InlineSnapshot.Validate(Structure("""
                                            //@run-panic  
                                            """), """
                --> Directive: RunPanic
                --- Code "" ---
                """);

        [Fact]
        public void AfterWhitespace_Accepted()
            => InlineSnapshot.Validate(Structure("""
                                               //@run-pass
                                            """), """
                --> Directive: RunPass
                --- Code "" ---
                """);

        [Fact]
        public void Multiples_Ignored()
            => InlineSnapshot.Validate(Structure("""
                                            //@run-pass
                                            //@check
                                            """), """
                --> Directive: RunPass
                --- Code "" ---
                """);
        [Fact]
        public void AfterText_Ignored()
            => InlineSnapshot.Validate(Structure("""
                                            bla
                                            //@run-pass
                                            """), """
                ERROR MissingTaxlDirective@[0, 5): Test directive missing.
                --> Directive: ???
                --- Code "" ---
                """);

        [Fact]
        public void AfterComments_Accepted()
            => InlineSnapshot.Validate(Structure("""
                                            // just a comment
                                            //@check
                                            """), """
                --> Directive: Check
                --- Code "" ---
                """);

        [Fact]
        public void InFragments_Ignored()
            => InlineSnapshot.Validate(Structure("""
                                            //@check
                                            //---
                                            //@run-pass
                                            //===
                                            //@run-panic
                                            """), """
                --> Directive: Check
                --- Code "" ---
                --- Code "" ---
                --- Output "" ---
                """);

        [Fact]
        public void Empty()
            => InlineSnapshot.Validate(Structure("//@"), """
                ERROR UnknownTaxlDirective@[0, 3): Directive '//@' is not known.
                --> Directive: ???
                --- Code "" ---
                """);
        
        [Fact]
        public void Unknown()
            => InlineSnapshot.Validate(Structure("//@bla"), """
                ERROR UnknownTaxlDirective@[0, 6): Directive '//@bla' is not known.
                --> Directive: ???
                --- Code "" ---
                """);
    }
}