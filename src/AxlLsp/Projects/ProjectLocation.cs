using AxlCompiler;
using AxlCompiler.Text;

namespace AxlLsp.Projects;

public sealed record ProjectLocation(IProject Project, SourceLocation Location)
{
    public Compilation Compilation => Project.Compilation;
}