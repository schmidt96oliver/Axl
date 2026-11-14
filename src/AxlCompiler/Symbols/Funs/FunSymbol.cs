using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

public union FunBody(Intrinsic, BoundBlock);

public closed class FunSymbol(Compilation compilation, string name, Symbol? owner, bool isPublic) 
    : Symbol(compilation, name, owner)
{
    public override bool IsPublic { get; } = isPublic;

    /// <inheritdoc />
    public override bool IsConcrete 
        => Owner?.IsConcrete != false && TypeArguments.All(arg => arg.IsConcrete);

    /// <inheritdoc/>
    public abstract override FunDefinitionSymbol OriginalDefinition { get; }
    
    public bool IsOperator => SyntaxFacts.IsOperator(Name);

    public bool IsStatic => SelfParameter is null;
    

    public abstract ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

    /// <summary>
    /// The type arguments that fill <see cref="TypeParameters"/>. In their
    /// initial state, <see cref="TypeArguments"/> is filled with the parameters
    /// themselves. Might be specialized.
    /// </summary>
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

    /// <inheritdoc />
    protected virtual IEnumerable<Symbol> MembersForTraversal()
        => [.. TypeParameters, .. Parameters, .. Members];
    
    public FunSymbol Specialize(ImmutableArray<TypeSymbol> typeArguments)
    {
        Guard.MustBe(TypeArguments.SequenceEqual(TypeParameters), "For specialization, the fun must still be open.");
        Guard.MustBe(typeArguments.Length == TypeParameters.Length);
        if (typeArguments.Length == 0)
            return this;
        
        return OriginalDefinition.Specialize(Owner, typeArguments);
    }

    public override FunSymbol Substitute(TypeMap map)
    {
        var specializedOwner = Owner?.Substitute(map);
        var specializedArgs = TypeArguments.Select(arg => arg.Substitute(map)).ToImmutableArray();
        return OriginalDefinition.Specialize(specializedOwner, specializedArgs);
    }
}