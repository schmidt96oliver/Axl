using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Lowering;

public closed record Operand(TypeSymbol Type, SyntaxNode? Syntax)
{
    public sealed record Copy(Place Place, SyntaxNode? Syntax = null) : Operand(Place.Type, Syntax);
    
    public sealed record Const(ConstValue Value, TypeSymbol Type, SyntaxNode? Syntax = null) : Operand(Type, Syntax);

    public sealed record EmptyStructConst(TypeSymbol Type, SyntaxNode? Syntax = null) : Operand(Type, Syntax);

    public sealed record Unreachable() : Operand(NeverTypeSymbol.Instance, null)
    {
        public static readonly Unreachable Instance = new ();
    }
}