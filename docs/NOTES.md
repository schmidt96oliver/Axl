# Axl Project
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

# Questions / Little ones

* ?? Remove or rework fun overloads
 

* ?? BindLookupResult: Report WrongKind error before anything else; remove isThisField in BindFieldAccess

* Mir: Validator
  * Phase.Monomorphized: Check that everything is concrete. No TypeParams
  * Call with mut return: ref is directly used and never again

* Binder: BindLookupResult should report WrongKind error before binding

* ?? Fun: TokenSet iteration with `i & -i` trick

* Separate IfStmt and IfExpr in syntax (-> SemanticModel.BoundNodeFor tests)

# Milestones

**Classes, ByRef types**: Class, `===` operator
5. `===`: reference types only, rejected on `T`

**UnsafeBuffer**: `UnsafeBuffer[T]`, `List[T]/MutList[T]`
* Miri: BufferAtPointerRoot

# Loose Ends
* Overload resolution specificity
   `fun A[T](t: T){} fun A[T](t: I32){}   A[I32](1);`
* Operators and generics: `pub fun +[T](t: T)` how do they interact?
* Mut exclusivity check
* RecursiveStructLayout: Only check type args/fields that store by value

* SemanticModel and Scopes:
  * Record (Scope, LookupOptions) from Binder and removed Completion Probe

# Later

* `===` and `=!=` for reference equality

* Miri flat execution + stack trace + @StackTraceHidden

* Mir Inliner
  * Measure performance for number heavy code before and after!
  * `@Inline`, `@NoInline` + small size threshold

* Mir Analysis
  * Unreachable code analysis
      * Build HashSet for reachable StmtSyntax by walk on MIR. Then walk over syntax tree to report.
  * Read/Write analysis
      * Unused variables
      * 'var' can be 'let'
      * unecessary parameter 'mut' + self
      * unecessary assignment

# Ideas
[Ideas](IDEAS.md)

* LSP: Signature Help, Folding Ranges, Taxl Annotation completion, Rename

* refactor: Split (Lex)Token and SyntaxToken
  * Id => .Text
  * StringText => Text Token + StringEscape token; SyntaxFacts.IsEscape/GetEsxcapedText is source of truth
  * NumLiteral => Split into Number + Suffix
  * Lexer emits Token (struct, Kind + Range)
  * Parser translates to SyntaxToken (Kind + Range, with Parent, trivia and whatnot)
* refactor: Rework syntax tree; Trivia to Tokens; Concrete SyntaxNode construction
  * SyntaxToken = SyntaxNode; removed SyntaxElement
  * Put trivia on tokens; Range not nullable!!
  * Put GarbageSyntax as trivia on tokens
  * Parser TryXX returns null, if grammar does not start here. Remove FirstSets that way
  * Remove diagnostics from events
  * construct Tree directly; remove events
  * Take slots in tree constructor