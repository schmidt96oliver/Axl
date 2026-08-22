using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

/// <summary>
/// Derives from <see cref="ExprSyntax"/>, because <see cref="ArmSyntax"/> wants to
/// be named in expression positions, even though it is not technically an expression.
/// </summary>
public abstract class BodySyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : ExprSyntax(kind, children);