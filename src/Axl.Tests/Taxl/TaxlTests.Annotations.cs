using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TaxlTests
{
    public sealed class Annotations
    {
        [Fact]
        public void ErrorAndLint_Valid()
            => InlineSnapshot.Validate(Taxl("""
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
                a; //~ error ID
                a; //~error ID
                b; //~ lint ID
                b; //~lint ID
                """);
        
        [Fact]
        public void ErrorAndLint_EmptyId()
            => InlineSnapshot.Validate(Taxl("""
                                            a; //~error
                                            b; //~lint
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ Error@l.0: ""
                --> //~ Lint@l.1: ""
                a; //~error
                b; //~lint
                """);
        
        [Fact]
        public void Type_EmptyId()
            => InlineSnapshot.Validate(Taxl("""
                                                    a;
                                            //~type ^
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ type "" on "a"
                        a;
                //~type ^
                """);
        
        [Fact]
        public void Type_Valid_1()
            => InlineSnapshot.Validate(Taxl("""
                                            var a = 2;
                                            //~type ^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ type "i32" on "2"
                var a = 2;
                //~type ^ i32
                """);
        
        [Fact]
        public void Type_Valid_2()
            => InlineSnapshot.Validate(Taxl("""
                                            var a = 2;
                                            //~ type^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ type "i32" on "2"
                var a = 2;
                //~ type^ i32
                """);
        
        [Fact]
        public void Type_MissingCarets()
            => InlineSnapshot.Validate(Taxl("""
                                            var a = 2;
                                            //~ type i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ INVALID: Type annotation must use '^' to point at an expression.
                var a = 2;
                //~ type i32
                """);
        [Fact]
        public void Type_FirstLine()
            => InlineSnapshot.Validate(Taxl("""
                                            //~type ^^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ INVALID: Type annotation with carets on first line. It cannot point to line above.
                //~type ^^ i32
                """);
        
        [Fact]
        public void Type_RefOutOfBounds()
            => InlineSnapshot.Validate(Taxl("""
                                            a;
                                            //~type ^^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ INVALID: Type annotation does not reference a valid position in line above.
                a;
                //~type ^^ i32
                """);
        [Fact]
        public void Type_DoubledCarets()
            => InlineSnapshot.Validate(Taxl("""
                                                    1  +  2  +  3;
                                            //~type ^^ ^^ ^^ ^^ i32
                                            """), """
                --> Directives: 
                --- Code "" ---
                --> //~ INVALID: Type annotation can only contain one block of carets.
                        1  +  2  +  3;
                //~type ^^ ^^ ^^ ^^ i32
                """);
    }
}