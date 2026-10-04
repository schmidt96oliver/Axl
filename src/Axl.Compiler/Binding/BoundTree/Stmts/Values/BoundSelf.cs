using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundSelf(TypeSymbol type, SyntaxNode? syntax = null) : BoundValue(type, syntax);