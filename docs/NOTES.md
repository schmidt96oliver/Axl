# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

* reject all Annotations. Add to syntax

* Add Compilation refs to Symbol

* "var fun" -> "mut fun"

# Questions / Little ones****

* refactor: cleanup FunBuilder (Const instead of True())

* ?? Should StringExpr bind to string.Concat?
* ?? Should all duplicates be added to members? Leads to unecessary AmbiguousRef errors.

# Milestones

## 5. `Base` namespace as Axl-Code
[Proposal](Proposals/Primitives.axl)
* [ ] Annotations (private inside BaseLib)
    * [ ] `@Intrinsic` looks up funs agains LookupTable and checks signatures
    * [ ] `@Primitive` marks types primitive; rejects any fields declared
* [x] Compilation.ReferencedCompilations (only BaseLib implicitly)
  * [x] Namespace closed across Compilations
* [ ] Project marker files (without dependencies)
* [x] BaseLibrary searches for correct symbols
* [ ] BaseNamespace shared across Compilations (through LSP)

* [ ] Replace entire BaseNamespaceSymbol with axl text!!


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