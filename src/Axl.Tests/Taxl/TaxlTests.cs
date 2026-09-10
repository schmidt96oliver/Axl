using Axl.Compiler;
using Axl.Compiler.Taxl;
using Axl.Compiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Taxl;

public sealed partial class TaxlTests
{
    private static string Structure(string input)
    {
        var source = SourceFileView.FromText(input);
        var taxlFile = TaxlFile.Parse(source);

        return new Dump(source).Add(taxlFile, onlyStructure: true).ToString();
    }

    private static string StructureAndCode(string input)
    {
        var source = SourceFileView.FromText(input);
        var taxlFile = TaxlFile.Parse(source);

        return new Dump(source).Add(taxlFile, onlyStructure: false).ToString();
    }

    [Fact]
    public void Empty()
        => InlineSnapshot.Validate(StructureAndCode(""), """
                                                         --> Directives: 
                                                         --- Code "" ---
                                                         """);

    [Fact]
    public void Whitespace()
        => InlineSnapshot.Validate(StructureAndCode("   "), """
                                                            --> Directives: 
                                                            --- Code "" ---
                                                            """);
}