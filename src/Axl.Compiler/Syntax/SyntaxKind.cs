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
    
    // Directives, Statements
    ExprStmt,
    UsingDirective,
    VarDecl,
    
    // Declarations
    ModuleDecl,
    FileScopedModuleDecl,
    FnDecl,
    NativeFnDecl,
    
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
    
    // Operand Expressions
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
    
    // Arm
    Arm,
    
    // Clauses
    Arg,
    ArgList,
    Param,
    ParamList,
    NativeClause,
    ElseClause,
    TypeAnnotationClause,
    InitializerClause,
}