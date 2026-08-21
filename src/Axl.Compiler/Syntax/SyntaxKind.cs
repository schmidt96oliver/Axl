namespace Axl.Compiler.Syntax;

public enum SyntaxKind
{
    /// <summary>
    /// Flat tokens the parser could not understand. They have
    /// no relevance for consumers.
    /// </summary>
    Garbage,
    
    /// <summary>
    /// An expr the parser could understand, but only
    /// ambiguously. Only used by invalidly chained operators
    /// now. Can be consumed with caution.
    /// </summary>
    ErrorExpr,
    
    // Top-Level
    TreeRoot,
    ExprStmt,
    
    // Declarations
    UsingDecl,
    GlobalModuleDecl,
    ModuleDecl,
    VarDecl,
    FnDecl,
    
    // Type Expressions
    QualifiedName,
    NativeTypeName,
    
    // Operand Expressions
    TrueLiteral,
    FalseLiteral,
    NumberLiteral,
    
    IdName,
    
    GroupExpr,
    BinaryExpr,
    UnaryExpr,
    CallExpr,
    GetMemberExpr,
    
    // Tail Expressions
    BreakExpr,
    ContinueExpr,
    ReturnExpr,
    AssignExpr,
    
    // Bodied Expressions
    BlockExpr,
    IfExpr,
    LoopExpr,
    
    // Strings
    StringExpr,
    StringInterpolation,
    StringText,
    
    // Arm
    Arm,
    
    // Clauses
    ArgList,
    Arg,
    ParamList,
    Param,
    NativeClause,
}