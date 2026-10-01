using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Testing;

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
                Directive: Check
                """);
        [Fact]
        public void Error_AtEof()
            => InlineSnapshot.Validate(Structure("""
                                                 //@check
                                                 a; //~error
                                                 """), """
                ERROR InvalidTaxlAnnotation@[13, 21): Annotation '//~error' is invalid.
                Directive: Check
                """);
        [Fact]
        public void Lint_AtEof()
            => InlineSnapshot.Validate(Structure("""
                                                 //@check
                                                 a; //~lint
                                                 """), """
                ERROR InvalidTaxlAnnotation@[13, 20): Annotation '//~lint' is invalid.
                Directive: Check
                """);
        [Fact]
        public void ErrorAndLint_Valid()
            => InlineSnapshot.Validate(Structure("""
                                            //@check
                                            a; //~error ID
                                            b; //~lint ID
                                            """), """
                Directive: Check
                //~ error@l.1: "ID"
                //~ lint@l.2: "ID"
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
                Directive: Check
                """);
    }
}