using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class TypeAnnotationClauseSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.TypeAnnotationClause, children)
{
    public TypeNameSyntax TypeName => Children.FirstOfType<TypeNameSyntax>();
}