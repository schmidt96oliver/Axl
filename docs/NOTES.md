# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

* Parser: Freeze and stop after probe, missing inserted

* Completion Context
  * `mut` only in valid position; reject `mut mut`
  * Fun modifiers (static -> not mut)
  * Stmt in namespace file
  * Comments

* Improve Symbol -> CompletionItem. Explore possibilites.
* For funs, insert `()`

* A way to show completion status of symbols along the way?
  * `Compilation.WithDebugTrace`
  * `using var _ = Compilation.TraceCompletion(this, nameof(Body))`
  * use in SemanticModel tests

* Identifier completion context
  * SyntaxTree.TokenAt binary search from lazy flat token list


# Now

# Questions / Little ones

* ?? Miri: `Environment` -> `ActivationFrame` with .GetRef that throws if exited
* ?? Miri: Store reference as Root (ActivationFrame, LocalId) + Path (FieldSymbol[]). See Claude, think why?

* ?? Binder: BoundParameter and BoundSelf again?

* ?? SyntaxLocations.CalleeOf, CalleeAndTypeArgs, CalleeAndArgs, ...
  * Pass CallSyntax and similar to Diagnostics. They find out where to put it.

* ?? LayoutGraphNode to TypeDefinitionSymbol

* Mir: Validator
  * Phase.Monomorphized: Check that everything is concrete. No TypeParams
  * Call with mut return: ref is directly used and never again

* Mir: StructInit as ImmutableArray<Operand> ordered from declaration/member order. No map

# Milestones

**Panic**: `1/0` panics, return code, taxl panic, Assert tests

**Buffer**: `Buffer[T]`, `List[T]/MutList[T]`

**Classes, ByRef types**: Class, `===` operator
* RecursiveStructLayout: Only check type args/fields that store by value

**Mir Analysis**
* Unreachable code analysis
    * Build HashSet for reachable StmtSyntax by walk on MIR. Then walk over syntax tree to report.
* Read/Write analysis
    * Unused variables
    * 'var' can be 'let'
    * unecessary parameter 'mut' + self
    * unecessary assignment

**LSP Work**: Completion context, signature help, folding ranges, symbol highlighting, rename
* Churns the data structure heavily

**Enums/Exhaustiveness/Patterns**

**Traits/Constraints**

# Loose Ends
* Overload resolution specificity
   `fun A[T](t: T){} fun A[T](t: I32){}   A[I32](1);`
* Operators and generics: `pub fun +[T](t: T)` how do they interact?
* Mut exclusivity check
* RecursiveStructLayout: Only check type args/fields that store by value

# Later

* ?? Drop initializers + definite assignment
* Miri flat execution + stack trace + @StackTraceHidden
* LSP Debug through Code Lens (show MIR through external file), Code Actions, Document Symbols (Syntax tree, Bound tree, Symbol tree)

# Ideas
[Ideas](IDEAS.md)

* Axl: Named arguments
* Axl: Default fun arguments
* Axl: Default struct field values
* Axl: `static let` constants

* Axl: Nested structs
  * Enclosing types instance members rejected (MemberOfEnclosingType)
  * Nested type through instance rejected (or like ColorColor)
  * Private nested type leaking through public fun rejected
* Axl: `@derive(==)` and `@derive(ToString)`

* LSP: Completion Context (SemanticModel and ad-hoc binding)

* refactor: Rework syntax tree; Trivia to Tokens; Concrete SyntaxNode construction
* refactor: Single sealed Token class (Id => .Text; StringText => .EscapedText lazy on every token; NumLiteral => Split Number + Suffix)