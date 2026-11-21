namespace AxlCompiler.Syntax;

public enum Precedence
{
    // Ordered from lowest to highest, so the int value can be used 
    // for comparison.
    Assign,

    LogicOr,
    LogicAnd,

    Comparison,

    
    Sum,
    Factor,
    
    Range,

    LogicNot,
    Negate,

    ArgList,

    Dot,
}