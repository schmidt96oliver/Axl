using System.Runtime.CompilerServices;
using Axl.Compiler.Syntax.Tree;
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.

namespace Axl.Compiler.Symbols;

public sealed class ParameterSymbol(
    string name,
    FunSymbol owner,
    TypeSymbol type,
    bool isMut,
    bool isSelf,
    ParameterSymbol? originalDefinition = null,
    ParamSyntax? declarationSyntax = null)
    : VariableSymbol(name, owner, isReadOnly: !isMut, type)
{
    public override ParameterSymbol OriginalDefinition => originalDefinition ?? this;
    
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
            owner: owner.Substitute(map),
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