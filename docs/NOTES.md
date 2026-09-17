# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Next:** 
* `if (true) else 1;` generates UndefinedName
* `var a = i32` fix crash (`EatExprHead` does not parse nativetypename)

**Syntax ideas from https://core-lang.dev/design.html**
> "Always rules" are better than "almost rules":
>     . selects
>     = assigns
>     : ascribes (a type) = 'zuweisen'
>     @ annotates
>     () encloses terms
>     [] encloses types

* [x] replace `->` with `:` (gets rid of `->`; unifies TypeAnnotationClause)
* [x] replace `=>` with `=` (get rid of `=>` and the production is already there)
* [ ] drop compound assign (for now)

* ?? replace `fn` with `fun` 

* _experiment_: @internal types and functions:
  * `Int32, Int64, Float32, Float64, Unit, Bool, String` are all symbols nameable through symbol lookup
  * They are internally defined
  * Have @internal functions `fn +(Int32 other)`; Binary operators resolve to functions with operator name

* _design_: arrays as `Array[Int32]`, construct `Array[Int32](1, 2, 3)`, get `array.Get(index)` and `array.Set(index, value)` through @internal functions

**Moving On**
* allow any type in string interpolation (that's a lowering problem)
* local fn bodies with "cannot capture" warning.
    * They can see other local fns transitively

**Simplifications**
* SyntaxTree API
    * SyntaxTree.From/Parse, ParseTokens
    * Pass SyntaxTree into SyntaxNode
    * Span not nullable
    * AST: Members, Usings, etc necessary or just walk completely over it?
    * Tokens coming from Lexer should also have .Text, .Location, etc...
* Diagnostics
    * Move to DiagnosticBag.Report***
    * Think about Parser deduping

**Stashed small ones**:
- LSP: Make Serial (see Omnisharp) and weave CancellationToken

**Regressions**
* `1_i32 == 1_i64`
* `var a = i32`

# Taxl: Multiple files
* SourceText gets Origin (SourceText, Offset); construction by SourceText.Subtext
* SourceLocation always refers to root source text
* .GetLocation walks origins
* SourceText.Contains checks for origin as well
* Compilation.GetSyntaxTreeAt(location) just checks text.Contains(location)

# First features
* i32, i64, f32, f64, bool, string
* expressions: numeric, comparison, boolean
* variables****
* string interpolation, escaped
* blocks, if, loop (with break expression, continue)

* fns
  * none, never type
  * return expr
  * forward-declared, overloaded
* native functions: Print, PrintLine, ToString

* multi-file modules
  * `using` directive


# Possible Refactors
## String Awkwardness: Lexer <-> Parser
Lexer emits flat tokens and Parser must reconstruct the Lexers ideas about strings
(see `WillStringBeContinued`). 
* Idea: Lexer emits TokenTree. A StringTree = `"` + Text + InterpolationTree
* Idea: Tokens are a singly linked list.
  * normal Tokens have on `Next` pointer
  * DelimitedToken has pointers `Next` and `EndGroup`
  * StringInterpolation: Parser can easily reconstruct the Lexers ideas without unbounded lookahead
  * RecoverTo: {} balancing is free, because it can skip until `EndGroup`