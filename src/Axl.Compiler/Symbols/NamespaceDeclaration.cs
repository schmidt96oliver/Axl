using System.Collections.Immutable;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed record NamespaceDeclaration(
    string Name,
    List<NamespaceDeclaration> Children,
    List<SyntaxTree> Syntaxes)
{
    public ImmutableArray<NamespaceDeclSyntax> DeclarationSyntaxes =>
    [
        .. Syntaxes.Select(tree => tree.FileSyntax.Children.FirstOfType<NamespaceDeclSyntax>())
    ];
}