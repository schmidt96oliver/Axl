# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      

# Now


--------

* Compilation exposes `Namespaces` and `Scripts` it declares. They can be accessed from
  other Compilations.

* Compilation (= one project) is one of
  * Project (stdlib) with `project.axlproj` and dependencies
    * Cyclic dependency graphs must be rejected (?? What happens if not ??)
  * Taxl (many files), only stdlib referenced
  * Free *.axl script. One file, only stdlib referenced

# Questions / Little ones


* ?? Should I get rid of RootNamespace?
* refactor: cleanup FunBuilder (Const instead of True())

* ?? Should StringExpr bind to string.Concat?
* ?? Should all duplicates be added to members? Leads to unecessary AmbiguousRef errors.

# Milestones

## 4. Namespaces and multiple files
* [ ] Taxl file splitter + LSP Project abstraction (handles AxlLocation <-> LspLocation)
* [ ] Compilation takes multiple files
* [ ] Namespaces
  * [ ] Open across same compilation
  * [ ] Free funs + structs
  * [ ] `pub` and private visibility in namespaces
* [ ] Project files
  * [ ] Dependencies
  * [ ] Cyclic dependencies rejected
  * [ ] Namespaces cannot be added to if in another Compilation
  * [ ] LSP automatically opens referenced Compilations
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