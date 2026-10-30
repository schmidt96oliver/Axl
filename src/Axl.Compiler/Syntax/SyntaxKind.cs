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
    NamespaceDecl,
    FunDecl,
    FieldDecl,
    StructDecl,
    
    // Literals
    TrueLiteral,
    FalseLiteral,
    NumberLiteral,
    
    // Strings
    StringExpr,
    StringInterpolation,
    StringText,
    
    // Expressions
    IdName,
    GroupExpr,
    BinaryExpr,
    UnaryExpr,
    CallExpr,
    MemberAccessExpr,
    SubscriptExpr,
    
    BreakExpr,
    ContinueExpr,
    ReturnExpr,
    
    BlockExpr,
    IfExpr,
    MutExpr,
    
    // Clauses
    Arg,
    ArgList,
    Param,
    ParamList,
    TypeParam,
    TypeParamList,
    
    NativeClause,
    ElseClause,
    TypeAnnotationClause,
    ReturnTypeAnnotation,
    InitializerClause,
    FunBody,
    ConditionClause,
    StructBody,
    Self,
    FunName,
    NamespaceName,
    Annotation,
    SubscriptArgList,
}