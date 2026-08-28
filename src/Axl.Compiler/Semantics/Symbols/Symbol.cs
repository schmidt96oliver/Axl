using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using Axl.Compiler.Semantics.Binders;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

/// <summary>
/// Represents a declaration and executes lazy binding.
/// </summary>
/// <param name="Compilation"></param>
/// <param name="Name"></param>
public abstract record Symbol(Compilation Compilation, SymbolName Name, Symbol? Parent = null)
{
    private SymbolPath? _lazyPath;
    public SymbolPath Path
    {
        get
        {
            _lazyPath ??= Parent is null
                ? SymbolPath.From(Name)
                : SymbolPath.Combine(Parent.Path, Name);

            return _lazyPath.Value;
        }
    }
}

/// <summary>
/// Eagerly built during local body binding by a LocalBinder.
/// </summary>
public sealed record LocalSymbol(Compilation Compilation, SymbolName Name, 
    AxlType Type, VarDeclSyntax Syntax,
    Symbol? Parent) 
    : Symbol(Compilation, Name, Parent);

public sealed record FnSymbol(Compilation Compilation, SymbolName Name, 
    FnDeclSyntax Syntax, Symbol? Parent)
    : Symbol(Compilation, Name, Parent)
{
    public ImmutableArray<LocalSymbol> GetParameters()
    {
        var paramSyntaxes = Syntax.Parameters.ToList();
        
        var array = ImmutableArray.CreateBuilder<LocalSymbol>(initialCapacity: paramSyntaxes.Count);
        foreach (var paramSyntax in paramSyntaxes)
        {
            // Bind Type
            var binder = Compilation.GetBinderFactory().GetBinderAt(Syntax);
            var boundType = binder.BindType(paramSyntax.TypeAnnotation!);
            array.Add(new LocalSymbol(Compilation, SymbolName.From(paramSyntax.Name), boundType, null, this));
        }

        return array.DrainToImmutable();
    }

    public HirBody GetHir()
    {
        var parentBinder = Compilation.GetBinderFactory().GetBinderAt(Syntax);
        var fnBinder = new FnBinder(parentBinder, this);

        return fnBinder.BindBody(Syntax.Body);
    }
}

public sealed record ModuleSymbol(
    Compilation Compilation,
    SymbolName Name,
    ImmutableArray<ModuleDeclSyntax> Syntaxes,
    Symbol? Parent)
    : Symbol(Compilation, Name, Parent)
{
    /// <summary>
    /// Set eagerly during construction, because modules might not contain
    /// syntax.
    /// </summary>
    /// <example>
    /// `module A.B.C` has module A and B without syntax.
    /// </example>
    internal ImmutableArray<ModuleSymbol> ModuleMembers
    {
        get
        {
            Debug.Assert(!field.IsDefault, "Not set during construction.");
            return field; 
        }
        set
        {
            Debug.Assert(field.IsDefault, "Set multiple times during construction.");
            field = value;
        }
    }

    private ImmutableArray<Symbol> _members = default;
    
    public ImmutableArray<Symbol> GetMembers()
    {
        if (_members.IsDefault)
        {
            var nonModuleMembers = Syntaxes
                .SelectMany(syntax => syntax.Members)
                .Where(syntax => syntax is not ModuleDeclSyntax)
                .Select(Compilation.GetSymbolTable().GetSymbol)
                .Where(symbol => symbol.Parent == this);

            _members = [.. ModuleMembers, .. nonModuleMembers];
        }

        return _members;
    }
}