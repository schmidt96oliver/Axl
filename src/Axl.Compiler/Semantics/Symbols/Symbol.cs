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

    public Symbol? Parent { get; } = parent;


    public abstract ImmutableArray<SyntaxNode> DeclaringSyntaxes { get; }

    
    /// <summary>
    /// Collect the diagnostics this symbol produced into <paramref name="diagnosticBag"/>.
    /// Does not collect member diagnostics.
    /// </summary>
    public virtual void CollectDiagnosticsInto(DiagnosticBag diagnosticBag)
    {
        // Default is a no-op
    }
}