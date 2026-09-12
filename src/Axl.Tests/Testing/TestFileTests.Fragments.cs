using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TestFileTests
{
    public sealed class Fragments
    {
        [Fact]
        public void ValidSplit()
            => InlineSnapshot.Validate(StructureAndCode("""
                                                        //@check
                                                        first
                                                        //--- 2nd.axl
                                                        second
                                                        //--- 3rd.axl
                                                        third
                                                        //=== stdout
                                                        // stdout
                                                        """), """
                --> Directive: Check
                --- Code "" ---
                //@check
                first

                --- Code "2nd.axl" ---
                //--- 2nd.axl
                second

                --- Code "3rd.axl" ---
                //--- 3rd.axl
                third

                --- Output "stdout" ---
                //=== stdout
                // stdout
                """);
        
        [Fact]
        public void WhitespaceBeforeSplit_Accepted()
            => InlineSnapshot.Validate(StructureAndCode("""
                                                        //@check
                                                        first
                                                           //--- 2nd.axl
                                                        second
                                                          //=== stdout
                                                        // stdout
                                                        """), """
                --> Directive: Check
                --- Code "" ---
                //@check
                first

                --- Code "2nd.axl" ---
                   //--- 2nd.axl
                second

                --- Output "stdout" ---
                  //=== stdout
                // stdout
                """);
        [Fact]
        public void AfterText_Ignored_1()
            => InlineSnapshot.Validate(StructureAndCode("""
                                                        //@check
                                                        first //--- 2nd.axl
                                                        still first
                                                        """), """
                --> Directive: Check
                --- Code "" ---
                //@check
                first //--- 2nd.axl
                still first
                """);
        
        [Fact]
        public void AfterText_Ignored_2()
            => InlineSnapshot.Validate(StructureAndCode("""
                                                        //@check
                                                        first //=== stdout
                                                        still first
                                                        """), """
                --> Directive: Check
                --- Code "" ---
                //@check
                first //=== stdout
                still first
                """);
        [Fact]
        public void EmptyNames()
            => InlineSnapshot.Validate(StructureAndCode("""
                                                        //@check
                                                        first 
                                                        //---
                                                        second
                                                        //===
                                                        output
                                                        """), """
                --> Directive: Check
                --- Code "" ---
                //@check
                first 

                --- Code "" ---
                //---
                second

                --- Output "" ---
                //===
                output
                """);
        [Fact]
        public void SplitAtStart()
            => InlineSnapshot.Validate(StructureAndCode("""
                                                        //---   
                                                        //@check
                                                        first 
                                                        """), """
                --> Directive: Check
                --- Code "" ---
                //---   
                //@check
                first
                """);
    }
}