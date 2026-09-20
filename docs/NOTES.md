# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Next:** 
* [ ] Syntax highlighting for types
  * Probably requires rework of BoundNode structure


* Add Hover symbol kind

* LSP: Add SyntaxFacts.IsKeyword

# Roadmap

## 1. Running scripts (no funs)
* [ ] Intrinsic Print
* [ ] Scripts bind
* [ ] Duck-typed ToString
* [ ] `let` binding
* [ ] Treewalking Interpreter on BoundTree
  * --or-- MIR and MirInterpreter

## 2. Funs (in script)
* [ ] Forward-declaration of funs
* [ ] Local funs
* [ ] Cannot be shadowed by local (variable or parameter)
* [ ] Definite Return Analysis (needs MIR or ad-hoc)
* ?? Named arguments

## 3. Type (in script)
One type (maybe struct) inside scripts.
* [ ] Initialization
* [ ] Fields
* [ ] (Static) fun and methods
* [ ] User-declared operators
* [ ] User-declared ToString
* [ ] `pub` visibility
* ?? Member generation

## 4. Modules and multiple files
* [ ] Modules visible anywhere
* [ ] `pub` visibility
* [ ] Taxl handles multiple files
* [ ] LSP manages Compilation objects
* [ ] Using directives

## 5. `Base` module as Axl-Code
* [ ] `@intrinsic` and `@primitive` annotations
* [ ] BaseModuleSymbol searches for correct symbols
* [ ] IntrinsicLookup checks signatures
* [ ] Replace entire BaseModuleSymbol with axl text :)).

# Little proposals
- LSP: Make Serial (see Omnisharp) and weave CancellationToken to avoid concurrency awkwardness.
* Binder: Duck-type `ToString` for string interpolations
* Axl: Replace `and, or` with `&&, ||`. For familiarity and possibly conflict with "and/or" patterns later
* Diagnostics: Report unsupported only on the first token. It is much more fun to play without those squiggles.
* Taxl: Expected output through `//@expect "1stline\n2ndline"` normalized.
* Axl: Named arguments as `callee(parameter = value, param2 = value2)`
* Compiler: Replace `SymbolName` with `string`. There is really no reason to have a separate type.

# Proposals
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
*Idea*:
  * Construction through `SyntaxTree.From/LoadFile` similar to SourceText
  * Trivia attached to Tokens. As necessity that SyntaxNodes only represent non-trivia. This improves the
    split between Range and FullRange. The entire pipeline doesn't need to know about trivia anyway.
  * Directly construct SyntaxNode with children. Parser needs to pass all tokens and trees. This removes
    the matklad style events. This could also improve overall design of the parser and readability.

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

## Axl: Member generation
*Requires*: Structs/Type
Currently, all operators must be declared on the type. It is enough to declared
`==` and `<=` (or `<`) and then compiler-generate `!=, <=, >, >=` from those. It will enhance
the language with less boilerplate. Care needs to be taken to (1) allow overwritten
declarations for speed and (2) maybe lint operators that can be generated. Also
`==` and `ToString` could be generated from fields.

# Design notes and ideas
## From https://core-lang.dev/design.html
> "Always rules" are better than "almost rules":
>     . selects
>     = assigns
>     : ascribes (a type) = 'zuweisen'
>     @ annotates
>     () encloses terms
>     [] encloses types

Possibly add:
>     | | lambda

* Arrays: `Array[Int32]`. Needs generics
  * construct `Array[Int32](1, 2, 3)`
  * get `array.Get(index)`
  * `array.Set(index, value)` through @intrinsic functions