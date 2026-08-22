using System.Collections.Immutable;
using Dunet;

namespace Axl.Compiler.Syntax.Tree;

public sealed class BlockExprSyntax(ImmutableArray<SyntaxElement> children)
    : BodySyntax(SyntaxKind.BlockExpr, children)
{
    public IEnumerable<StmtOrMemberSyntax> Items
        => Children.OfType<StmtOrMemberSyntax>();

    public ArmSyntax? Arm => Children
        .FirstOfTypeOrNull<ArmSyntax>();
}