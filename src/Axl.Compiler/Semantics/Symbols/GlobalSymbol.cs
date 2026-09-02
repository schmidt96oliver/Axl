using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class GlobalSymbol(Compilation compilation,
    ImmutableArray<ModuleFragment> moduleFragments) 
    : Symbol(compilation, SymbolName.Empty, parent: null)
{
    private readonly DiagnosticBag _diagnosticBag = new();
    
    private LazyField<ImmutableArray<Symbol>> _lazyMembers;

    
    public ImmutableArray<Symbol> Members
        => _lazyMembers.GetOrCreate(CreateMembers);
    
    /// <summary>
    /// Global is compiler-generated and has no declaring syntax.
    /// </summary>
    public override ImmutableArray<SyntaxNode> DeclaringSyntaxes { get; } = [];

    
    private ImmutableArray<Symbol> CreateMembers()
        =>
        [
            .. moduleFragments
                .GroupBy(fragment => fragment.Name)
                .Select(fragmentsWithSameName => new ModuleSymbol(
                    Compilation,
                    name: fragmentsWithSameName.Key,
                    fragments: [.. fragmentsWithSameName],
                    parent: this))
        ];
    
    public override void CollectDiagnosticsInto(DiagnosticBag diagnosticBag)
    {
        // Make sure to evaluate members first.
        var members = Members;
        
        _diagnosticBag.DrainInto(diagnosticBag);
        foreach (var member in members)
            member.CollectDiagnosticsInto(diagnosticBag);
    }
}