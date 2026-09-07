using Axl.Compiler;
using Axl.Compiler.Taxl;

namespace Axl.Tests.Taxl;

public sealed partial class TaxlTests
{
    private static string Taxl(string input)
    {
        var source = SourceFileView.FromText(input);
        var taxlFile = TaxlFile.Parse(source);

        return new Dump(source).Add(taxlFile).ToString();
    }
}