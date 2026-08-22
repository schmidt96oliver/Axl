using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public abstract class ExprSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children) : SyntaxNode(kind, children)
{
    
}

public sealed class BinaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BinaryExpr, children)
{
    public ExprSyntax Left => NthChildOfType<ExprSyntax>(0);

    public Token Operator => NthToken(0);

    public ExprSyntax Right => NthChildOfType<ExprSyntax>(1);
}

public sealed class IfExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.IfExpr, children)
{
    public ExprSyntax Predicate => NthChildOfType<ExprSyntax>(0);

    public BodySyntax Body => NthChildOfType<BodySyntax>(0);
    
    public ExprSyntax? ElseBody => NthChildOfType<ExprSyntax>(1);
}

public abstract class BodySyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : ExprSyntax(kind, children)
{
    
}

public sealed class BlockSyntax(ImmutableArray<SyntaxElement> children)
    : BodySyntax(SyntaxKind.BlockExpr, children)
{
    //TODO: Add Items as Stmt | FnDecl
    
    public ArmSyntax? Arm => NthChildOfTypeOrNull<ArmSyntax>(0);
}

// Arm is expression now as well and makes sense. It does evaluate
// to a value, and it's better so that ElseBody above can use ExprSyntax.
public sealed class ArmSyntax(ImmutableArray<SyntaxElement> children)
    : BodySyntax(SyntaxKind.Arm, children)
{
    public ExprSyntax Expr => NthChildOfType<ExprSyntax>(0);
}

public sealed class TrueLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.TrueLiteral, children);

public sealed class FalseLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.FalseLiteral, children);

public sealed class NumberLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.NumberLiteral, children)
{
    public Token Token => NthToken(0);
}

public sealed class ErrorExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ErrorExpr, children)
{
    public IEnumerable<SyntaxNode> RecoverableNodes
        => Children.OfType<SyntaxNode>().Where(child => child.Kind is not SyntaxKind.Garbage);
}