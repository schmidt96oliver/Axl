# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

* [x] `Test(a, (a = 11));` evaluation order. Others with side-effects?

* [ ] `Place.Local` kind and return local
* [ ] Decide deref only by parameter kind, not by RefType (in LowerPlace)
* [ ] RefTypeSymbol: Extract, add doc, maybe already add Kind { Object, Place }
* [ ] Const value printing string "" and "\n", truncate
* [ ] Intrinsic lookup: Remove receiver, only by (receiver, arguments) (static and method the same)

* [ ] Split OperatorKind and VmCallKind -> Intrinsic = union
* [ ] Add Return Place (always _0) and remove "return expr".

* [ ] Miri: `Environment` -> `ActivationFrame` with .GetRef that throws if exited
* [ ] MirProgram.GetSignature(FunSymbol) has parameter ordering and byref parameter. Cached
* [ ] Miri: Store reference as Root (ActivationFrame, LocalId) + Path (FieldSymbol[]). See Claude, think why?

* Validator 
  * checks type and ref types
  * unreachable
  * terminators

* Try to pass big structs by ref

# Questions / Little ones****

# Milestones

# Later

* implement `mut` parameter. Mir should already handle it correctly.

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