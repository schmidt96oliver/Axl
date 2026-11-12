# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼                                      



# Now

* RefSymbol: 
  * Arg names: BindFieldInitializer in CheckTypeCandidate; BoundStructInitError carries TypeCandidate, which has
               (BoundFieldInitializers, UnmatchedArguments, UninitializedFields)
    * Do it after type argument inference

* How to walk symbol tree for syntax node?
  * Clarify DeclaringSyntaxes
  * Maybe add NameSyntax already

* Bind per fun/script body
  * Cache body -> BoundBody
  * exhaustive switch on BoundBodyFor(Symbol) (might add exprs later or something)
    * Also, fun offer the body anyway already :).
  * map Syntax -> *one* BoundNode (pick most current from side effects)
  * Should resolvers bind concrete nodes. ColorColor cases?

* move semantic tokens to ReferencedSymbolFor/DeclaredSymbolFor

* Add `SemanticModel.DeclaredSymbolFor(IdentifierToken)`
  * Symbol tree walk (for struct, fun, field, type parameter)
  * parameter: Inside fun
  * let/var: Record in BindStmt, bind the body
  * namespace: walk name parts


* Remove `AddResolvedSymbols` (run differential test first)
  
* Scope retrieval
  * They needs their scope children
  * Scopes store valid range
  * BoundBlock stores scopes
  * Position lookup is just a walk

* Completion:
    * Insert []
    * Insert ()

* [ ] GoTo Def (ReferencedSymbolFor + NameSyntax)
* [ ] Rename (about the same)
* [ ] Document Highlight
  * FindReferences(SourceLocation? scope) =
    (a) narrow search scope (block for local, fun for param, ...)
    (b) search for identifier tokens with that name
    (c) check against ReferencedSymbolFor

* [ ] SignatureHelp (through BoundCallError)

# Questions / Little ones

* ?? Miri: `Environment` -> `ActivationFrame` with .GetRef that throws if exited
* ?? Miri: Store reference as Root (ActivationFrame, LocalId) + Path (FieldSymbol[]). See Claude, think why?

* ?? Binder: BoundParameter and BoundSelf again?

* ?? SyntaxLocations.CalleeOf, CalleeAndTypeArgs, CalleeAndArgs, ...
  * Pass CallSyntax and similar to Diagnostics. They find out where to put it.

* ?? LayoutGraphNode to TypeDefinitionSymbol

* Mir: Validator
  * Phase.Monomorphized: Check that everything is concrete. No TypeParams
  * Call with mut return: ref is directly used and never again

* Mir: StructInit as ImmutableArray<Operand> ordered from declaration/member order. No map

# Milestones

**Panic**: `1/0` panics, return code, taxl panic, Assert tests

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

# Later

* ?? Drop initializers + definite assignment
* Miri flat execution + stack trace + @StackTraceHidden
* LSP Debug through Code Lens (show MIR through external file), Code Actions, Document Symbols (Syntax tree, Bound tree, Symbol tree)

# Ideas
[Ideas](IDEAS.md)

* Axl: Named arguments
* Axl: Default fun arguments
* Axl: Default struct field values
* Axl: `static let` constants

* Axl: Nested structs
  * Enclosing types instance members rejected (MemberOfEnclosingType)
  * Nested type through instance rejected (or like ColorColor)
  * Private nested type leaking through public fun rejected
* Axl: `@derive(==)` and `@derive(ToString)`

* LSP: Completion Context (SemanticModel and ad-hoc binding)

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