using System.Collections.Immutable;
using Dunet;

namespace Axl.Compiler.Syntax.Tree;

[Union]
public partial record RootItem
{
    public partial record Stmt(StmtSyntax Syntax);
    public partial record MemberDecl(MemberDeclSyntax Syntax);
    public partial record GlobalModuleDecl(GlobalModuleDeclSyntax Syntax);

    public static RootItem From(SyntaxNode node)
        => node switch
        {
            StmtSyntax stmt => new Stmt(stmt),
            MemberDeclSyntax memberDecl => new MemberDecl(memberDecl),
            GlobalModuleDeclSyntax globalModuleDecl => new GlobalModuleDecl(globalModuleDecl),
            _ => throw new ArgumentException($"{nameof(node)} is not a root item.", nameof(node))
        };
}

public sealed class TreeRootSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.TreeRoot, children)
{
    public IEnumerable<RootItem> Items
        => Children.OfType<SyntaxNode>()
            .Where(node => node is StmtSyntax or MemberDeclSyntax or GlobalModuleDeclSyntax)
            .Select(RootItem.From);
}