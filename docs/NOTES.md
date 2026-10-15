# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

# Questions / Little ones

* refactor: cleanup FunBuilder (Const instead of True())

* ?? Should StringExpr bind to string.Concat?
* ?? Should all duplicates be added to members? Leads to unecessary AmbiguousRef errors.

* bug: LSP folding range + don't collapse taxl directives

# Milestones

## 4. Namespaces and multiple files
* [ ] Namespaces visible anywhere
* [ ] `pub` visibility on fun/struct
* [ ] Taxl handles multiple files
* [ ] LSP manages Compilation objects
* [ ] Using directives

## 5. `Base` namespace as Axl-Code
[Proposal](Proposals/Primitives.axl)
* [ ] `@intrinsic` and `@primitive` annotations
  * `@primitive(I32)` mapped to PrimitiveKind on struct
  * `@intrinsic` maps receiver and params to PrimitiveKind (instead of type name)
* [ ] BaseNamespaceSymbol searches for correct symbols
* [ ] IntrinsicLookup checks signatures
* [ ] Replace entire BaseNamespaceSymbol with axl text :)).


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