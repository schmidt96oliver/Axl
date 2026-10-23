using System.Collections.Immutable;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed record NamespaceDeclaration(
    string Name,
    ImmutableArray<NamespaceDeclaration> Children,
    ImmutableArray<SyntaxTree> SyntaxTrees)
{
    public ImmutableArray<NamespaceDeclSyntax> NamespaceDeclSyntaxes =>
    [
        .. SyntaxTrees.Select(tree => tree.FileSyntax.Children.FirstOfType<NamespaceDeclSyntax>())
    ];
}

public sealed class NamespaceDeclarationBuilder(string name)
{
    public string Name { get; } = name;
    private readonly List<NamespaceDeclarationBuilder> _children = [];
    private readonly List<SyntaxTree> _syntaxTrees = [];

    public NamespaceDeclarationBuilder AddChild(string name)
    {
        var builder = new NamespaceDeclarationBuilder(name);
        _children.Add(builder);
        return builder;
    }

    public NamespaceDeclarationBuilder? TryGetChild(string name)
        => _children.FirstOrDefault(c => c.Name == name);
    
    public void AddSyntaxTree(SyntaxTree tree)
        => _syntaxTrees.Add(tree);
    
    

    public NamespaceDeclaration ToNamespaceDeclaration()
    {
        var trees = _syntaxTrees.ToImmutableArray();
        var children = _children.Select(c => c.ToNamespaceDeclaration()).ToImmutableArray();
        return new NamespaceDeclaration(Name, children, trees);
    }
}