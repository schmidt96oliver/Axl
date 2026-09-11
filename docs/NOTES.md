# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Next:** 
* Cleanup rest of binder
* Reset soft, make good commits into master


**Simplifications**
* SyntaxTree API
  * SyntaxTree.From/Parse, ParseTokens
  * Pass SyntaxTree into SyntaxNode
  * Span not nullable
  * AST: Members, Usings, etc necessary or just walk completely over it?
* Text API
  * Rethink SourceFile, SourceFileView. Really necessary?
* Diagnostics
  * Move to DiagnosticBag.Report***
  * Think about Parser deduping
* Testing API
  * Splitter by .Lines
  * Annotation.TryFrom
  * Diagnostics exposed
  * .Evaluate on TaxlFile
  * Parser allows only one directive
  * Fragment has offset

**Moving On**
* allow any type in string interpolation (that's a lowering problem)
* compound assign
* loop (how to handle arms?)
* return, break, continue
* local fn bodies with "cannot capture" warning.
    * They can see other local fns transitively

**Stashed small ones**:
- LSP: Make Serial (see Omnisharp) and weave CancellationToken

**Regressions**
* `1_i32 == 1_i64`
* `if true => 1 else => "A";` diagnostic message
* `1 + true` squiggle all?

# First features
* i32, i64, f32, f64, bool, string
* expressions: numeric, comparison, boolean
* variables
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