using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundError(SyntaxNode? syntax = null) 
    : BoundValue(ErrorTypeSymbol.Instance, syntax);