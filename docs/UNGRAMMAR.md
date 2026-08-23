Ungrammar names follow SyntaxKind names where they are product ungrammars
and Parse* names where they are sum ungrammars.

# Top-Level
File            = (Stmt | UsingDirective | Member)*

> The distinction between script and module files is not made in the parser
> or AST, but by declaration binding later in the pipeline.

ModuleFile      = (UsingDirective | ModuleDecl | GlobalModuleDecl)*
ScriptFile      = (Stmt | UsingDirective | Member)*


## Directives
> Directive: Tells the compiler how to process code.

UsingDirective  = "using" Path ";"

## Member Declarations
> Declaration: Introduces a name.

Member           = FnDecl 
                 | ModuleDecl 
                 | GlobalModuleDecl
                
Modifier         = "public" | "private"

ModuleDecl       = Modifier* "module" Path "{" (UsingDirective | Member)* "}"
GlobalModuleDecl = Modifier* "module" Path ";"


FnDecl           = NativeClause? "fn" IdName ParamList ("->" (TypeName | "never"))? Body? ";"§
> Identifier "never" is promoted to SyntaxKind.NativeTypeName with TokenKind.NeverKw
> Binder requires Body on non-native functions

NativeClause    = "native" "(" StringExpr ")"
> Binder rejects interpolations inside StringExpr

ParamList       = "(" ")"
                | "(" Param ("," Param)* ")"

Param           = IdName TypeAnnotation?       

## Statements
> Semicolon rule (";"§): ";" is omissible, iff the last token is "}"

Stmt        = ExprStmt
            | VarDecl

ExprStmt    = BodiedExpr ";"§             
            | (OperandExpr | TailExpr) ";"


VarDecl             = "var" IdName TypeAnnotation? InitializerClause? ";"
InitializerClause   = "=" Expr

# Expressions
There needs to be a division between 3 different types of expressions:
    1. BodiedExpr  - They _own_ a body
        * In statement position (as ExprStmt), ";" can be omitted, if it ends in `}`.
    2. TailExpr    - They might _end_ in a body, they don't own
        * ";" always required.
        * They might leak bodies into Exprs where bodies are not allowed, so they need to be their own category.
    3. OperandExpr - Contains bodies only in _clearly delimited_ cases
This is to avoid certain syntax footgun and ambiguities. Also the semicolon
rule is stated more clearly in this framing.

Expr        = BodiedExpr | OperandExpr | TailExpr

## Body, BodiedExpr, Arm
Body        = BlockExpr
            | Arm
Arm         = "=>" Expr
            | "=" Expr      > ERROR PRODUCTION
> In AST, Body and Arm are Expr as well, to allow it being named in expression positions.
> However, the grammar does not allow Arm in expression position.

BodiedExpr  = Block | If | Loop

BlockExpr   = "{" (Stmt | UsingDirective | Member)* Arm? "}"

If          = "if" OperandExpr Body ElseClause?     
> Condition is OperandExpr to disallow any unparenthesized body inside it.
> ERROR PRODUCTION: "=" accepted after OperandExpr

ElseClause  = "else" (Body | If)
Loop        = "loop" Body

## Operand Expressions

OperandExpr = Literal | IdName | StringExpr
            | Group
            | Binary
            | Unary
            | Call
            | GetMember

Group       = "(" Expr ")"

Literal     = "true" | "false"
            | NumberLiteral
            | NativeTypeName

Binary      = OperandExpr ("+"|"-"|"*"|"/"|"<"|"<="|">"|">="  |"=="|"!=" |"and"|"or") OperandExpr
Unary       = ("-" | "not") OperandExpr

GetMember   = OperandExpr "." IdName
Call        = OperandExpr ArgList

ArgList     = "(" ")"
            | "(" Expr ("," Expr)* ")"

StringExpr            = StringStart (StringText | StringInterpolation)* StringEnd
StringInterpolation   = "{" Expr? "}"
> Can be empty to allow multi-line breaks.

## Tail Expressions

TailExpr    = Break | Continue | Return | Assign

Assign      = OperandExpr ("="|"+="|"-=") Expr

Break       = "break" Expr?
Continue    = "continue"
Return      = "return" Expr?

## Type Names
TypeName        = NativeTypeName
                | Path
> TypeName is an Expr in AST. In the grammar we need to distinguish:
> Path is a TypeName construct, whereas GetMemberExpr would parse the
> same syntax but in expression position. In AST, they both collapse
> into Expr to be better nameable.

NativeTypeName  = "i32" | "i64" | "f32" | "f64" | "string" | "none"
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