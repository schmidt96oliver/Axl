using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public abstract record Annotation(SourceLocation FullLocation, SourceLocation PrefixLocation)
{
    public int LineNumber => FullLocation.StartLinePosition.Line;
}