using System.Collections.Immutable;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public abstract class HirStmt(SyntaxNode syntax)
{
    public SyntaxNode Syntax { get; } = syntax;

    public ImmutableArray<HirStmt> Children
    {
        get
        {
            if (field.IsDefault)
                field = GetChildren();
            return field;
        }
    }

    protected abstract ImmutableArray<HirStmt> GetChildren();
}