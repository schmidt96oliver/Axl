using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class SelfSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.Self, children);