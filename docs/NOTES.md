# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Symbol** = Outer surface; Eagerly built by declarations; lazy for binding needs
**Scope** = Eagerly built per-file; local scopes built during binding

1. Definition *Scope* and *Symbol*
   - What do they carry? What is queried elsewhere?
2. Pipeline AST to Bound- and Type-checked structure; How to avoid cycles?
3. Scopes persistent or transient? When created?

**Requirements**
**LSP**:
    1. Get file diagnostics
    2. What names are in scope at position n?
**Signature binding**:
    1. What names are in scope at module/file level?
**Code=Body binding**:
    1. What names are in scope at module/file level?
    2. What names are in scope inside _this block_?
    3. What locals are in scope at _this position_?
    4. Which declaration do they refer to? (for type-checking)

- LocalSymbols have Type => must be created by Binder _during_ type-checking


---- Roslyn:
**Symbol**
    * Container (module, assembly, type, ...)
    * Name
    * Visibility/Modifiers
    * Declaring Syntax (location and node)
    * Type
    * _MethodSymbol_: ParameterTypes, ReturnType, Parameter_Symbols_; NO local fns
    * _NamespaceSymbol_: Members belong to symbol; lazily merged



**Scopes**
1. Single mutable `Scope` weaved as parameter
   - Awkward: Mutable, mutation not visible, hard to reason about, declaration can be forgotten, hard to answer "Whats visible at (1,3)?"
   - Nice: Easy, pragmatic

QUESTIONS:
1. Persistent or binder-temporary?
2. Immutable or mutable?

**Next**: 


**Small points**:
* Regressions: `1+[EOF]`, `-[EOF]`
* SyntaxNode enumerator (all nodes, BFS/DFS, all tokens in sequence)
* rename `FileId` to `UnitId` (because it's not a _file_ per se. Several `FileId` could point to the same file on disc)
* Tests:
  * `MangledCorpus`
  * 1 invariant = 1 test

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
* `SyntaxKind.ErrorExpr` → bind children, wrap in `HirError`.

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