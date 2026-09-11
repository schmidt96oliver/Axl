using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TaxlTests
{
    public sealed class Fragments
    {
        [Fact]
        public void ValidSplit()
            => InlineSnapshot.Validate(StructureAndCode("""
                                                        first
                                                        //--- 2nd.axl
                                                        second
                                                        //--- 3rd.axl
                                                        third
                                                        //=== stdout
                                                        // stdout
                                                        """), """
                                                              --> Directives: 
                                                              --- Code "" ---
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
                                                        first
                                                           //--- 2nd.axl
                                                        second
                                                          //=== stdout
                                                        // stdout
                                                        """), """
                                                              --> Directives: 
                                                              --- Code "" ---
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
                                                        first //--- 2nd.axl
                                                        still first
                                                        """), """
                                                              --> Directives: 
                                                              --- Code "" ---
                                                              first //--- 2nd.axl
                                                              still first
                                                              """);
        
        [Fact]
        public void AfterText_Ignored_2()
            => InlineSnapshot.Validate(StructureAndCode("""
                                                        first //=== stdout
                                                        still first
                                                        """), """
                                                              --> Directives: 
                                                              --- Code "" ---
                                                              first //=== stdout
                                                              still first
                                                              """);
        [Fact]
        public void EmptyNames()
            => InlineSnapshot.Validate(StructureAndCode("""
                                                        first 
                                                        //---
                                                        second
                                                        //===
                                                        output
                                                        """), """
                                                              --> Directives: 
                                                              --- Code "" ---
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
                                                        first 
                                                        """), """
                                                              --> Directives: 
                                                              --- Code "" ---
                                                              //---   
                                                              first
                                                              """);
    }
}