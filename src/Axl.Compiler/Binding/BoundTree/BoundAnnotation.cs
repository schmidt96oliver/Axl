using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundAnnotation(AnnotationKind kind, SyntaxNode syntax)
{
    public AnnotationKind Kind { get; } = kind;
    public SyntaxNode Syntax { get; } = syntax;
}