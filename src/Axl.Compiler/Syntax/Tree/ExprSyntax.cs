using System.Collections.Immutable;
using Dunet;

namespace Axl.Compiler.Syntax.Tree;


public abstract class StmtSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : SyntaxNode(kind, children);

public sealed class UsingDeclSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.UsingDecl, children)
{
    public QualifiedNameSyntax Name => NthChildOfType<QualifiedNameSyntax>(0);
}

public sealed class ExprStmtSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.ExprStmt, children)
{
    public ExprSyntax Expr => NthChildOfType<ExprSyntax>(0);
}

public sealed class VarDeclSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.VarDecl, children)
{
    public IdentifierToken Name => NthChildOfType<IdNameSyntax>(0).Token;

    public TypeNameSyntax? TypeAnnotation => NthNodeOfKindOrNull(SyntaxKind.TypeAnnotationClause, 0)?
        .NthChildOfType<TypeNameSyntax>(0);
    
    public ExprSyntax? Initializer => NthNodeOfKindOrNull(SyntaxKind.InitializerClause, 0)?
        .NthChildOfType<ExprSyntax>(0);
}

public abstract class MemberDeclSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : SyntaxNode(kind, children);

public sealed class FnDeclSyntax(ImmutableArray<SyntaxElement> children)
    : MemberDeclSyntax(SyntaxKind.FnDecl, children)
{
    public IEnumerable<Token> Modifiers
        => Children.OfType<Token>().Where(token => token.Kind.IsModifier);

    public StringExprSyntax? NativeName
        => NthNodeOfKindOrNull(SyntaxKind.NativeClause, 0)?
            .NthChildOfType<StringExprSyntax>(0);
    
    public IdentifierToken Name => NthChildOfType<IdNameSyntax>(0).Token;

    public IEnumerable<ParamSyntax> Parameters
        => NthNodeOfKind(SyntaxKind.ParamList, 0)
            .Children.OfType<ParamSyntax>();

    public TypeNameSyntax? ReturnTypeAnnotation
        => NthNodeOfKindOrNull(SyntaxKind.TypeAnnotationClause, 0)?
            .NthChildOfType<TypeNameSyntax>(0);
    
    /// <summary>
    /// Only <c>null</c>, if <see cref="NativeName"/> is not <c>null</c>.
    /// </summary>
    public BodySyntax? Body => NthChildOfTypeOrNull<BodySyntax>(0);
}

public sealed class ModuleDeclSyntax(ImmutableArray<SyntaxElement> children)
    : MemberDeclSyntax(SyntaxKind.ModuleDecl, children)
{
    public QualifiedNameSyntax Name => NthChildOfType<QualifiedNameSyntax>(0);

    public IEnumerable<MemberDeclSyntax> Members
        => Children.OfType<MemberDeclSyntax>();
}

public sealed class GlobalModuleDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.GlobalModuleDecl, children)
{
    public QualifiedNameSyntax Name => NthChildOfType<QualifiedNameSyntax>(0);
}

public sealed class ParamSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Param, children)
{
    public IdentifierToken Name => NthChildOfType<IdNameSyntax>(0).Token;
    
    public TypeNameSyntax? TypeAnnotation => NthNodeOfKindOrNull(SyntaxKind.TypeAnnotationClause, 0)?
        .NthChildOfType<TypeNameSyntax>(0);
}

/// <summary>
/// Derives from <see cref="ExprSyntax"/> because <see cref="IdNameSyntax"/> has two roles:
/// As expression and as type name. This allows easier access on the AST. 
/// </summary>
public abstract class TypeNameSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : ExprSyntax(kind, children);


public sealed class IdNameSyntax(ImmutableArray<SyntaxElement> children)
    : TypeNameSyntax(SyntaxKind.IdName, children)
{
    public IdentifierToken Token => NthToken(0) as IdentifierToken
                                    ?? throw new ArgumentException(
                                        $"Token on {nameof(IdNameSyntax)} was not {nameof(IdentifierToken)}",
                                        nameof(children));
}

public sealed class NativeTypeNameSyntax(ImmutableArray<SyntaxElement> children)
    : TypeNameSyntax(SyntaxKind.NativeTypeName, children)
{
    public Token Token => NthToken(0);
}

public sealed class QualifiedNameSyntax(ImmutableArray<SyntaxElement> children)
    : TypeNameSyntax(SyntaxKind.QualifiedName, children)
{
    public IEnumerable<IdentifierToken> Parts
        => Children.OfType<IdNameSyntax>().Select(idNameSyntax => idNameSyntax.Token);
}



public abstract class ExprSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : SyntaxNode(kind, children);

public sealed class BinaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BinaryExpr, children)
{
    public ExprSyntax Left => NthChildOfType<ExprSyntax>(0);

    public Token Operator => NthToken(0);

    public ExprSyntax Right => NthChildOfType<ExprSyntax>(1);
}

public sealed class UnaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.UnaryExpr, children)
{
    public Token Operator => NthToken(0);
    public ExprSyntax Operand => NthChildOfType<ExprSyntax>(0);
}

public sealed class GroupExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.GroupExpr, children)
{
    public ExprSyntax Inner => NthChildOfType<ExprSyntax>(0);
}
public sealed class BreakExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BreakExpr, children)
{
    public ExprSyntax? Expr => NthChildOfTypeOrNull<ExprSyntax>(0);
}
public sealed class ReturnExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ReturnExpr, children)
{
    public ExprSyntax? Expr => NthChildOfTypeOrNull<ExprSyntax>(0);
}
public sealed class AssignExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.AssignExpr, children)
{
    public ExprSyntax Target => NthChildOfType<ExprSyntax>(0);
    public ExprSyntax Value => NthChildOfType<ExprSyntax>(1);
}

public sealed class ContinueExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ContinueExpr, children);

public sealed class CallExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.CallExpr, children)
{
    public ExprSyntax Callee => NthChildOfType<ExprSyntax>(0);

    public IEnumerable<ExprSyntax> Arguments => NthNodeOfKind(SyntaxKind.ArgList, 0)
        .NodesOfKind(SyntaxKind.Arg)
        .Select(node => node.NthChildOfType<ExprSyntax>(0));
}


public sealed class GetMemberExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.GetMemberExpr, children)
{
    public ExprSyntax Left => NthChildOfType<ExprSyntax>(0);

    public IdNameSyntax Member => NthChildOfType<IdNameSyntax>(0);
}

public sealed class IfExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.IfExpr, children)
{
    public ExprSyntax Predicate => NthChildOfType<ExprSyntax>(0);

    public BodySyntax Body => NthChildOfType<BodySyntax>(0);

    public ExprSyntax? ElseBody => 
        NthNodeOfKindOrNull(SyntaxKind.ElseClause, 0)?
            .NthChildOfType<ExprSyntax>(0);
}
public sealed class LoopExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.LoopExpr, children)
{
    public BodySyntax Body => NthChildOfType<BodySyntax>(0);
}



/// <summary>
/// Derives from <see cref="ExprSyntax"/>, because <see cref="ArmSyntax"/> wants to
/// be named in expression positions, even though it is not technically an expression.
/// </summary>
public abstract class BodySyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : ExprSyntax(kind, children);

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

/// <summary>
/// Derives from <see cref="ExprSyntax"/> through <see cref="BodySyntax"/>, because it wants to
/// be named in expression positions.
/// It does evaluate to a value, so it does make sense.
/// </summary>

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

[Union]
public partial record StringPart
{
    public partial record Text(StringTextToken Token);

    public partial record Interpolation(ExprSyntax Expr);

    public static StringPart From(SyntaxNode node)
        => node.Kind switch
        {
            SyntaxKind.StringInterpolation => new Interpolation(node.NthChildOfType<ExprSyntax>(0)),
            SyntaxKind.StringText => new Text(node.NthToken(0) as StringTextToken
                                              ?? throw new ArgumentException(
                                                  $"Token on {nameof(SyntaxKind.StringText)} was not {nameof(StringTextToken)}",
                                                  nameof(node))),

            _ => throw new ArgumentException($"{nameof(node)} is not a string part.", nameof(node))
        };
}

public sealed class StringExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.StringExpr, children)
{
    public IEnumerable<StringPart> Parts
        => Children
            .OfType<SyntaxNode>()
            .Where(node => node.Kind is SyntaxKind.StringText or SyntaxKind.StringInterpolation)
            .Select(StringPart.From);
}

public sealed class ErrorExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ErrorExpr, children)
{
    public IEnumerable<SyntaxNode> RecoverableNodes
        => Children.OfType<SyntaxNode>().Where(child => child.Kind is not SyntaxKind.Garbage);
}