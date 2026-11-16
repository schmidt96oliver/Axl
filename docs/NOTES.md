# Axl Project
                                       ≽(◕ ᴗ ◕)≼                                      

# Now

* constants in types: For I32.Min/Max, ...

* ?? Move TestFile Evalutor into test suite

# Questions / Little ones

* ?? BindLookupResult: Report WrongKind error before anything else; remove isThisField in BindFieldAccess

* ?? Miri: `Environment` -> `ActivationFrame` with .GetRef that throws if exited
* ?? Miri: Store reference as Root (ActivationFrame, LocalId) + Path (FieldSymbol[]). See Claude, think why?

* ?? LayoutGraphNode to TypeDefinitionSymbol

* Mir: Validator
  * Phase.Monomorphized: Check that everything is concrete. No TypeParams
  * Call with mut return: ref is directly used and never again

* Mir: StructInit as ImmutableArray<Operand> ordered from declaration/member order. No map

* Binder: BindLookupResult should report WrongKind error before binding

* ?? Fun: TokenSet iteration with `i & -i` trick

* Separate IfStmt and IfExpr in syntax (-> SemanticModel.BoundNodeFor tests)


# Milestones

**Buffer**: `Buffer[T]`, `List[T]/MutList[T]`

**Classes, ByRef types**: Class, `===` operator
* RecursiveStructLayout: Only check type args/fields that store by value

**Mir Analysis**
* Unreachable code analysis
    * Build HashSet for reachable StmtSyntax by walk on MIR. Then walk over syntax tree to report.
* Read/Write analysis
    * Unused variables
    * 'var' can be 'let'
    * unecessary parameter 'mut' + self
    * unecessary assignment

**Enums/Exhaustiveness/Patterns**

**Traits/Constraints**

# Loose Ends
* Overload resolution specificity
   `fun A[T](t: T){} fun A[T](t: I32){}   A[I32](1);`
* Operators and generics: `pub fun +[T](t: T)` how do they interact?
* Mut exclusivity check
* RecursiveStructLayout: Only check type args/fields that store by value

* SemanticModel and Scopes:
  * Record (Scope, LookupOptions) from Binder and removed Completion Probe

# Later

* Miri flat execution + stack trace + @StackTraceHidden

* Mir Inliner
  * Measure performance for number heavy code before and after!
  * `@Inline`, `@NoInline` + small size threshold

# Ideas
[Ideas](IDEAS.md)

* Axl: Named arguments
* Axl: Default fun arguments
* Axl: Default struct field values
* Axl: `static let` constants

* Axl: Nested structs
  * Enclosing types instance members rejected (MemberOfEnclosingType)
* Axl: `@derive(==)` and `@derive(ToString)`

* Axl: Drop initializers, definite assignment, maybe `let a = undefined;`

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