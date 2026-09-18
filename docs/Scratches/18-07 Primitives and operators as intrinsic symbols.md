# Primitives as Standard Lib types and operators as intrinsic functions

*Types*:
  * Are `TypeSymbol` on Standard Library module symbol
  * Discoverable through name resolution
  * Have members

*Operators*
  * Are intrinsic functions as members on their TypeSymbol
  * Discoverered through duck-typing as search on members.
  * Name is the operator
  * Intrinsic operators: `+, -, *, /,  ==, <,  not`
  * Derived operators `!=, <=, >, >=`

*Reason*
  * Unified type-checking; no split between native types and user types
  * Frees keywords: `i32, i64, f32, f64, bool, string, unit, never`
  * Supports extensibility naturally

*Problems*
  * `Never` becomes nameable

*Implementation*
  * [ ] Add ModuleSymbol; Make StdLib a ModuleSymbol
  * [ ] Add type name resolution
  * [ ] Remove native type keywords
  * [ ] Add IntrinsicFnSymbol, Intrinsic
  * [ ] Add Operators as intrinsic fns
  * [ ] Bind BinaryExpr as BoundIntrinsicCall and lower derived operators

*Open questions*
  * [ ] Name of standard library: `Standard, System, Core`
  * [ ] Name of types: `Int32, Bool, Unit, Never`; `I32, I64, F32, F64, Bool, String, Unit, Never`