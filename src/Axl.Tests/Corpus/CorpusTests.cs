using Axl.Compiler.Testing;
using Axl.Compiler.Text;

namespace Axl.Tests.Corpus;

public sealed class CorpusTests
{
    [Theory, Corpus]
    public void T(string path)
    {
        var sourceText = SourceText.LoadFile(path);
        var evaluation = TestFile.From(sourceText).Evaluation;

        if (evaluation.HasUnsupportedFeatures)
        {
            TestContext.Current.TestOutputHelper?.Write(evaluation.Message);
            Assert.Skip("Unsupported features.");
        }
        
        if (evaluation.HasFailed)
        {
            TestContext.Current.TestOutputHelper?.Write(evaluation.Message);
            Assert.Fail("Test failed.");
        }
    }
}