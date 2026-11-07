using Axl.Compiler;
using Axl.Compiler.Text;

namespace Axl.Lsp;

public sealed record ProjectLocation(IProject Project, SourceLocation Location)
{
    public Compilation Compilation => Project.Compilation;
}