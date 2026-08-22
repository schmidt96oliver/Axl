using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

/// <summary>
/// Derives from <see cref="ExprSyntax"/> because <see cref="IdNameSyntax"/> has two roles:
/// As expression and as type name. This allows easier access on the AST. 
/// </summary>
public abstract class TypeNameSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : ExprSyntax(kind, children);