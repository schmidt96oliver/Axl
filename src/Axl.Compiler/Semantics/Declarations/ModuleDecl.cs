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
    /// <summary>
    /// Empty, if this is the global module.
    /// </summary>
    public SymbolName Name { get; } = name;

    public bool IsGlobal => Name.IsEmpty;
    
    
    public ImmutableArray<ModuleDecl> ChildModules
    {
        get
        {
            if (field.IsDefault)
            {
                field =
                [
                    .. fragments
                        .SelectMany(singleDecl => singleDecl.ChildFragments)
                        .GroupBy(singleDecl => singleDecl.Name)
                        .Select(grouping =>
                            new ModuleDecl(grouping.Key, [.. grouping]))
                ];
            }
            
            return field;
        }
    }

    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (field.IsDefault)
                field = [.. fragments.SelectMany(decl => decl.Diagnostics)];
            return field;
        }
    }

    /// <summary>
    /// Empty, if this is the global module.
    /// </summary>
    public ImmutableArray<BaseModuleDeclSyntax> Syntaxes
    {
        get
        {
            if (field.IsDefault)
            {
                field =
                [
                    .. fragments
                        .Select(fragment => fragment.Syntax)
                        .Where(syntax => syntax is not null)!
                ];
            }

            return field;
        }
    }
}