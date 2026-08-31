using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Syntax;

public sealed class SyntaxTree
{
    /// <summary>
    /// The <see cref="SyntaxKind.File"/> node spanning the whole file.
    /// </summary>
    public FileSyntax FileSyntax { get; }
    
    public SourceFileView Source { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public bool HasError { get; }

    
    internal SyntaxTree(FileSyntax fileSyntax, SourceFileView source, ImmutableArray<Diagnostic> diagnostics, bool hasError)
    {
        Guard.MustBe(fileSyntax.Kind is SyntaxKind.File);

        FileSyntax = fileSyntax;
        Source = source;
        Diagnostics = diagnostics;
        HasError = hasError;
    }
}