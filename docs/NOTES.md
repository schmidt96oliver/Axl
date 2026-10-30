# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

* Binder: `ArgumentMutMismatch` -> `MissingMut`, `UnexpectedMut`
* Binder: implement exclusivity check in 'CheckCallToSingleFun'

* Mir: Validator

# Questions / Little ones****

* ?? Local Funs: Lowered by MIR directly?

* ?? Miri: `Environment` -> `ActivationFrame` with .GetRef that throws if exited
* ?? Miri: Store reference as Root (ActivationFrame, LocalId) + Path (FieldSymbol[]). See Claude, think why?

* ?? Binder: Intrinsic lookup on SelfAndParameters instead of singling out receiver?
* ?? Binder: BoundParameter and BoundSelf again?
* ?? Mir: Split OperatorKind and VmCallKind -> Intrinsic = union

# Milestones

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