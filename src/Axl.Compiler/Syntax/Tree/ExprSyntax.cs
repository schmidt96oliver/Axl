using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Syntax.Tree;

public abstract class AstBase(SyntaxKind kind, ImmutableArray<SyntaxElement> children) 
    : SyntaxNode(kind, children)
{
    protected IEnumerable<SyntaxElement> SyntaxChildren()
        => Children
            .Where(child => child is Token { Kind.IsTrivia: false } 
                or SyntaxNode { Kind: not SyntaxKind.Garbage });
    
    protected T NthChild<T>(int n)
        where T : SyntaxElement
        => MaybeNthChild<T>(n) ?? throw new UnreachableException($"Slot {n} is not present.");

    protected T? MaybeNthChild<T>(int n)
        where T : SyntaxElement
    {
        var child = SyntaxChildren()
            .Skip(n)
            .FirstOrDefault();
        if (child is null)
            return null;
        
        Guard.MustBe(child is T, $"Slot {n} is {child.GetType().Name}, but expected {typeof(T).Name}");
        return (T)child;
    }
}

public abstract class ExprSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children) : AstBase(kind, children)
{
    
}

public sealed class BinaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BinaryExpr, children)
{
    public ExprSyntax Left => NthChild<ExprSyntax>(0);

    public Token Operator => NthChild<Token>(1);
    
    public ExprSyntax Right => NthChild<ExprSyntax>(2);
}

public sealed class IfExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.IfExpr, children)
{
    public ExprSyntax Predicate => NthChild<ExprSyntax>(1);

    public BodySyntax Body => NthChild<BodySyntax>(2);
    
    public ExprSyntax? ElseBody => MaybeNthChild<ExprSyntax>(4);
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
    public ExprSyntax Expr => NthChild<ExprSyntax>(1);
}

public sealed class TrueLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.TrueLiteral, children);
public sealed class FalseLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.FalseLiteral, children);

public sealed class NumberLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.NumberLiteral, children)
{
    public Token ValueToken => NthChild<Token>(0);
}

public sealed class ErrorExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ErrorExpr, children);