using Axl.Compiler;
using Axl.Compiler.Taxl;
using Axl.Compiler.Text;

namespace Axl.Tests.Corpus;

public sealed class CorpusTests
{
    [Theory, Corpus]
    public void T(string path)
    {
        var source = SourceFileView.FromFile(path);
        var taxl = TaxlFile.From(source);
        TaxlRunner.Test(taxl);
    }
}