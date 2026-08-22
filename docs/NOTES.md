# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Next**: 
  * If: else clause
  * VarDecl: initializer clause, type annotation
  * block: Items = <Expr>; Arm = <Arm>; FnDecls = <FnDecl>

**Small points**:
* SyntaxNode enumerator (all nodes, BFS/DFS, all tokens in sequence)
* ?? rename `QualifiedName` to `PathName`
* rename `FileId` to `UnitId` (because it's not a _file_ per se. Several `FileId` could point to the same file on disc)

# Semantics

## Phase 1 — Script HIR
Script files only. 
Bind expressions and `VarDecl`. 
Build the HIR interpreter. 
`UnsupportedDiagnostic` marks corpus files to skip.
Taxl (Splitter, @check, @runpass)
Folding Ranges for segments

* Types are not symbols; TypeContext on Compilation
  * ErrorType, NeverType (not different)
* `LocalSymbol` belong to Hir body
* Immutable, fixed, nested scopes
  * Shadowing/new declaration creates new scope
* Binder is owned by `Compilation`; no SemanticModel (yet)
* Special case `Standard.PrintLine` until fns
* expected-type propagation; `loop`/`break` type checks
* `SyntaxKind.Error` → bind children, wrap in `HirError`.

## Phase 2 — Script functions

Local fns and script fns handled identically. 
Signature binding separate from body binding.
native fns and their validation
return-type checking.
divergence tracking, definite return, never return type
overloads and overload resolution

* Each overload is it's own `FunctionSymbol`; Lookup returns `OneOrMany<Symbol>`


## Phase 3 — Module files

Compilation carries everything, `.Compile` picks entry script
* Lsp uses one Compilation for all *.axl
* And separate Compilation for each *.taxl
Script files invisible to other files
ModuleScope, FileScope
Multi-file module merging
visibility and modifier binding — lint for wrong order, error for duplicates.

## Phase 4 — Using

Parser work to allow `using` at every position
insert aliases into the relevant scope

# First features
* i32, i64, f32, f64, bool, string
* literals integral, float
* expressions: numeric, comparison, boolean
* variables
* blocks, if, loop (with break expression, continue), return
* none, never
* string interpolation, escaped
* native functions: Print, PrintLine, ToString
* hoisted, overloaded functions
* multi-file modules


# Possible Refactors
## String Awkwardness: Lexer <-> Parser
Lexer emits flat tokens and Parser must reconstruct the Lexers ideas about strings
(see `WillStringBeContinued`). 
* Idea: Lexer emits TokenTree. A StringTree = `"` + Text + InterpolationTree
* Idea: Tokens are a singly linked list.
  * normal Tokens have on `Next` pointer
  * DelimitedToken has pointers `Next` and `EndGroup`
  * StringInterpolation: Parser can easily reconstruct the Lexers ideas without unbounded lookahead
  * RecoverTo: {} balancing is free, because it can skip until `EndGroup`