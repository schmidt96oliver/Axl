using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public closed record Annotation(SourceLocation FullLocation, SourceLocation PrefixLocation)
{
    public int LineNumber => FullLocation.StartLine;
}