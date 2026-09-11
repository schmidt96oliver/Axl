using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TaxlTests
{
    public sealed class Directives
    {
        [Fact]
        public void Valid()
            => InlineSnapshot.Validate(Structure("""
                                            //@    run-pass   
                                            //@check  
                                            //@ run-panic
                                            """), """
                --> Directives: RunPass, Check, RunPanic
                --- Code "" ---
                """);

        [Fact]
        public void AfterWhitespace_Accepted()
            => InlineSnapshot.Validate(Structure("""
                                               //@run-pass
                                             //@check
                                               //@run-panic
                                            """), """
                --> Directives: RunPass, Check, RunPanic
                --- Code "" ---
                """);

        [Fact]
        public void AfterText_Ignored()
            => InlineSnapshot.Validate(Structure("""
                                            //@run-pass
                                            test;
                                            //@check
                                            """), """
                --> Directives: RunPass
                --- Code "" ---
                """);

        [Fact]
        public void BetweenComments_Accepted()
            => InlineSnapshot.Validate(Structure("""
                                            //@run-pass
                                            // just a comment
                                            //@check
                                            """), """
                --> Directives: RunPass, Check
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
                --> Directives: Check
                --- Code "" ---
                --- Code "" ---
                --- Output "" ---
                """);

        [Fact]
        public void Unknown()
            => InlineSnapshot.Validate(Structure("""
                                            //@
                                            //@ bla
                                            """), """
                --> Directives: Unknown, Unknown
                --- Code "" ---
                """);
    }
}