using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class ParameterSymbol(
    string name,
    FunSymbol owner,
    TypeSymbol type,
    bool isMut,
    bool isSelf,
    ParameterSymbol? originalDefinition = null,
    ParamSyntax? declarationSyntax = null)
    : LocalSymbol(name, owner)
{
    public override ParameterSymbol OriginalDefinition => originalDefinition ?? this;

    /// <inheritdoc />
    public override PlaceKind PlaceKind => IsMut ? PlaceKind.MutPlace : PlaceKind.ReadOnlyPlace;

    /// <inheritdoc />
    public override TypeSymbol Type { get; } = type;
    
    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax =>
        DeclarationSyntax is not null ? [DeclarationSyntax] : [];

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax =>
        DeclarationSyntax is not null ? [DeclarationSyntax.Name] : [];

    /// <summary>
    /// Whether assigning to or mutating through this parameter
    /// can assign to or mutate the callers place.
    /// </summary>
    public bool IsMut { get; } = isMut;

    /// <summary>
    /// Whether this is the implicit receiver parameter.
    /// </summary>
    public bool IsSelf { get; } = isSelf;

    public ParamSyntax? DeclarationSyntax { get; } = declarationSyntax;

    public override ParameterSymbol Substitute(TypeMap map) =>
        new(Name,
            owner: Owner.Substitute(map),
            Type.Substitute(map),
            IsMut,
            IsSelf,
            OriginalDefinition,
            DeclarationSyntax);

    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => other is ParameterSymbol otherParam &&
           ReferenceEquals(OriginalDefinition, otherParam.OriginalDefinition) &&
           Equals(Owner, otherParam.Owner);

    /// <inheritdoc />
    public override int GetHashCode()
        // ! OriginalDefinition might return this, so we must check it before. Nobody wants
        //   infinite loops.
        => ReferenceEquals(this, OriginalDefinition)
            ? RuntimeHelpers.GetHashCode(this)
            : HashCode.Combine(0x882F_1ABD, Owner, OriginalDefinition);

    
}