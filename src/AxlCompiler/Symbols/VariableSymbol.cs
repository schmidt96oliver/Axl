using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public enum VariableKind
{
    Let,
    Var,
}

public class VariableSymbol(string name, FunSymbol owner, VariableKind kind, TypeSymbol type, VarDeclSyntax declarationSyntax)
    : LocalSymbol(name, owner)
{
    public VariableKind Kind { get; } = kind;

    /// <inheritdoc />
    public override PlaceKind PlaceKind => Kind switch
    {
        VariableKind.Let => PlaceKind.ReadOnlyPlace,
        VariableKind.Var => PlaceKind.MutPlace,
        _ => throw new UnreachableException($"Unknown {nameof(VariableKind)}.")
    };
    
    public override TypeSymbol Type { get; } = type;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [declarationSyntax];

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => [declarationSyntax.Name];
    
    /// <inheritdoc />
    public override Symbol Substitute(TypeMap map)
        => throw new InvalidOperationException("Substitution reached a variable, which doesn't make sense.");
}