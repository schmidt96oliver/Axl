using System.Collections.Immutable;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFunGroupRef(ImmutableArray<BoundFunRef> boundFunRefs, SyntaxNode syntax)
    : BoundNode(syntax)
{
    public ImmutableArray<BoundFunRef> BoundFunRefs { get; } = boundFunRefs;
    
    public string Name => BoundFunRefs.FirstOrDefault()?.Fun.Name ?? "???";
}