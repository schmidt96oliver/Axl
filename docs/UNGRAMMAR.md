Ungrammar names follow SyntaxKind names where they are product ungrammars
and Parse* names where they are sum ungrammars.

# Top-Level
File            = BlockItem*
BlockItem       = (Stmt | Member)


## Member Declarations
Member      = ModifierList (FnDecl | ModuleDecl | GlobalModuleDecl)
                
GlobalModuleDecl= "module" Path ";"

ModifierList    = ("public" | "private")*
// DeclBinder only accepts correct combinations

FnDecl          = NativeClause? "fn" IdName ParamList ("->" (TypeName | "never"))? Body? ";"§
// Identifier "never" is promoted to SyntaxKind.NativeTypeName with TokenKind.NeverKw
// DeclBinder requires Body on non-native functions

NativeClause    = "native" "(" StringExpr ")"
// DeclBinder rejects interpolations inside StringExpr
// Syntactically, we can permit them to get better parses

ParamList       = "(" ")"
                | "(" Param ("," Param)* ")"
Param           = IdName TypeAnnotation       

ModuleDecl      = "module" Path "{" (Stmt | Member)* "}"

## Statements
Stmt        = ExprStmt
            | VarDecl
            | UsingDecl

UsingDecl   = "using" Path ";"

ExprStmt    = BodiedExpr ";"§               // ";" omissible, iff last token is "}"
            | (OperandExpr | TailExpr) ";"  // ";" always required

VarDecl         = "var" IdName TypeAnnotation? InitializerClause? ";"
// InitializerClause required by Binder. Parser is permissive

InitializerClause   = "=" Expr


# Expressions
There needs to be a division between 3 different types of expressions:
    1. BodiedExpr  - They _own_ a body
        * In statement position (as ExprStmt), ";" can be omitted, if it ends in `}`.
    2. TailExpr    - They might _end_ in a body, they don't own
        * ";" always required.
        * They might leak bodies into Exprs where bodies are not allowed, so they need to be their own category.
    3. OperandExpr - Contains bodies only in _clearly delimited_ cases
This is to avoid certain syntax footgun and ambiguities. Also The semicolon
rule is stated much more clearly in this framing.

Expr        = BodiedExpr | OperandExpr | TailExpr

## Body, BodiedExpr, Arm
Body        = Block
            | Arm
Arm         = "=>" Expr
            | "=" Expr      // ERROR PRODUCTION

BodiedExpr  = Block | If | Loop
//          = Expressions that own a body.

Block       = "{" (BlockItem)* Arm? "}"

If          = "if" OperandExpr Body ElseClause?     
// Condition is OperandExpr to disallow any unparenthesized body inside it.
// ERROR PRODUCTION: "=" accepted after OperandExpr

ElseClause  = "else" (Body | If)
Loop        = "loop" Body

## Operand Expressions
Expressions that contain bodies only in very limited, clearly delimited cases.

OperandExpr = Literal | IdName | StringExpr
            | Group
            | Binary
            | Unary
            | Call
            | GetMember

Group       = "(" Expr ")"
// Clearly delimited by `)`, so it may contain a body.

Literal     = "true" | "false"
            | NumberLiteral
            | NativeTypeName

Binary      = OperandExpr ("+"|"-"|"*"|"/"|"<"|"<="|">"|">="  |"=="|"!=" |"and"|"or") OperandExpr
Unary       = ("-" | "not") OperandExpr

GetMember   = OperandExpr "." IdName
Call        = OperandExpr ArgList

ArgList     = "(" ")"
            | "(" Expr ("," Expr)* ")"
// Arg is clearly delimited by `)` or `,`, so may contain body

StringExpr            = StringStart (StringText | StringInterpolation)* StringEnd
StringInterpolation   = "{" Expr? "}"
// Interpolation is clearly delimited by `}`, so may contain body
// It can also be empty to allow multi-line breaks.

## Tail Expressions
Expressions that don't own a body but might contain one.

TailExpr    = Break | Continue | Return | Assign

Assign      = OperandExpr ("="|"+="|"-=") Expr

Break       = "break" Expr?
Continue    = "continue"
Return      = "return" Expr?

## Type Expressions/Clauses
TypeName        = NativeTypeName
                | Path
// Note that TypeName is deliberately a subset of OperandExpr

NativeTypeName  = "i32" | "i64" | "f32" | "f64" | "string" | "none"
// SyntaxKind.NativeTypeName can also hold TokenKind.NeverKw. NeverKw is promoted
// from TokenKind.Identifier if FnDecl return type and only there.

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