using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed record Fragment(SourceLocation LocationInTestFile)
{
    public SourceText SourceText { get; } = SourceText.From(LocationInTestFile.Text.ToString());

    public SourceLocation LocalLocation => SourceText.GetLocation(SourceText.Range);
}