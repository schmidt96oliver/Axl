using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

public union FunBody(Intrinsic, BoundBlock);

public sealed class FunSymbol : Symbol
{
    private readonly ImmutableArray<TypeSymbol>? _typeArguments;

    internal FunSymbol(FunDefinition definition, Symbol? owner, ImmutableArray<TypeSymbol>? typeArguments)
        : base(definition.Compilation, definition.Name, owner)
    {
        Definition = definition;
        _typeArguments = typeArguments;
    }

    public FunDefinition Definition { get; }

    public bool IsIdentity => _typeArguments is null;

    /// <inheritdoc/>
    public override FunSymbol OriginalDefinition => Definition.Identity;

    public override bool IsPublic => Definition.IsPublic;

    /// <inheritdoc />
    public override bool IsConcrete
        => Owner?.IsConcrete != false && TypeArguments.All(arg => arg.IsConcrete);

    /// <inheritdoc />
    public override bool CanBeReferencedByName => Definition.CanBeReferencedByName;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => Definition.DeclaringSyntax;

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => Definition.NameSyntax;

    public bool IsOperator => SyntaxFacts.IsOperator(Name);

    public bool IsStatic => Definition.SelfParameter is null;

    public ImmutableArray<TypeParameterSymbol> TypeParameters => Definition.TypeParameters;

    /// <summary>
    /// The type arguments that fill <see cref="TypeParameters"/>. For the
    /// identity, these are the parameters themselves.
    /// </summary>
    public ImmutableArray<TypeSymbol> TypeArguments
        => _typeArguments ?? Definition.TypeParameters.CastArray<TypeSymbol>();

    private TypeMap TypeMap
    {
        get
        {
            field ??= TypeMap.For(this);
            return field;
        }
    }

    /// <summary>
    /// Declared parameters. Excluding <see cref="SelfParameter"/>.
    /// </summary>
    public ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (IsIdentity)
                return Definition.Parameters;
            if (field.IsDefault)
                field = [.. Definition.Parameters.Select(p => p.Definition.WithOwner(this))];
            return field;
        }
    }

    /// <summary>
    /// Parameter that takes the receiver in methods.
    /// </summary>
    public ParameterSymbol? SelfParameter
    {
        get
        {
            if (IsIdentity || Definition.SelfParameter is null)
                return Definition.SelfParameter;
            field ??= Definition.SelfParameter.Definition.WithOwner(this);
            return field;
        }
    }

    /// <summary>
    /// Self parameter if it exists followed by declared parameters.
    /// </summary>
    public ImmutableArray<ParameterSymbol> SelfAndParameters =>
        SelfParameter is not null ? [SelfParameter, .. Parameters] : Parameters;

    public TypeSymbol ReturnType
    {
        get
        {
            if (IsIdentity)
                return Definition.ReturnType;
            field ??= Definition.ReturnType.Substitute(TypeMap);
            return field;
        }
    }

    /// <summary>
    /// Whether the return type was declared 'mut', which means that
    /// it returns a place rather than a value.
    /// </summary>
    public bool IsReturnMut => Definition.IsReturnMut;

    public FunSymbol Specialize(ImmutableArray<TypeSymbol> typeArguments)
    {
        Guard.MustBe(TypeArguments.SequenceEqual(TypeParameters), "For specialization, the fun must still be open.");
        Guard.MustBe(typeArguments.Length == TypeParameters.Length);
        if (typeArguments.Length == 0)
            return this;

        return Definition.Specialize(Owner, typeArguments);
    }

    public override FunSymbol Substitute(TypeMap map)
        => Definition.Specialize(Owner?.Substitute(map), [.. TypeArguments.Select(arg => arg.Substitute(map))]);

    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        if (IsIdentity)
            Definition.Complete(sideEffects);
    }
}