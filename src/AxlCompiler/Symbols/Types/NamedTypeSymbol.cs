using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

/// <summary>
/// A type declared by a <see cref="TypeDefinition"/>. Either the identity
/// (type arguments are its own type parameters) or a specialization.
/// Only created by <see cref="Definition{TSymbol}.Specialize"/>, so reference
/// equality is enough.
/// </summary>
public sealed class NamedTypeSymbol : TypeSymbol
{
    private readonly ImmutableArray<TypeSymbol>? _typeArguments;

    internal NamedTypeSymbol(TypeDefinition definition, Symbol? owner, ImmutableArray<TypeSymbol>? typeArguments)
        : base(definition.Compilation, definition.Name, owner, definition.IsPublic, definition.PrimitiveKind)
    {
        Definition = definition;
        _typeArguments = typeArguments;
    }

    public TypeDefinition Definition { get; }

    public bool IsIdentity => _typeArguments is null;

    public override NamedTypeSymbol OriginalDefinition => Definition.Identity;

    public override bool IsValueType => Definition.IsValueType;
    public override bool IsReferenceType => Definition.IsReferenceType;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => Definition.CanBeReferencedByName;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => Definition.DeclaringSyntax;

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => Definition.NameSyntax;

    /// <inheritdoc />
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => Definition.TypeParameters;

    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments
        => _typeArguments ?? Definition.TypeParameters.CastArray<TypeSymbol>();

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members
    {
        get
        {
            if (IsIdentity)
                return Definition.Members;

            if (field.IsDefault)
            {
                var typeMap = TypeMap.For(this);
                field = [.. Definition.Members.Select(m => m.Substitute(typeMap))];
            }

            return field;
        }
    }

    /// <inheritdoc />
    public override TypeSymbol Substitute(TypeMap map)
        => Definition.Specialize(Owner?.Substitute(map), [.. TypeArguments.Select(arg => arg.Substitute(map))]);

    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        if (IsIdentity)
            Definition.Complete(sideEffects);
    }
}