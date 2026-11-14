using AxlCompiler;
using AxlCompiler.Syntax;
using AxlCompiler.Text;

namespace AxlLsp.Projects;

public sealed record ProjectLocation(IProject Project, SourceLocation Location)
{
    public Compilation Compilation => Project.Compilation;
    
    public SyntaxTree SyntaxTree => Project.Compilation.SyntaxTreeFor(Location.SourceText);
}