using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public sealed class SyntaxTree
{
    /// <summary>
    /// The <see cref="SyntaxKind.TreeRoot"/> node spanning the whole file.
    /// </summary>
    public SyntaxNode Root { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public bool HasError { get; }

    internal SyntaxTree(SyntaxNode root, ImmutableArray<Diagnostic> diagnostics, bool hasError)
    {
        Guard.MustBe(root.Kind is SyntaxKind.TreeRoot);

        Root = root;
        Diagnostics = diagnostics;
        HasError = hasError;
    }
}
