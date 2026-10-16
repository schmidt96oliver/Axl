using Axl.Compiler.Testing;
using Axl.Compiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Testing;

public sealed class TestFileTests
{
    private static string Taxl(string input)
    {
        var sourceText = SourceText.From(input);
        var testFile = TestFile.From(sourceText);

        return new Dump(sourceText).Add(testFile).ToString();
    }

    [Fact]
    public void Empty()
        => InlineSnapshot.Validate(Taxl(""), """
            Kind: Error
            ----- Fragment -----
            """);

    [Fact]
    public void Whitespace()
        => InlineSnapshot.Validate(Taxl("   "), """
            Kind: Error
            ----- Fragment -----
            """);

    [Fact]
    public void Annotations()
        => InlineSnapshot.Validate(Taxl("""
                                             //@check
                                             //= Output
                                             a; //~error ID
                                             b; //~lint ID
                                             """), """
            Kind: Check
            CheckDirective "//@check": ""
            ExpectOutput "//=": " Output"
            ExpectError "//~error": " ID"
            ExpectLint "//~lint": " ID"
            ----- Fragment -----
            //@check
            //= Output
            a; //~error ID
            b; //~lint ID
            """);

    [Fact]
    public void Annotations_WithEmptyText()
        => InlineSnapshot.Validate(Taxl("""
                                         //@run
                                         //=
                                         a; //~error
                                         b; //~lint
                                         """), """
            Kind: Run
            RunDirective "//@run": ""
            ExpectOutput "//=": ""
            ExpectError "//~error": ""
            ExpectLint "//~lint": ""
            ----- Fragment -----
            //@run
            //=
            a; //~error
            b; //~lint
            """);

    [Fact]
    public void Annotation_InsideComment_Ignored()
        => InlineSnapshot.Validate(Taxl("""
                                             ///@run
                                             ///~
                                             // Comment //= test
                                             """), """
            Kind: Error
            ----- Fragment -----
            ///@run
            ///~
            // Comment //= test
            """);

    [Fact]
    public void Annotation_InsideString_Ignored()
        => InlineSnapshot.Validate(Taxl("""
                                             "//@run"
                                             "//= Output"
                                             "//~error"
                                             """), """
            Kind: Error
            ----- Fragment -----
            "//@run"
            "//= Output"
            "//~error"
            """);

    [Fact]
    public void RunCheck_MustBeSingleLine()
        => InlineSnapshot.Validate(Taxl("""
                                             //@run
                                             bla //@check
                                              //@run
                                             //@check
                                             """), """
            Kind: Run
            RunDirective "//@run": ""
            CheckDirective "//@check": ""
            ----- Fragment -----
            //@run
            bla //@check
             //@run
            //@check
            """);

    [Fact]
    public void InlineOutput()
        => InlineSnapshot.Validate(Taxl("""
                                             //@run
                                             a; //= Output 1
                                             b; //= Output 2
                                             """), """
            Kind: Run
            RunDirective "//@run": ""
            ExpectOutput "//=": " Output 1"
            ExpectOutput "//=": " Output 2"
            ----- Fragment -----
            //@run
            a; //= Output 1
            b; //= Output 2
            """);

    [Fact]
    public void Fragments_NoDelimiter()
        => InlineSnapshot.Validate(Taxl("line 1\nline 2"), """
            Kind: Error
            ----- Fragment -----
            line 1
            line 2
            """);

    [Fact]
    public void Fragments_InSuccession()
        => InlineSnapshot.Validate(Taxl("//---\n//---\n//---"), """
            Kind: Error
            ----- Fragment -----
            //---
            ----- Fragment -----
            //---
            ----- Fragment -----
            //---
            """);

    [Fact]
    public void Fragments_AtStart()
        => InlineSnapshot.Validate(Taxl("""
                                         //---
                                         line 1
                                         line 2
                                         """), """
            Kind: Error
            ----- Fragment -----
            //---
            line 1
            line 2
            """);

    [Fact]
    public void Fragments_AtEnd()
        => InlineSnapshot.Validate(Taxl("""
                                         line 1
                                         //---
                                         """), """
            Kind: Error
            ----- Fragment -----
            line 1
            ----- Fragment -----
            //---
            """);

    [Fact]
    public void Fragments_SingleInMiddle()
        => InlineSnapshot.Validate(Taxl("""
                                         line 1
                                         //---
                                         line 1
                                         """), """
            Kind: Error
            ----- Fragment -----
            line 1
            ----- Fragment -----
            //---
            line 1
            """);

    [Fact]
    public void Fragments_Multiple()
        => InlineSnapshot.Validate(Taxl("""
                                         line 1
                                         line 2
                                         //---
                                         line 1
                                         //---
                                         line 1 
                                         line 2
                                         //---
                                         line 1
                                         """), """
            Kind: Error
            ----- Fragment -----
            line 1
            line 2
            ----- Fragment -----
            //---
            line 1
            ----- Fragment -----
            //---
            line 1 
            line 2
            ----- Fragment -----
            //---
            line 1
            """);
}