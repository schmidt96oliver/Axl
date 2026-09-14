using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class BlockExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BlockExpr, children)
{
    public IEnumerable<MemberSyntax> Members
        => Children.OfType<MemberSyntax>();
    
    public IEnumerable<StmtSyntax> Stmts 
        => Children.OfType<StmtSyntax>();
}