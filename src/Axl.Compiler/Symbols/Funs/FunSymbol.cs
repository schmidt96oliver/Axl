using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

public union FunBody(Intrinsic, BoundBlock);

public closed class FunSymbol(Compilation compilation, string name, Symbol? owner, bool isPublic) 
    : Symbol(compilation, name, owner)
{
    public override bool IsPublic { get; } = isPublic;

    public bool IsOperator => SyntaxFacts.IsOperator(Name);

    public bool IsStatic => SelfParameter is null;

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


    /// <summary>
    /// Returns a <see cref="FunSymbol"/>, where all <see cref="TypeParameters"/> are replaced
    /// by the given <paramref name="typeArguments"/>.
    /// </summary>
    public FunSymbol ConstructWith(ImmutableArray<TypeSymbol> typeArguments)
    {
        Guard.MustBe(TypeParameters.Length == typeArguments.Length);

        // We don't need to construct, if we are given that same type
        // parameters or arguments that are already there.
        if (typeArguments.SequenceEqual(TypeParameters) ||
            typeArguments.SequenceEqual(TypeArguments))
        {
            return this;
        }
        
        return new ConstructedFunSymbol(this, typeArguments);
    }


    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => other is FunSymbol otherFun &&
           ReferenceEquals(otherFun.OriginalDefinition, OriginalDefinition) &&
           Equals(otherFun.Owner, Owner) &&
           otherFun.TypeArguments.SequenceEqual(TypeArguments);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(0x18F2_AAD1);
        hash.Add(RuntimeHelpers.GetHashCode(OriginalDefinition));
        hash.Add(Owner);
        foreach (var typeArg in TypeArguments)
            hash.Add(typeArg);
        return hash.ToHashCode();
    }
}