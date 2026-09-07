using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TaxlTests
{
    public sealed class Annotations
    {
        [Fact]
        public void ErrorAndLint_Valid()
            => InlineSnapshot.Validate(Structure("""
                                            a; //~ error ID
                                            a; //~error ID
                                            b; //~ lint ID
                                            b; //~lint ID
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ Error@l.0: "ID"
                --> //~ Error@l.1: "ID"
                --> //~ Lint@l.2: "ID"
                --> //~ Lint@l.3: "ID"
                """);
        
        [Fact]
        public void ErrorAndLint_EmptyId()
            => InlineSnapshot.Validate(Structure("""
                                            a; //~error
                                            b; //~lint
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ Error@l.0: ""
                --> //~ Lint@l.1: ""
                """);
        
        [Fact]
        public void Type_EmptyId()
            => InlineSnapshot.Validate(Structure("""
                                                    a;
                                            //~type ^
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ type "" on "a"
                """);
        
        [Fact]
        public void Type_Valid_1()
            => InlineSnapshot.Validate(Structure("""
                                            var a = 2;
                                            //~type ^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ type "i32" on "2"
                """);
        
        [Fact]
        public void Type_Valid_2()
            => InlineSnapshot.Validate(Structure("""
                                            var a = 2;
                                            //~ type^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ type "i32" on "2"
                """);
        
        [Fact]
        public void Type_MissingCarets()
            => InlineSnapshot.Validate(Structure("""
                                            var a = 2;
                                            //~ type i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ INVALID: Type annotation must use '^' to point at an expression.
                """);
        [Fact]
        public void Type_FirstLine()
            => InlineSnapshot.Validate(Structure("""
                                            //~type ^^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ INVALID: Type annotation with carets on first line. It cannot point to line above.
                """);
        
        [Fact]
        public void Type_RefOutOfBounds()
            => InlineSnapshot.Validate(Structure("""
                                            a;
                                            //~type ^^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ INVALID: Type annotation does not reference a valid position in line above.
                """);
        [Fact]
        public void Type_DoubledCarets()
            => InlineSnapshot.Validate(Structure("""
                                                    1  +  2  +  3;
                                            //~type ^^ ^^ ^^ ^^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ INVALID: Type annotation can only contain one block of carets.
                """);
    }
}