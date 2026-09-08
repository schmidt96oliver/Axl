using Axl.Compiler;
using Axl.Compiler.Taxl;

namespace Axl.Tests.Corpus;

public sealed class CorpusTests
{
    [Theory, Corpus]
    public void T(string path)
    {
        var source = SourceFileView.FromFile(path);
        var taxl = TaxlFile.Parse(source);
        TaxlRunner.Test(taxl);
    }
}