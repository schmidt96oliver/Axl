# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

* Never nameable
* Panic, Assert, ...
* Miri flat execution + stack trace + @StackTraceHidden

* `ArgumentMutMismatch` -> `MissingMut`, `UnexpectedMut`
* implement exclusivity check in 'CheckCallToSingleFun'

* Intrinsic lookup: Remove receiver, only by (receiver, arguments) (static and method the same)
* Split OperatorKind and VmCallKind -> Intrinsic = union

* Validator

# Questions / Little ones****

* ?? Miri: `Environment` -> `ActivationFrame` with .GetRef that throws if exited
* ?? Miri: Store reference as Root (ActivationFrame, LocalId) + Path (FieldSymbol[]). See Claude, think why?

* ?? BoundParameter and BoundSelf again?

# Milestones

# Later

* Unreachable code analysis
  * Build HashSet for reachable StmtSyntax by walk on MIR. Then walk over syntax tree to report.
* Unused variables
* ?? Drop initializers + definite assignment

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