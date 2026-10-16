using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed record Fragment(SourceLocation Location)
{
    public SourceText SourceText
    {
        get
        {
            field ??= SourceText.From(Location.Text.ToString());
            return field;
        }
    }
}