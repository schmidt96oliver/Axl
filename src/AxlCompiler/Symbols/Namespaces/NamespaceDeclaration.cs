using System.Collections.Immutable;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

/// <summary>
/// For a single file that declares <c>namespace A.B.C;</c>,
/// the declarations for A and B will both have the file inside
/// their <see cref="SyntaxTrees"/>, but only C will have a
/// <see cref="MemberRoots"/>. 
/// </summary>
/// <param name="Name">The plain name of this namespace.</param>
/// <param name="Children">All child namespace declarations.</param>
/// <param name="MemberRoots">The syntax nodes that contribute members to this namespace.</param>
/// <param name="SyntaxTrees">All syntax trees that declare this namespace.</param>
/// <param name="NameTokens">The tokens this namespaces name is introduced at.</param>
public sealed record NamespaceDeclaration(
    string Name,
    ImmutableArray<NamespaceDeclaration> Children,
    ImmutableArray<FileSyntax> MemberRoots,
    ImmutableArray<SyntaxTree> SyntaxTrees,
    ImmutableArray<IdentifierToken> NameTokens)
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
    private readonly List<IdentifierToken> _nameTokens = [];
    private readonly List<FileSyntax> _memberRoots = [];

    public NamespaceDeclarationBuilder AddChild(string name)
    {
        var builder = new NamespaceDeclarationBuilder(name);
        _children.Add(builder);
        return builder;
    }

    public NamespaceDeclarationBuilder? TryGetChild(string name)
        => _children.FirstOrDefault(c => c.Name == name);
    
    public void AddSyntaxRef(SyntaxTree tree, IdentifierToken nameToken)
    {
        Guard.MustBe(nameToken.Tree == tree);
        Guard.MustBe(nameToken.Text.SequenceEqual(Name));
        
        _syntaxTrees.Add(tree);
        _nameTokens.Add(nameToken);
    }

    public void AddMemberRoot(FileSyntax syntax)
    {
        _memberRoots.Add(syntax);
    }


    public NamespaceDeclaration ToNamespaceDeclaration()
    {
        var children = _children.Select(c => c.ToNamespaceDeclaration()).ToImmutableArray();
        return new NamespaceDeclaration(Name, children, [.. _memberRoots], [.. _syntaxTrees], [.. _nameTokens]);
    }
}