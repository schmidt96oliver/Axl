using System.Collections.Immutable;
using Dunet;

namespace Axl.Compiler.Syntax.Tree;

[Union]
public partial record BlockItem
{
    public partial record Stmt(StmtSyntax Syntax);
    public partial record FnDecl(FnDeclSyntax Syntax);

    public static BlockItem From(SyntaxNode node)
        => node switch
        {
            StmtSyntax stmt => new Stmt(stmt),
            FnDeclSyntax fnDecl => new FnDecl(fnDecl),
            _ => throw new ArgumentException($"{nameof(node)} is not a valid block item.", nameof(node))
        };
}

public sealed class BlockSyntax(ImmutableArray<SyntaxElement> children)
    : BodySyntax(SyntaxKind.BlockExpr, children)
{
    public IEnumerable<BlockItem> Items
        => Children.OfType<SyntaxNode>()
            .Where(node => node is StmtSyntax or FnDeclSyntax)
            .Select(BlockItem.From);
    
    public ArmSyntax? Arm => NthChildOfTypeOrNull<ArmSyntax>(0);
}