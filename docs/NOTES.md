# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Next:** 
* [x] Add ModuleSymbol; Make BaseModuleSymbol a ModuleSymbol
* [x] Add IntrinsicFnSymbol, Intrinsic
* [x] Add type name resolution
* [x] Remove native type keywords
* [x] Bind Operators as intrinsic fns
* [x] Bind BinaryExpr as BoundIntrinsicCall and lower derived operators

* [x] Add `<=` intrinsics****
* [ ] Add generic operator text somewhere
* [ ] Error messages for undefined operator "1 != 1.1", "1 >= 2"
 

* [ ] Make param type annotation required in Parser
* [ ] Churn through Parser.BrokenTrees

* [ ] ?? Replace `and/or` with `&& ||`. Reason: Familiarity and possibly conflict with "and/or" patterns later
* ?? Common base class for "Type" and "Module" -> "ModuleOrTypeSymbol" that has members and member lookup


* [ ] Corpus Tests
  * [ ] Primitives bind as `Base.I32` and `I32` for `I32, I64, F32, F64, Bool, String, Unit`
  * [ ] Variables, `Base` rejeceted as type
  * [ ] Types, Module rejected as value
  * [ ] Undefined Members on `Base`
  * [ ] Undefined Names inside patzhs `A.B.C`
  * [ ] `Never`, `Error` not nameable
  * [ ] All operators `+ - * / < <= > >= not and or`

**Syntax ideas from https://core-lang.dev/design.html**
> "Always rules" are better than "almost rules":
>     . selects
>     = assigns
>     : ascribes (a type) = 'zuweisen'
>     @ annotates
>     () encloses terms
>     [] encloses types

* _design_: arrays as `Array[Int32]`, construct `Array[Int32](1, 2, 3)`, get `array.Get(index)` and `array.Set(index, value)` through @internal functions

* _design_: `pub func`, `@internal pub func`, `@internal func`; static is encoded in signature? `func(self)` vs `func(arg: Int32)`
  * allows no body on all `func`'s; Binder ensures: `Unit`-typed or `@internal`

* _design_: duck-typing on `ToString` for the time; `@internal` symbols allow that straight-away

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