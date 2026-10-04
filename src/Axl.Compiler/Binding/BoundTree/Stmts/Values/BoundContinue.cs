using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundContinue(SyntaxNode? syntax = null)
    : BoundValue(NeverTypeSymbol.Instance, syntax);