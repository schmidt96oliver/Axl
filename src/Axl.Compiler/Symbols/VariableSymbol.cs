using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

public class VariableSymbol(string name, FunSymbol owner, bool isReadOnly, TypeSymbol type, SyntaxNode? declarationSyntax)
    : Symbol(owner.Compilation, name, owner)
{
    public bool IsReadOnly { get; } = isReadOnly;
    public TypeSymbol Type { get; } = type;

    /// <summary>
    /// Variables are always private inside their fun body.
    /// </summary>
    public override bool IsPublic => false;

    /// <inheritdoc />
    public override bool IsConcrete => true;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax =>
        declarationSyntax is not null ? [declarationSyntax] : [];
    
    /// <inheritdoc />
    public override Symbol Substitute(TypeMap map)
        => throw new InvalidOperationException("Substitution reached a variable, which doesn't make sense.");

    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => ReferenceEquals(this,
            other); // Variable have no substituted types, so there will only be one reference.

    /// <inheritdoc />
    public override int GetHashCode()
        => RuntimeHelpers.GetHashCode(this);
}