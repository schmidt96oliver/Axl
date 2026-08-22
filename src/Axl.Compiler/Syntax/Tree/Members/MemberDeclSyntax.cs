using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public abstract class MemberDeclSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : SyntaxNode(kind, children);