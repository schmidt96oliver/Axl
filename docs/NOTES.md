# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

* boundTypeGroup. ResolveTypeGroup. ResolveFunGroup

* Mir TypeInit -> StructInit

* wrong type params: Should AddResolvedSymbol ??

* NotAType: Should be TypeParameterArityMismatch instead, see test.taxl
* SubscriptNotAllowed: Should be TypeParameterArityMismatch instead, see test.taxl

* Name clash `struct S[T] { T: I32 }`
* Infinite size check, follow into generic fields
  * `struct Node[T] { next: Node[T] }`
  * `struct A[T] { x: T } struct B { a: A[B] }`
  * `struct S[T] { s: S[Box[T]] }`

# Questions / Little ones

* ?? Miri: `Environment` -> `ActivationFrame` with .GetRef that throws if exited
* ?? Miri: Store reference as Root (ActivationFrame, LocalId) + Path (FieldSymbol[]). See Claude, think why?

* ?? Binder: BoundParameter and BoundSelf again?

* ?? Generic operators: `fun +[T]` or `fun ==[T]`
  * at least one parameter must be the containing type with its own parameters (`Pair[T]`), not a specialization
    like `Pair[I32]`. C# has the same rule. Otherwise operator lookup on `Pair[Bool]` would be inconsistent.
    == and operators on T inside the body: same errors as for generic funs until traits exist.

* Mir: Validator
  * Phase.Monomorphized: Check that everything is concrete. No TypeParams
  * Call with mut return: ref is directly used and never again

* Mir: StructInit as ImmutableArray<Operand> ordered from declaration/member order. No map

# Milestones

**Generic structs** (`Pair[A, B]`, fields, construction, static funs like `List[I32].New()`)
* Constructed types, interning, member substitution.
* InfiniteSize check after substitution (see above).
* and methods

**Inference**
* Rework overload resolution
    * BindArguments -> Bind each candidate fully, tentatively `BindSingleCall` (returns Viability)
      -> sort with viability ->
    * One left, viable: merge it and return the BoundCall.
      One left, not viable: merge its diagnostics.
      Several left, viable: PickBest, then either one result or AmbiguousCall.
      Several left, not viable: a specific message per grade, for example:
    * Implement rule for specificity in overload resolution (-> Claude)
* Errors: CannotInferTypeArgument (hint: “Specify them explicitly, like ‘F[…]‘.”); conflicting inferences
        (“Inferred both
        ‘I32‘ and ‘String‘ for ‘T‘.”).
* Never: treat it as the bottom type, so F(panic(), 1) infers I32 rather than conflicting.
* Overload resolution: decide the tie-break between generic and non-generic candidates (usually non-generic wins).

**Infinite instantiation detection**
* Depth limit first, then the static cycle check.
* Build static graph cycle detection

**BaseLib: `Buffer[T]`, `List[T]/MutList[T]`**

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