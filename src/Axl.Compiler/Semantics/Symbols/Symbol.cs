using System.Collections.Immutable;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

/// <summary>
/// Declaration stuff is built eagerly. Lazily binds on request.
/// </summary>
/// <param name="Compilation"></param>
/// <param name="Name"></param>
public abstract record Symbol(Compilation Compilation, SymbolName Name, Symbol? Parent = null);

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
        // foreach (var paramSyntax in paramSyntaxes)
        // {
        //     // Bind Type
        //     var binderContext = Compilation.GetBindingContext(paramSyntax.TypeAnnotation);
        //     var boundType = Binder.BindType(paramSyntax.TypeAnnotation, binderContext);
        //     array.Add(new LocalSymbol(Compilation, SymbolName.From(paramSyntax.Name), boundType, null));
        // }

        return array.DrainToImmutable();
    }
}

public sealed record ModuleSymbol(
    Compilation Compilation,
    SymbolName Name,
    ImmutableArray<ModuleDeclSyntax> Syntaxes,
    Symbol? Parent)
    : Symbol(Compilation, Name, Parent)
{
    private ImmutableArray<Symbol> _members = default;
    
    public ImmutableArray<Symbol> GetMembers()
    {
        if (_members.IsDefault)
        {
            _members = Syntaxes
                .SelectMany(syntax => syntax.Members)
                .Select(Compilation.GetSymbolTable().GetSymbol)
                .ToHashSet()
                .ToImmutableArray();
        }

        return _members;
    }
}