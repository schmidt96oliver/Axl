using Axl.Compiler;
using Axl.Compiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Binding;

public class BrokenInput
{
    private string Diagnostics(string input)
    {
        var source = SourceText.From(input);
        var compilation = Compilation.From(source).AddBaseLibrary();
        return new Dump(source)
            .Add(compilation.Diagnostics)
            .ToString();
    }

    [Fact]
    public void ScriptLocalFunWithoutName()
        => InlineSnapshot.Validate(Diagnostics("fun"), "ERROR MissingToken@[3, 3): Expected a fun name.");
}