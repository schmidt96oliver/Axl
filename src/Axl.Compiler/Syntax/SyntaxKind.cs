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
    QualifiedName, //x
    NativeTypeName, //x
    
    // Operand Expressions
    TrueLiteral, //x
    FalseLiteral, //x
    NumberLiteral, //x
    
    IdName, //x
    
    GroupExpr, //x
    BinaryExpr, //x
    UnaryExpr, //x
    CallExpr, //x
    GetMemberExpr, //x
    
    // Tail Expressions
    BreakExpr, //x
    ContinueExpr, //x
    ReturnExpr, //x
    AssignExpr, //x
    
    // Bodied Expressions
    BlockExpr,
    IfExpr, //x
    LoopExpr, //x
    
    // Strings
    StringExpr, //x
    StringInterpolation, //
    StringText, //
    
    // Arm
    Arm, //x
    
    // Clauses
    ArgList, //
    Arg, //
    ParamList,
    Param,
    NativeClause,
    ElseClause //
    ,
    TypeAnnotationClause,
    InitializerClause
}