# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Next:** 
* Cleanup native operator info/kind + FindNativeOperator

* Cleanup rest of binder
* Reset soft, make good commits into master

**Simplifications**
* SyntaxTree API
  * SyntaxTree.From/Parse, ParseTokens
  * Pass SyntaxTree into SyntaxNode
* Text API
  * Rethink SourceFile, SourceFileView. Really necessary?
* DiagnosticBag.Report*** instead of data structure

**Moving On**
* ?? carry expected type into exprs -- or -- delete that feature
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
* literals integral, float
* expressions: numeric, comparison, boolean
* variables
* blocks, if, loop (with break expression, continue), return
* none, never
* string interpolation, escaped
* native functions: Print, PrintLine, ToString
* hoisted, overloaded functions
* multi-file modules


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