
# Top-Level

File            = (Stmt | UsingDirective | ModuleDecl | Member)*

ModuleDecl      = "module" Path ";"
UsingDirective  = "using" Path ";"

## Member Declarations
MemberDecl       = FnDecl | NativeFnDecl
                
FnDecl           = "fn" IdName ParamList ReturnTypeAnnotation? FnBody
> Identifier "never" is promoted to SyntaxKind.NativeTypeName with TokenKind.NeverKw

FnBody          = "=>" Expr ";"
                | BlockExpr

ParamList       = "(" ")"
                | "(" Param ("," Param)* ")"
Param           = IdName TypeAnnotation?
ReturnTypeAnnotation    = "->" (TypeName | "never")

NativeFnDecl    = NativeClause "fn" IdName ParamList ReturnTypeAnnotation? ";"
NativeClause    = "native" "(" StringExpr ")"
> Binder rejects interpolations inside StringExpr

## Statements
Stmt        = ExprStmt
            | VarDecl
            | WhileStmt

ExprStmt    = Expr ";"

VarDecl             = "var" IdName TypeAnnotation? InitializerClause? ";"
InitializerClause   = "=" Expr

WhileStmt   = "while" "(" Expr ")" Block



# Expressions
Expr        = BlockExpr
            | IfExpr
            | Break | Continue | Return
            | Literal | IdName | StringExpr
            | Group | Binary | Unary
            | Call
            | GetMember
            | AssignExpr

BlockExpr   = "{" (Stmt | Member)* "}"

IfExpr      = "if" (Expr) Expr ("else" Expr)
> ERROR PRODUCTION: "=" accepted inside (Expr)

AssignExpr  = Expr ("="|"+="|"-=") Expr

Group       = "(" Expr ")"

Literal     = "true" | "false"
            | NumberLiteral
            | NativeTypeName

Binary      = Expr ("+"|"-"|"*"|"/"|"<"|"<="|">"|">="  |"=="|"!=" |"and"|"or") Expr
Unary       = ("-" | "not") Expr

GetMember   = Expr "." IdName
Call        = Expr ArgList

ArgList     = "(" ")"
            | "(" Arg ("," Arg)* ")"
Arg         = Expr

StringExpr            = StringStart (StringText | StringInterpolation)* StringEnd
StringInterpolation   = "{" Expr? "}"
> Can be empty to allow multi-line breaks.

Break       = "break"
Continue    = "continue"
Return      = "return" Expr?

## Type Names
TypeName        = NativeTypeName
                | Path
> TypeName is an Expr in AST. In the grammar we need to distinguish:
> Path is a TypeName construct, whereas GetMemberExpr would parse the
> same syntax but in expression position. In AST, they both collapse
> into Expr to be better nameable.

NativeTypeName  = "i32" | "i64" | "f32" | "f64" | "string" | "bool" | "unit"
> SyntaxKind.NativeTypeName can also hold TokenKind.NeverKw. NeverKw is promoted
> from TokenKind.Identifier if FnDecl return type and only there.

Path   = IdName ("." IdName)*

TypeAnnotation  = ":" TypeName

# Precedence Table
. (                 (left-assoc)
-                   (prefix)
* /                 (left-assoc)
+ -                 (left-assoc)
< <= > >= == !=     (ambig assoc)
not                 (prefix)
and                 (left-assoc, ambig with or)
or                  (left-assoc, ambig with and)
=                   (right-assoc)