using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

/// <summary>
/// A parameter of a fun. The identity is the local used inside the
/// definition's body. Specialized funs have specialized parameters
/// with substituted types.
/// </summary>
public sealed class ParameterSymbol : LocalSymbol
{
    private readonly bool _isIdentity;

    internal ParameterSymbol(ParameterDefinition definition, FunSymbol owner, bool isIdentity)
        : base(definition.Name, owner)
    {
        Definition = definition;
        _isIdentity = isIdentity;
    }

    public ParameterDefinition Definition { get; }

    public override ParameterSymbol OriginalDefinition => Definition.Identity;

    /// <inheritdoc />
    public override PlaceKind PlaceKind => IsMut ? PlaceKind.MutPlace : PlaceKind.ReadOnlyPlace;

    /// <inheritdoc />
    public override TypeSymbol Type
    {
        get
        {
            if (_isIdentity)
                return Definition.Type;
            field ??= Definition.Type.Substitute(TypeMap.For(this));
            return field;
        }
    }

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => Definition.DeclaringSyntax;

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => Definition.NameSyntax;

    /// <summary>
    /// Whether assigning to or mutating through this parameter
    /// can assign to or mutate the callers place.
    /// </summary>
    public bool IsMut => Definition.IsMut;

    /// <summary>
    /// Whether this is the implicit receiver parameter.
    /// </summary>
    public bool IsSelf => Definition.IsSelf;

    public ParamSyntax? DeclarationSyntax => Definition.DeclarationSyntax;

    public override ParameterSymbol Substitute(TypeMap map)
        => Definition.Specialize(Owner.Substitute(map), []);
}