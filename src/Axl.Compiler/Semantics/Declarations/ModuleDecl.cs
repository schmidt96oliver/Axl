using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Declarations;

/// <summary>
/// A fully merged module declaration composed of many <see cref="ModuleDeclFragment"/>s.
/// </summary>
public sealed class ModuleDecl(SymbolName name, ImmutableArray<ModuleDeclFragment> fragments)
{
    private LazyField<ImmutableArray<ModuleDecl>> _lazyChildModules;
    private LazyField<ImmutableArray<BaseModuleDeclSyntax>> _lazySyntaxes;
    
    /// <summary>
    /// Empty, if this is the global module.
    /// </summary>
    public SymbolName Name { get; } = name;

    public ImmutableArray<ModuleDecl> ChildModules
        => _lazyChildModules.GetOrCreate(MergeChildModules);

    /// <summary>
    /// Empty, if this is the global module.
    /// </summary>
    public ImmutableArray<BaseModuleDeclSyntax> Syntaxes
        => _lazySyntaxes.GetOrCreate(MergeSyntaxes); 


    private ImmutableArray<ModuleDecl> MergeChildModules()
        =>
        [
            .. fragments
                .SelectMany(singleDecl => singleDecl.ChildFragments)
                .GroupBy(singleDecl => singleDecl.Name)
                .Select(grouping =>
                    new ModuleDecl(grouping.Key, [.. grouping]))
        ];
    
    private ImmutableArray<BaseModuleDeclSyntax> MergeSyntaxes()
        => 
        [
            .. fragments
                .Select(fragment => fragment.Syntax)
                .Where(syntax => syntax is not null)!
        ];
    
    
    public void CollectDiagnosticsInto(DiagnosticBag diagnosticBag)
    {
        foreach (var fragment in fragments)
            diagnosticBag.AddRange(fragment.Diagnostics);
    }
}