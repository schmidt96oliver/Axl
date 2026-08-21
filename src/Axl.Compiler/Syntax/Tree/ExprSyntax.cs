using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public abstract class ExprSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children) : SyntaxNode(kind, children)
{
    
}

public sealed class BinaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BinaryExpr, children)
{
    public ExprSyntax Left => NthSlot<ExprSyntax>(0);

    public Token Operator => NthSlot<Token>(1);
    
    public ExprSyntax Right => NthSlot<ExprSyntax>(2);
}

public sealed class IfExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.IfExpr, children)
{
    public ExprSyntax Predicate => NthSlot<ExprSyntax>(1);

    public BodySyntax Body => NthSlot<BodySyntax>(2);
    
    public ExprSyntax? ElseBody => NthSlotOrNull<ExprSyntax>(4);
}

public abstract class BodySyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : ExprSyntax(kind, children)
{
    
}

public sealed class BlockSyntax(ImmutableArray<SyntaxElement> children)
    : BodySyntax(SyntaxKind.BlockExpr, children)
{
    public IEnumerable<SyntaxNode> Items
    {
        get
        {
            foreach (var child in SyntaxChildren().Skip(1))
            {
                if (child is Token { Kind: TokenKind.CloseBrace } or ArmSyntax)
                    yield break;
                if (child is SyntaxNode node)
                    yield return node;
                // if (child is FnDeclSyntax)
                //     yield return (FnDeclSyntax)child;
            }
        }
    }

    public ArmSyntax? Arm
        => SyntaxChildren().TakeLast(2).First() as ArmSyntax;
}

// Arm is expression now as well and makes sense. It does evaluate
// to a value, and it's better so that ElseBody above can use ExprSyntax.
public sealed class ArmSyntax(ImmutableArray<SyntaxElement> children)
    : BodySyntax(SyntaxKind.Arm, children)
{
    public ExprSyntax Expr => NthSlot<ExprSyntax>(1);
}

public sealed class TrueLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.TrueLiteral, children);
public sealed class FalseLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.FalseLiteral, children);

public sealed class NumberLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.NumberLiteral, children)
{
    public Token ValueToken => NthSlot<Token>(0);
}

public sealed class ErrorExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ErrorExpr, children);