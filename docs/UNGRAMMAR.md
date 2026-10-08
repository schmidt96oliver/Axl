
# Top-Level

File            = (Stmt | UsingDirective | NamespaceDecl | FnDecl | StructDecl)*

NamespaceDecl      = "namespace" TypeName ";"
UsingDirective  = "using" TypeName ";"

## Member Declarations
MemberDecl       = FnDecl | StructDecl
                
FunDecl          = "pub"? "static"? "var"? "fun" IdName ParamList TypeAnnotation? FunBody
FunBody          = "=>" Expr ";"
                | BlockExpr

ParamList       = "(" ")"
                | "(" Param ("," Param)* ")"
Param           = IdName TypeAnnotation?

StructDecl      = "struct" IdName (StructBody | ";")
StructBody      = "{" FieldDecl* "}"
FieldDecl       = "pub"? IdName TypeAnnotation ";"

## Statements
Stmt        = ExprStmt
            | VarDecl
            | WhileStmt

ExprStmt    = Expr ";"

VarDecl             = ("var" | "let") IdName TypeAnnotation? InitializerClause? ";"
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

BlockExpr   = "{" (Stmt | FnDecl)* "}"

IfExpr      = "if" (Expr) Expr (";"? "else" Expr)

AssignExpr  = Expr ("="|"+="|"-=") Expr

Group       = "(" Expr ")"

Literal     = "true" | "false"
            | NumberLiteral

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
TypeName        = IdName | (TypeName ("." IdName))
> Deliberately a subset of GetMember. And the parser produces a GetMemberExpr

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