using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class BlockExprSyntax(ImmutableArray<SyntaxElement> children)
    : BodySyntax(SyntaxKind.BlockExpr, children)
{
    public IEnumerable<UsingDirectiveSyntax> Usings 
        => Children.OfType<UsingDirectiveSyntax>();
    
    public IEnumerable<MemberSyntax> Members
        => Children.OfType<MemberSyntax>();
    
    public IEnumerable<StmtSyntax> Stmts 
        => Children.OfType<StmtSyntax>();
    
    public ArmSyntax? Arm 
        => Children.FirstOfTypeOrNull<ArmSyntax>();
}