# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

**Source-declared operators**

- [x] Parse FunNames
- [x] Reject Operators on local/script
- [x] Reject Operators on static funs
- [x] Reject static, var keywords
- [x] Allow only single parameter for binary ops and no parameter for unary ops
- [x] Respect accessibility
- [ ] Generate `!=` from `==` if not present
- [ ] Generate `<=, >, >=` from `<` if not present
  - [ ] ComparisonNotDerivable, because <=(RIGHT) needs ==(RIGHT), SELF.>(RIGHT) needs RIGHT.<(SELF)
- [x] Allow overloads


# Questions / Little ones

* ?? Taxl expectation: Can I collect across entire doc? For `Operators.taxl`, its useful to have expected output next to PrintLine

* refactor: cleanup FunBuilder (Const instead of True())

* ?? Should StringExpr bind to string.Concat?
* ?? Should all duplicates be added to members? Leads to unecessary AmbiguousRef errors.


# Milestones

## 3. Structs (in script)
* [ ] Source-declared operators

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

# Little proposals


# Proposals
## Axl: Lambdas with inference
* Fun type expression to name fun types
* Lambdas with full type annotations
* Parameter and return inference (interacts with overload resolution; required tentative argument binding into scrap diagnostic bag)

# Design and feature ideas