using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

public union FunBody(Intrinsic, BoundBlock);

public closed class FunSymbol(Compilation compilation, string name, Symbol? owner, bool isPublic) 
    : Symbol(compilation, name, owner)
{
    private readonly List<ConstructedFunSymbol> _constructedFunSymbols = [];
    
    
    public override bool IsPublic { get; } = isPublic;

    public bool IsOperator => SyntaxFacts.IsOperator(Name);

    public bool IsStatic => SelfParameter is null;

    /// <summary>
    /// Whether this fun has all type arguments that fill its type parameters.
    /// </summary>
    public bool IsConstructed => ConstructedFrom is not null;

    /// <summary>
    /// Whether this fun has type parameters.
    /// </summary>
    public bool IsGeneric => TypeParameters.Length > 0;

    /// <summary>
    /// The <see cref="FunDefinitionSymbol"/> that originally declared this fun in
    /// source text without any type parameter substitutions.
    /// </summary>
    public abstract FunDefinitionSymbol OriginalDefinition { get; }

    /// <summary>
    /// The <see cref="FunSymbol"/> this fun was constructed from by substituting
    /// its own type parameters. <c>null</c>, if this fun is still generic.
    /// </summary>
    public abstract FunSymbol? ConstructedFrom { get; }


    public abstract ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

    public abstract ImmutableArray<TypeSymbol> TypeArguments { get; }

    /// <summary>
    /// Declared parameters. Excluding <see cref="SelfParameter"/>.
    /// </summary>
    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }
    
    /// <summary>
    /// Parameter that takes the receiver in methods.
    /// </summary>
    public abstract ParameterSymbol? SelfParameter { get; }

    /// <summary>
    /// Self parameter if it exists followed by declared parameters.
    /// </summary>
    public ImmutableArray<ParameterSymbol> SelfAndParameters =>
        SelfParameter is not null ? [SelfParameter, .. Parameters] : Parameters;
    
    public abstract TypeSymbol ReturnType { get; }

    /// <summary>
    /// Whether the return type was declared 'mut', which means that
    /// it returns a place rather than a value.
    /// </summary>
    public abstract bool IsReturnMut { get; }


    public FunSymbol GetConstructedFun(ImmutableArray<TypeSymbol> typeArguments)
    {
        Guard.IsState(!IsConstructed);
        Guard.MustBe(TypeParameters.Length == typeArguments.Length);
        
        if (_constructedFunSymbols.FirstOrDefault(s => s.TypeArguments.SequenceEqual(typeArguments))
            is { } constructed)
        {
            Debug.Assert(constructed.ConstructedFrom == this);
            return constructed;
        }

        constructed = new ConstructedFunSymbol(this, typeArguments);
        _constructedFunSymbols.Add(constructed);
        return constructed;
    }
}