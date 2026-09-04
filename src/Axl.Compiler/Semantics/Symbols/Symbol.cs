using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Symbols;

public abstract class Symbol(Compilation compilation, SymbolName name, Symbol? parent = null)
{
    public Compilation Compilation { get; } = compilation;

    /// <summary>
    /// Can be empty.
    /// </summary>
    public SymbolName Name { get; } = name;
    
    /// <summary>
    /// The name this symbol will be displayed in user-facing diagnostics.
    /// </summary>
    /// <example>
    /// "fn Test" or "module Global"
    /// </example>
    public abstract string DisplayName { get; }

    public Symbol? Parent { get; } = parent;


    public abstract ImmutableArray<SyntaxNode> DeclaringSyntaxes { get; }

    
    /// <summary>
    /// Collect the diagnostics this symbol or it's children produced into <paramref name="diagnosticBag"/>.
    /// </summary>
    public virtual void CollectDiagnosticsInto(DiagnosticBag diagnosticBag)
    {
        // Default is a no-op
    }
}