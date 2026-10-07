# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Next:** 

* test: StructInit evaluation order

* visibility tests are added; test for methods :)

* overload resolution: filter visibility

* add leaking private members to worklist somewhere or do it :D

* cleanup FunBuilder (Const instead of True())

# Roadmap

## 3. Structs (in script)
One type (maybe struct) inside scripts.
* [x] Fields, pub/default private
* [x] Initialization 
  * [x] Fields must be named arguments (Can be omitted, if param name is the same as argument)
  * [x] Reject, if any field is private
  * [x] Reject for primitves
* [x] Fields access and assignment through `var`
  * [x] rejected through `let`
  * [x] chained assignment mutates in place through `var`, rejected through `let`
* [x] Copy semantics
* [x] Cyclic references in fields are disallowed. Also nested.
* [x] Cannot shadow funs, variables
* [x] Empty structs; Unit is non-primitive empty struct; braces not required 
  * [ ] Enhance Empty.taxl with fun examples :)

* [ ] `var`/non-var methods
  * [ ] Reject `pub, var` on free funs
  * [ ] only `pub` can be called from outside
  * [ ] explicit `self`
  * [ ] reject field mutation in non-var context
  * [ ] non-var can only call non-var methods through `self`
  * [ ] `var fun` can only be called through `var`. Also nested cases.
  * [ ] implicit `self`
  * [ ] local funs become methods as well (`self` is captured)
  * [ ] Can see private fields

* [ ] `static fun`s
  * [ ] only `pub` can be called from outside
* [ ] User-declared operators + generation of `!=, <=, >, >=` from `==, <`
* [ ] User-declared ToString

Stretch goals
* [ ] Getter funs
* [ ] Default field values
* [ ] `static let` constants
* [ ] `@derive(==)` and `@derive(ToString)`
* [ ] Overloaded construction
* [ ] Nested structs (beware access through instance: `instance.SubType` must be rejected)

* Pointers to structs: Only of `self` and chained fields
* private means: Only visible inside struct body

## 4. Namespaces and multiple files
* [ ] Namespaces visible anywhere
* [ ] `pub` visibility
* [ ] Taxl handles multiple files
* [ ] LSP manages Compilation objects
* [ ] Using directives

## 5. `Base` namespace as Axl-Code
* [ ] `@intrinsic` and `@primitive` annotations
  * `@primitive(I32)` mapped to PrimitiveKind on struct
  * `@intrinsic` maps receiver and params to PrimitiveKind (instead of type name)
* [ ] BaseNamespaceSymbol searches for correct symbols
* [ ] IntrinsicLookup checks signatures
* [ ] Replace entire BaseNamespaceSymbol with axl text :)).

# Little proposals
* LSP: Make Serial (see Omnisharp) and weave CancellationToken to avoid concurrency awkwardness.
* Axl: Named arguments as `callee(parameter = value, param2 = value2)`

# Proposals
## LSP-Work: Completion Context
Add Completion Context, Goto Definition, Hover, Highlighting, ...
* Recoverable (and probably immutable) scopes;
  * Binder could them them in sidetable or on data structure 
  * or scopes are cached and created lazily
* Binder can run ad-hoc to bind special items the LSP asks for
* SemanticModel builds indices to answer LSP questions

## Lexer/Parser: Resolve string interpolation awkwardness
Currently, Lexer emits flat tokens and Parser must reconstruct the Lexers ideas about strings
(see `WillStringBeContinued`).
* Idea: Lexer emits TokenTree. A StringTree = `"` + Text + InterpolationTree
* Idea: Tokens are a singly linked list.
    * normal Tokens have on `Next` pointer
    * DelimitedToken has pointers `Next` and `EndGroup`

## Lexer: Single sealed Token class
Currently, some TokenKinds have a derived class which carries a value. These can be
flattened into a single Token class.
* IdentifierToken => Just the token, `.Text` is enough
* StringTextToken => Provide `.EscapedText` on every Token, lazily computing from a generalized
  `SyntaxFacts.EscapeSequences`. Lexer should read this too.
* NumberLiteralToken => Split into NumberLiteral with pure text and a following NumberSuffix
  (`I32NumberSuffix`, `I64NumberSuffix`, ...). This split is wanted for diagnostics anyway.

## SyntaxTree: Rework
*BIG Rework*, requires multiple steps.
*Issues*:
  * Parent and Tree pointers make mutable nodes necessary. 
  * Tokens that come from the Lexer have no SyntaxTree reference, so access of Location or Text will throw. 
  * Parser and AST layer can drift silently apart. 
  * Nullable Range is awkward to handle.
*Idea(s)*:
  * Construction through `SyntaxTree.From/LoadFile` similar to SourceText
  * Trivia attached to Tokens. As necessity that SyntaxNodes only represent non-trivia. This improves the
    split between Range and FullRange. The entire pipeline doesn't need to know about trivia anyway.
  * Directly construct SyntaxNode with children. Parser needs to pass all tokens and trees. This removes
    the matklad style events. This could also improve overall design of the parser and readability.
  * Collapse FileSyntax and SyntaxTree. SyntaxTree can be the root node. This might simplify the design,
    especially around adding Tree.

## Taxl: Multiple files
TestFile needs to split one taxl file into multiple SourceTexts and map
diagnostics and annotations accordingly.
* Idea:
  * SourceText gets Origin (SourceText, Offset); construction by SourceText.Subtext
  * SourceLocation always refers to root source text
  * .GetLocation walks origins
  * SourceText.Contains checks for origin as well
  * Compilation.GetSyntaxTreeAt(location) just checks text.Contains(location)

## Diagnostics: Flat bag
Currently, diagnostics are their own data structure. It would be easier and less
boilerplate-heavy to provide `Report*` methods on DiagnosticBag. Only the parser
needs a new way to report them (either through `Make*` or on a different mechanism).
*Other option*: Report them locally, which keeps the message and data coupled to the
reporting site. Which does make sense architciturally, since they will be searched together.

## Axl: Member generation
*Requires*: Structs/Type
Currently, all operators must be declared on the type. It is enough to declared
`==` and `<=` (or `<`) and then compiler-generate `!=, <=, >, >=` from those. It will enhance
the language with less boilerplate. Care needs to be taken to (1) allow overwritten
declarations for speed and (2) maybe lint operators that can be generated. Also
`==` and `ToString` could be generated from fields.

# Design and feature ideas
## From https://core-lang.dev/design.html
> "Always rules" are better than "almost rules":
>     .   selects
>     =   assigns
>     ==  structural equality
>     === reference equality
>     :   ascribes (a type) = 'zuweisen'
>     @   annotates
>     ()  encloses terms
>     []  encloses types
>     

Possibly add or change:
>     | | lambda
>     < > encloses types
>     [ ] encloses collections
>      ?  maybe/option
>      !  nope

* Arrays: `Array[Int32]`. Needs generics
  * construct `Array[Int32](1, 2, 3)`
  * get `array.Get(index)`
  * `array.Set(index, value)` through @intrinsic functions

* Lambdas and non-local return.
  * Return in a lambda should ideally return from the fun it's written in (not the lambda)
    or be disallowed. Non-local return needs escape-analysis; does not allow lazy iterators
    and is a little more complicated. A way needs to be found to return from the lambda. Research
    Kotlins non-local returns.

* Extension syntax: `fun Type.ExtensionMethod()` instead of `extend() { }` block.

* Static/Private/Public members: Maybe with `public: `, `private: `, `static: ` similar to C++.

# Archive

## 1. Running scripts (no funs)

* [x] Intrinsic Print
* [x] Scripts bind
* [x] Duck-typed ToString
* [x] Bind methods
* [x] `let` binding
* [x] Treewalking Interpreter on BoundTree
* [x] Taxl Run tests

## 2. Funs (in script)

* [x] Forward-declaration of funs
* [x] Binding of fun bodies
* [x] Overloads
* [x] Local funs
* [x] Reject shadowing of funs on same scope
* [x] Reject captured variables
* [x] Definite Return Analysis (needs MIR or ad-hoc)
* [x] Compiler generated versions of `!=, <=, >, >=` from `==, <`
* [ ] Named arguments