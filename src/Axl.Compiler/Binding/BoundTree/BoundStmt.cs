using System.Collections.Immutable;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public closed class BoundStmt(SyntaxNode syntax)
{
    public SyntaxNode Syntax { get; } = syntax;

    public ImmutableArray<BoundStmt> Children
    {
        get
        {
            if (field.IsDefault)
                field = GetChildren();
            return field;
        }
    }

    protected abstract ImmutableArray<BoundStmt> GetChildren();
}