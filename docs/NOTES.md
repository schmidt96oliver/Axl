# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

* Infinite Struct Size check
* Cleanup `LayoutGraph`, maybe rename it to be clearer
* Check if target actually contains T by value
`struct F[T] { s: Empty[F[T]]; }`
`struct H[T] { g: A[H[Empty[T]]]; }`
`struct Empty[T];`

# Questions / Little ones

* ?? Miri: `Environment` -> `ActivationFrame` with .GetRef that throws if exited
* ?? Miri: Store reference as Root (ActivationFrame, LocalId) + Path (FieldSymbol[]). See Claude, think why?
* Miri: `1 / 0` must panic
* Miri: Allow return code and panic tests
* Try Assert tests :)

* ?? Binder: BoundParameter and BoundSelf again?

* ?? Generic operators: `fun +[T]` or `fun ==[T]`
  * at least one parameter must be the containing type with its own parameters (`Pair[T]`), not a specialization
    like `Pair[I32]`. C# has the same rule. Otherwise operator lookup on `Pair[Bool]` would be inconsistent.
    == and operators on T inside the body: same errors as for generic funs until traits exist.

* ?? SyntaxLocations.CalleeOf, CalleeAndTypeArgs, CalleeAndArgs, ...
  * Pass CallSyntax and similar to Diagnostics. They find out where to put it.

* Mir: Validator
  * Phase.Monomorphized: Check that everything is concrete. No TypeParams
  * Call with mut return: ref is directly used and never again

* Mir: StructInit as ImmutableArray<Operand> ordered from declaration/member order. No map

# Milestones

**BaseLib: `Buffer[T]`, `List[T]/MutList[T]`**

**Classes, ByRef types**
* RecursiveStructLayout: Only check type args/fields that store by value

# Later

* Unreachable code analysis
  * Build HashSet for reachable StmtSyntax by walk on MIR. Then walk over syntax tree to report.
* Read/Write analysis
  * Unused variables
  * 'var' can be 'let'
  * unecessary parameter 'mut' + self
  * unecessary assignment
* ?? Drop initializers + definite assignment
* Miri flat execution + stack trace + @StackTraceHidden

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