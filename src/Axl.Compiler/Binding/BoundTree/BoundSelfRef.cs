using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundSelfRef(TypeSymbol type, SyntaxNode? syntax = null) : BoundExpr(type, syntax);