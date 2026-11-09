using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public closed class BoundStmt(SyntaxNode? syntax = null)
    : BoundNode(syntax)
{
    public bool HasError
        => EnumerateAllChildren().OfType<BoundError>().Any();

    public abstract IEnumerable<BoundStmt> GetChildren();

    public IEnumerable<BoundStmt> EnumerateAllChildren()
        => [.. GetChildren(), .. GetChildren().SelectMany(c => c.EnumerateAllChildren())];

}