# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

**Next:** 
* error message for non-matching if arms


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
* `if true 1 else "A";` diagnostic message
  * "'if' must have both main and 'else' branches when used as an expression."
* `if (true) else 1;` generates UndefinedName
* `if (true) a(); else b();` allow the semicolon is stmt position?

# Taxl: Multiple files
* SourceText gets Origin (SourceText, Offset); construction by SourceText.Subtext
* SourceLocation always refers to root source text
* .GetLocation walks origins
* SourceText.Contains checks for origin as well
* Compilation.GetSyntaxTreeAt(location) just checks text.Contains(location)

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