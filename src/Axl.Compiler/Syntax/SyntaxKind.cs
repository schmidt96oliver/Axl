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
    File,
    UsingDirective,
    
    // Statements
    ExprStmt,
    VarDecl,
    WhileStmt,
    
    // Declarations
    ModuleDecl,
    FunDecl,
    
    // Type Names
    Path,
    IdName,
    NativeTypeName,
    
    // Literals
    TrueLiteral,
    FalseLiteral,
    NumberLiteral,
    
    // Strings
    StringExpr,
    StringInterpolation,
    StringText,
    
    // Expressions
    GroupExpr,
    BinaryExpr,
    UnaryExpr,
    CallExpr,
    GetMemberExpr,
    
    BreakExpr,
    ContinueExpr,
    ReturnExpr,
    
    BlockExpr,
    IfExpr,
    
    // Clauses
    Arg,
    ArgList,
    Param,
    ParamList,
    NativeClause,
    ElseClause,
    TypeAnnotationClause,
    InitializerClause,
    FunBody,
    ConditionClause
}