using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public abstract class StmtSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : StmtOrMemberSyntax(kind, children);