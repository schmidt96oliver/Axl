using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class NativeTypeNameSyntax(ImmutableArray<SyntaxElement> children)
    : TypeNameSyntax(SyntaxKind.NativeTypeName, children)
{
    public Token Token => Children.FirstNonTriviaToken();
}