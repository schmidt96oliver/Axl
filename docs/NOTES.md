# ------------------------------------ Axl Project ------------------------------------
                                       ≽(◕ ᴗ ◕)≼

# Lexer
[x] Comments, Whitespace
[x] Identifier and keywords
   * fn var module public private native return if else loop break continue and or not true false i32 f32 i64 f64 bool string char none
   * never: = identifier token with contextual kind. Parser replaces it to never kind
[x] Symbols
   * . , ; : -> =>  = += -=    <= >= + - * / == !=   ( ) { } < >
[x] Errors
   * AddToken concats => one Token per error _run_
   * One diagnostic per character
[ ] Number Literals
   * 0x, 0b; suffixes; underscores;
[ ] Plain string literals
[ ] String escapes
[ ] Interpolated strings

# First features
* i32, i64, f32, f64, bool, string
* literals integral, float, char
* expressions: numeric, comparison, boolean
* variables
* blocks, if, loop (with break expression, continue), return
* none, never
* string formatting, escaped
* native functions: Print, PrintLine, ToString
* hoisted, overloaded functions
* multi-file modules

# Implementation Requirements
1. Testing
   * validate diagnostic code & token kinds
   * accept mode
   * multi-file test cases

3. Parser
   * "=>" syntax according to ExpressionsAndStatements.axl
   * "<>" generics disambiguation
   * Handle contextual keyword never
4. Binder
   * expected type propagation: number literals
   * loop break type checks
   * divergence tracking, definite return
   * never type (see Never.axl)
   * native fn validation
5. Lowering

# Implementation Ideas
Doc comments:
   * "<c>" = Code, single line
   * "<code>" = Code, multi line
   * "<example>" "<exception>"

1. Lexer -> TokenList
   * ref struct Scanner: .Advance, .AdvanceWhile, .MakeToken carries ReadOnlySpan<char> Text
2. Parser -> SyntaxTree
   * ?? untyped tree or typed
3. SymbolTable (DeclarationTable?)
   * builds DeclId (see Project.md)
   * builds symbols as second step (i.e. binds signatures)
   * ?? lazy symbols
   * maps Name to DeclarationSyntax
   * ?? how find all extend members
   * ?? how to represent symbols (lazy objects, just a ref?)
   * -> Type = NeverType |  NonNeverType (SoundType) to keep them apart structurally
4. Binding -> Hir; queried by SemanticModel (answers questions. Always answers the same.)
   * ?? Binding per declaration => ModuleBinder, RecordBinder, FunctionBinder, ... They dont go into declarations themselves. Creates by SemanticModel
   * SemanticModel needs to protect against cyclic refs
   * Needs to keep track of divergence
5. Lowering -> Mir
6. CodeGen -> ByteCode
7. VirtualMachine

# Mir Claude sketch

## TLDR

Three examples below. The pattern that emerges: **`call.trait` → `call` is the only opcode that substitution rewrites.** Everything else changes only its type annotation, and the CFG is byte-for-byte identical across instantiations. That's what makes `Substitute` ~40 lines and what makes the analysis-once claim a theorem rather than a hope.

## Example 1 — the general case

```
fn Contains<T: Equatable>(xs: [T], needle: T) -> bool
    => loop {
        if i >= xs.Length => break false;
        if xs[i] == needle => break true;
        i += 1;
    };
```

**Generic MIR** (what you analyze, once):

```
fn Contains<T: Equatable>(%0: [T], %1: T) -> bool
  locals: %2: i32   // i
          %3: bool  // loop value
          %4: i32, %5: bool, %6: T, %7: bool   // temps

bb0:
  %2 = const 0_i32
  goto bb1

bb1:                                        // loop head
  %4 = array.len %0
  %5 = i32.ge %2, %4
  branch %5 bb2 bb3

bb2:
  %3 = const false
  goto bb6                                  // break false

bb3:
  %6 = array.get %0, %2 : T                 // ← type annotation
  %7 = call.trait Equatable::Equals<T>(%6, %1)   // ← polymorphic call
  branch %7 bb4 bb5

bb4:
  %3 = const true
  goto bb6                                  // break true

bb5:
  %2 = i32.add %2, const 1_i32
  goto bb1

bb6:
  return %3
```

**After `Substitute(T → i32)`:**

```
fn Contains$i32(%0: [i32], %1: i32) -> bool
  locals: %2: i32, %3: bool, %4: i32, %5: bool, %6: i32, %7: bool
  ...
bb3:
  %6 = array.get %0, %2 : i32
  %7 = call I32Equals(%6, %1)               // devirtualized
  branch %7 bb4 bb5
  ...
```

**After `Substitute(T → Vec2)`** (a `value record`, structural equality generated):

```
fn Contains$Vec2(%0: [Vec2], %1: Vec2) -> bool
  ...
bb3:
  %6 = array.get %0, %2 : Vec2
  %7 = call Vec2$Equals(%6, %1)             // compiler-generated
  branch %7 bb4 bb5
  ...
```

Two lines differ between instantiations. `bb0`, `bb1`, `bb2`, `bb4`, `bb5`, `bb6` are identical in all three — including `i32.add`, `array.len`, and every terminator, because index arithmetic and lengths were never polymorphic to begin with.

## Example 2 — assignment, and a design choice it forces

```
fn Duplicate<T>(x: T) -> T { var y = x; => y }
```

```
fn Duplicate<T>(%0: T) -> T
  locals: %1: T
bb0:
  %1 = copy %0 : T
  return %1
```

```
Substitute(T → Person)  →  %1 = copy %0 : Person   // reference: alias, one word
Substitute(T → Vec2)    →  %1 = copy %0 : Vec2     // value: clone fields
```

**Same opcode.** The type annotation carries the semantics, and the interpreter (or later, codegen) reads it.

This is a real fork, so make it deliberately:

| approach | generic MIR possible? |
|---|---|
| one `copy` op, semantics from the type | yes — substitution is a pure type-fold |
| distinct `alias` / `clone` opcodes | no — lowering can't choose while `T` is opaque |

Keep instructions abstract and let types carry meaning, and substitution never rewrites opcodes except `call.trait`. Emit distinct opcodes at lowering and you've forced mono-at-lowering by accident. It's the same principle as MIR being register-based rather than committing to a stack layout early.

## Example 3 — a trait call with a return value

Ties into your string formatting:

```
fn Show<T: Stringable>(x: T) => PrintLine("Value: {x}");
```

```
fn Show<T: Stringable>(%0: T) -> none
  locals: %1: string, %2: string, %3: none
bb0:
  %1 = call.trait Stringable::ToString<T>(%0)
  %2 = string.concat const "Value: ", %1
  %3 = call.native PrintLine(%2)
  return %3
```

```
Substitute(T → i32)  →  %1 = call.native I32ToString(%0)
Substitute(T → f32)  →  %1 = call.native F32ToString(%0)
```

Note `%1: string` is known **pre-mono** — the return type comes from the trait declaration. That's why divergence analysis stays correct on generic MIR: a `-> never` trait method is visibly divergent before you know what `T` is.

## The substitution pass

```csharp
MirBody Substitute(MirBody body, Substitution s) => body with {
    Locals = body.Locals.Select(l => l with { Type = Subst(l.Type, s) }),
    Blocks = body.Blocks.Select(b => b with {
        Instructions = b.Instructions.Select(i => SubstInstr(i, s))
    })
    // terminators: untouched. Blocks, edges, order — all identical.
};

Instruction SubstInstr(Instruction i, Substitution s) => i switch {
    TraitCall tc => new Call(ResolveImpl(tc.Trait, tc.Method, Subst(tc.Self, s)),
                             tc.Args, Subst(tc.ResultType, s)),
    _            => i with { Type = Subst(i.Type, s) }
};
```

`ResolveImpl` is the only interesting line, and it's a lookup in the impl table you need anyway for non-generic calls.

## What the analyses see

Definite assignment on `Contains<T>` reads:

```
bb3:  %6 ← def,  %0 %2 ← use
      %7 ← def,  %6 %1 ← use
```

No types consulted. Identical for every `T`. Same for unreachable, dead stores, and your capture check.

Which is the payoff worth naming: **the generic MIR *is* the analysis MIR.** You don't produce a throwaway body with a fake substitution — you analyze the real thing, once, and every instantiation is derived from it by a function that provably preserves the CFG. The invariant I asked you to write down last time stops being an assumption.

## Two small things

**Register types are declared in `locals`, so the `: T` on `array.get` is redundant.** Keep it anyway — it makes MIR dumps readable without cross-referencing, and dumps are your primary debugging surface. Same reasoning as spans on every instruction.

**Name mangling.** `Contains$i32`, `Contains$Vec2` — pick a scheme now, since it's what your instantiation cache keys on and what shows up in panic stack traces. Nested generics (`Contains$[i32]`) need a bracket or arity convention; anything unambiguous is fine.