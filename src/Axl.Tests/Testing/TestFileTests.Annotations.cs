using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TestFileTests
{
    public sealed class Annotations
    {
        [Fact]
        public void Empty()
            => InlineSnapshot.Validate(Structure("""
                                                 //@check
                                                 //~
                                                 """), """
                ERROR InvalidTaxlAnnotation@[10, 13): Annotation '//~' is invalid.
                --> Directive: Check
                --- Code "" ---
                """);
        [Fact]
        public void Error_AtEof()
            => InlineSnapshot.Validate(Structure("""
                                                 //@check
                                                 a; //~error
                                                 """), """
                ERROR InvalidTaxlAnnotation@[13, 21): Annotation '//~error' is invalid.
                --> Directive: Check
                --- Code "" ---
                """);
        [Fact]
        public void Lint_AtEof()
            => InlineSnapshot.Validate(Structure("""
                                                 //@check
                                                 a; //~lint
                                                 """), """
                ERROR InvalidTaxlAnnotation@[13, 20): Annotation '//~lint' is invalid.
                --> Directive: Check
                --- Code "" ---
                """);
        [Fact]
        public void Type_AtEof()
            => InlineSnapshot.Validate(Structure("""
                                                 //@check
                                                 //~type
                                                 """), """
                ERROR InvalidTaxlAnnotation@[10, 17): Annotation '//~type' is invalid.
                --> Directive: Check
                --- Code "" ---
                """);
        
        [Fact]
        public void ErrorAndLint_Valid()
            => InlineSnapshot.Validate(Structure("""
                                            //@check
                                            a; //~error ID
                                            b; //~lint ID
                                            """), """
                --> Directive: Check
                --- Code "" ---
                --> //~ Error@l.1: "ID"
                --> //~ Lint@l.2: "ID"
                """);
        
        [Fact]
        public void ErrorAndLint_EmptyId()
            => InlineSnapshot.Validate(Structure("""
                                            //@check
                                            a; //~error
                                            b; //~lint
                                            """), """
                ERROR InvalidTaxlAnnotation@[13, 21): Annotation '//~error' is invalid.
                ERROR InvalidTaxlAnnotation@[26, 33): Annotation '//~lint' is invalid.
                --> Directive: Check
                --- Code "" ---
                """);
        
        [Fact]
        public void Type_EmptyId()
            => InlineSnapshot.Validate(Structure("""
                                                 //@check
                                                         a;
                                                 //~type ^
                                                 """), """
                ERROR InvalidTaxlAnnotation@[22, 31): Annotation '//~type ^' is invalid.
                --> Directive: Check
                --- Code "" ---
                """);
        
        [Fact]
        public void Type_Valid_1()
            => InlineSnapshot.Validate(Structure("""
                                            //@run-pass
                                            var a = 2;
                                            //~type ^ i32
                                            """), """
                --> Directive: RunPass
                --- Code "" ---
                --> //~ type "i32" on "2"
                """);
        
        [Fact]
        public void Type_Valid_2()
            => InlineSnapshot.Validate(Structure("""
                                            //@run-pass
                                            var a = 2;
                                             //~type^ i32
                                            """), """
                --> Directive: RunPass
                --- Code "" ---
                --> //~ type "i32" on "2"
                """);
        
        [Fact]
        public void Type_MissingCarets()
            => InlineSnapshot.Validate(Structure("""
                                            //@run-panic
                                            var a = 2;
                                            //~type i32
                                            """), """
                ERROR InvalidTaxlAnnotation@[26, 37): Annotation '//~type i32' is invalid.
                --> Directive: RunPanic
                --- Code "" ---
                """);
        [Fact]
        public void Type_FirstLine()
            => InlineSnapshot.Validate(Structure("""
                                            //~type ^^ i32
                                            //@check
                                            """), """
                ERROR InvalidTaxlAnnotation@[0, 14): Annotation '//~type ^^ i32' is invalid.
                --> Directive: Check
                --- Code "" ---
                """);
        
        [Fact]
        public void Type_RefOutOfBounds()
            => InlineSnapshot.Validate(Structure("""
                                            //@check
                                            a;
                                            //~type ^^ i32
                                            """), """
                ERROR InvalidTaxlAnnotation@[14, 28): Annotation '//~type ^^ i32' is invalid.
                --> Directive: Check
                --- Code "" ---
                """);
        [Fact]
        public void Type_DoubledCarets()
            => InlineSnapshot.Validate(Structure("""
                                            //@check
                                                    1  +  2  +  3;
                                            //~type ^^ ^^ ^^ ^^ i32
                                            """), """
                ERROR InvalidTaxlAnnotation@[34, 57): Annotation '//~type ^^ ^^ ^^ ^^ i32' is invalid.
                --> Directive: Check
                --- Code "" ---
                """);
    }
}