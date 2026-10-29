using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Mir;

public closed record Operand(TypeSymbol Type, SyntaxNode? Syntax)
{
    public sealed record Copy(Place Place, SyntaxNode? Syntax = null) 
        : Operand(Place.Type, Syntax);

    public sealed record Ref(Place Place, TypeSymbol Type, SyntaxNode? Syntax = null) : 
        Operand(Type, Syntax);
    
    public sealed record Const(ConstValue Value, TypeSymbol Type, SyntaxNode? Syntax = null) 
        : Operand(Type, Syntax);

    public sealed record Empty(TypeSymbol Type, SyntaxNode? Syntax = null) 
        : Operand(Type, Syntax);

    public sealed record Unreachable(TypeSymbol Type)
        : Operand(Type, null);
}