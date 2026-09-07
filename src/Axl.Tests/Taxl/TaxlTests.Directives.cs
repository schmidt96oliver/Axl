using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TaxlTests
{
    public sealed class Directives
    {
        [Fact]
        public void Valid()
            => InlineSnapshot.Validate(Taxl("""
                                            //@    run-pass   
                                            //@check  
                                            //@ run-panic
                                            """), """
                --> Directives: RunPass, Check, RunPanic
                --- Code "" ---
                //@    run-pass   
                //@check  
                //@ run-panic
                """);

        [Fact]
        public void AfterWhitespace_Accepted()
            => InlineSnapshot.Validate(Taxl("""
                                               //@run-pass
                                             //@check
                                               //@run-panic
                                            """), """
                --> Directives: RunPass, Check, RunPanic
                --- Code "" ---
                   //@run-pass
                 //@check
                   //@run-panic
                """);

        [Fact]
        public void AfterText_Ignored()
            => InlineSnapshot.Validate(Taxl("""
                                            //@run-pass
                                            test;
                                            //@check
                                            """), """
                --> Directives: RunPass
                --- Code "" ---
                //@run-pass
                test;
                //@check
                """);

        [Fact]
        public void BetweenComments_Accepted()
            => InlineSnapshot.Validate(Taxl("""
                                            //@run-pass
                                            // just a comment
                                            //@check
                                            """), """
                --> Directives: RunPass, Check
                --- Code "" ---
                //@run-pass
                // just a comment
                //@check
                """);

        [Fact]
        public void InFragments_Ignored()
            => InlineSnapshot.Validate(Taxl("""
                                            //@check
                                            //---
                                            //@run-pass
                                            //===
                                            //@run-panic
                                            """), """
                --> Directives: Check
                --- Code "" ---
                //@check

                --- Code "" ---
                //---
                //@run-pass

                --- Output "" ---
                //===
                //@run-panic
                """);

        [Fact]
        public void Unknown()
            => InlineSnapshot.Validate(Taxl("""
                                            //@
                                            //@ bla
                                            """), """
                --> Directives: Unknown, Unknown
                --- Code "" ---
                //@
                //@ bla
                """);
    }
}