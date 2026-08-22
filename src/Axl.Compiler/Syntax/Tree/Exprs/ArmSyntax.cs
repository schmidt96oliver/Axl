using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

/// <summary>
/// Derives from <see cref="ExprSyntax"/> through <see cref="BodySyntax"/>, because it wants to
/// be named in expression positions.
/// It does evaluate to a value, so it does make sense.
/// </summary>
public sealed class ArmSyntax(ImmutableArray<SyntaxElement> children)
    : BodySyntax(SyntaxKind.Arm, children)
{
    public ExprSyntax Expr => Children.FirstOfType<ExprSyntax>();
}