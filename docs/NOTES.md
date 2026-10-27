# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

* [ ] `ArgumentMutMismatch`
* [ ] implement exclusivity check in 'CheckCallToSingleFun'
* [ ] MIR: Check types in constructors

* [ ] check CannotAssign diagnostic message
* [ ] check CannotMutateReceiver diagnostic message
* [ ] check CannotPassAsMut diagnostic message
* [ ] check CannotReturnAsMut diagnostic message

* [ ] test: UnexpectedMutReturn, MissingMutReturn. Expression bodied and return 
* [ ] test: `mut EXPR` must reject let + fields, non-mut-self + fields
* [ ] test: `mut EXPR` must accept var + fields, mut-self + fields
* [ ] test: `mut EXPR` in various position
* [ ] test: Cannot call mut fun on non-mut parameter (message)
* [ ] test: mut arguments assign correctly
* [ ] test: fun signatures differ by mut as well

* [ ] Intrinsic lookup: Remove receiver, only by (receiver, arguments) (static and method the same)
* [ ] Split OperatorKind and VmCallKind -> Intrinsic = union

* [ ] Miri: `Environment` -> `ActivationFrame` with .GetRef that throws if exited
* [ ] Miri: Store reference as Root (ActivationFrame, LocalId) + Path (FieldSymbol[]). See Claude, think why?

* Validator 
  * checks type and ref types
  * unreachable
  * terminators

* Try to pass big structs by ref

# Questions / Little ones****

# Milestones

# Later

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