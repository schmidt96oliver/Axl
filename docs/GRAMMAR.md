# Top-Level
File    = Stmt*

Stmt    = Expr ";"?
        | VariableDecl ";"  // special-case: ";" _always_ required, because `var a = { => callable } \n (1);`
        | FnDecl ";"?
        | ModuleDecl ";"?
        | UsingDecl ";"     // same ";" rule. Practically always required
// The ";"-rule: ";"? can be omitted if syntax before ends in "}". It is always _allowed_.
// Only exception: VariableDecl


## Declarations
VariableDecl        = "var" Identifier InitializerClause?   // ";" expected at Decl
InitializerClause   = "=" Expr

ModuleDecl  = "module" ModuleName ("{" Stmt* "}")?
UsingDecl   = "using" ModuleName
ModuleName  = Identifier ("." Identifier)*

FnDecl      = ("public"|"private")? NativeFnClause? "fn" Identifier ParamList ("->" (Type | "never")) Body    // Identifier "never" is turned into TypeLiteral NeverKw
NativeFnClause  = "native" "(" StringStart StringText StringEnd ")"
ParamList   = "(" Param* ")"
Param       = Identifier TypeAnnotation ","             // "," _can_ be omitted, if it is the last param


# Body level
// ----- Body/Arm ------
Body        = Block
            | Arm
Arm         = "=>" Expr

// ------ Types -------------
Type        = TypeLiteral
            | Identifier
            | TypeMember
TypeLiteral = "i32" | "i64" | "f32" | "f64" | "string" | "none"
TypeMember  = Type "." Identifier

// ------ Expressions -------
PrimaryExpr = Literal
            | StringExpr
            | "(" Expr ")"
Expr        = PrimaryExpr
            | Block | If | Loop
            | Break | Continue | Return
            | BinaryExpr | NegationExpr | BinaryBoolExpr | BoolNotExpr

// --- Constructs
Block       = "{" Stmt* Arm? "}"
If          = "if" Expr Body ElseClause?     // Condition Expr MUST NOT be Block
ElseClause  = "else" (Body | If)
Loop        = "loop" Body

// --- Control Flow
Break       = "break" Expr?
Continue    = "continue"
Return      = "return" Expr?

// --- Assignment/Call/Member
Assign      = Expr ("="|"+="|"-=") Expr
Member      = Expr "." Identifier

Call        = Expr ArgList
ArgList     = "(" Arg* ")"
Arg         = Expr ","?     // ","? _can_ be omitted if its the last arg

// --- Numeric/Bool/Equality
BinaryExpr      = Expr ("+"|"-"|"*"|"/"|"<"|"<="|">"|">="  |"=="|"!=") Expr
NegationExpr    = "-" Expr
BinaryBoolExpr  = Expr ("and" | "or") Expr
BoolNotExpr     = "not" Expr

// --- Interpolated String
StringExpr      = StringStart (StringText | Interpolation)* StringEnd
Interpolation   = "{" Expr "}"      // NOT body

// --- Literals
Literal         = Identifier
                | StringLiteral