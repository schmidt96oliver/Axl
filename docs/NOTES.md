# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Next:** 
* API: SyntaxTree.From does parsing

**Moving On**
* ?? carry expected type into exprs -- or -- delete that feature
* allow any type in string interpolation (that's a lowering problem)
* compound assign
* loop (how to handle arms?)
* return, break, continue
* local fn bodies with "cannot capture" warning.
    * They can see other local fns transitively

**Stashed small ones**:
* API: `SyntaxTree.ParseFrom`, `*Tree/Table.BuildFrom`
- LSP: Make Serial (see Omnisharp) and weave CancellationToken

**Regressions**
* `1_i32 == 1_i64`
* `if true => 1 else => "A";` diagnostic message
* `1 + true` squiggle all?

# Taxl
**Requirements**
- Split files "//---".
- Allow empty file names (split tests inside same compilation)
- Split expected outputs "//=== stdout"
- Directives "//@run-pass", "//@run-panic", "//@check"
- "//~error" and "//~lint" on this line
- "//~  ^^^ type name" expression type checking

- Error resilient for LSP
- No need for proper diagnostics
- Test runner can fail with "Invalid taxl".
- API for rewriting in bless mode
  - entire output block
  - inline annotations

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