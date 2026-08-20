using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public sealed class SyntaxTree
{
    /// <summary>
    /// The <see cref="SyntaxKind.TreeRoot"/> node spanning the whole file.
    /// </summary>
    public SyntaxNode Root { get; }
    
    public SourceFileView Source { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public bool HasError { get; }

    internal SyntaxTree(SyntaxNode root, SourceFileView source, ImmutableArray<Diagnostic> diagnostics, bool hasError)
    {
        Guard.MustBe(root.Kind is SyntaxKind.TreeRoot);

        Root = root;
        Source = source;
        Diagnostics = diagnostics;
        HasError = hasError;
    }
}