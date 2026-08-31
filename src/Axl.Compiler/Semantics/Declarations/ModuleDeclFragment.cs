using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Declarations;

/// <summary>
/// Part of a module declaration declared by a single <see cref="BaseModuleDeclSyntax"/>
/// or by the compiler.
/// </summary>
/// <param name="Name">Empty, if this fragment represents the entire file.</param>
/// <param name="Syntax">
/// <c>null</c> if compiler-generated declaration, i.e. those
/// fragments that represent the entire file or left parts of
/// paths. E.g. in `module A.B.C;`, A and B are compiler generated.
/// </param>
public sealed record ModuleDeclFragment(
    SymbolName Name,
    BaseModuleDeclSyntax? Syntax,
    ImmutableArray<ModuleDeclFragment> ChildFragments,
    ImmutableArray<Diagnostic> Diagnostics);