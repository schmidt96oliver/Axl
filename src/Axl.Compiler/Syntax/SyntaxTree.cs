using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public sealed class SyntaxTree : SyntaxNode
{
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    
    public bool HasError { get; }

    internal SyntaxTree(SourceSpan emptySpan, ImmutableArray<Diagnostic> diagnostics, bool hasError)
        : base(SyntaxKind.TreeRoot, emptySpan)
    {
        Diagnostics = diagnostics;
        HasError = hasError;
    }
    
    internal SyntaxTree(ImmutableArray<SyntaxElement> children, ImmutableArray<Diagnostic> diagnostics, bool hasError)
        : base(SyntaxKind.TreeRoot, children)
    {
        Diagnostics = diagnostics;
        HasError = hasError;
    }
}