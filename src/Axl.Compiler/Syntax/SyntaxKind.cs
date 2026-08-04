namespace Axl.Compiler.Syntax;

public enum SyntaxKind
{
    Error,
    
    // Top-Level
    TreeRoot,
    ExprStmt,
    
    // Declarations
    UsingDecl,
    GlobalModuleDecl,
    ModuleDecl,
    VarDecl,
    
    // Type Expressions
    TypeExpr,
    NativeTypeName,
    
    // Operand Expressions
    TrueLiteral,
    FalseLiteral,
    NumberLiteral,
    
    Identifier,
    StringExpr,
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
    Param,
    ArgList,
    ParamList,
    StringInterpolation,
    TypeAnnotation,
    InitializerClause,
    ModifierList,
    NativeClause,
    ModuleName,
}