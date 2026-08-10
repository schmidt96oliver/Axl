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
    QualifiedName,
    NativeTypeName,
    
    // Operand Expressions
    TrueLiteral,
    FalseLiteral,
    NumberLiteral,
    
    Identifier,
    
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
    ParamList,
    TypeAnnotation,
    InitializerClause,
    ModifierList,
    NativeClause,
    ModuleName,
    
}