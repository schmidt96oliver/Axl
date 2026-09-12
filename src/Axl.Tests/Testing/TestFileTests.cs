using Axl.Compiler.Testing;
using Axl.Compiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TestFileTests
{
    private static string Structure(string input)
    {
        var sourceText = SourceText.From(input);
        var testFile = TestFile.From(sourceText);

        return new Dump(sourceText)
            .Add(testFile.Diagnostics)
            .Add(testFile, onlyStructure: true).ToString();
    }

    private static string StructureAndCode(string input)
    {
        var sourceText = SourceText.From(input);
        var testFile = TestFile.From(sourceText);

        return new Dump(sourceText)
            .Add(testFile.Diagnostics)
            .Add(testFile, onlyStructure: false).ToString();
    }

    [Fact]
    public void Empty()
        => InlineSnapshot.Validate(StructureAndCode(""), """
            ERROR MissingTaxlDirective@[0, 0): Test directive missing.
            --> Directive: ???
            --- Code "" ---
            """);

    [Fact]
    public void Whitespace()
        => InlineSnapshot.Validate(StructureAndCode("   "), """
            ERROR MissingTaxlDirective@[0, 3): Test directive missing.
            --> Directive: ???
            --- Code "" ---
            """);
}