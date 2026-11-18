using System.Collections.Immutable;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public closed class FunDefinition(Compilation compilation, string name, Symbol? owner, bool isPublic)
    : Definition<FunSymbol>(compilation, name, owner, isPublic)
{
    public abstract FunDeclSyntax? DeclarationSyntax { get; }

    /// <summary>
    /// Declared parameters as locals of the body. Excluding <see cref="SelfParameter"/>.
    /// </summary>
    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }

    public abstract ParameterSymbol? SelfParameter { get; }

    public ImmutableArray<ParameterSymbol> SelfAndParameters =>
        SelfParameter is not null ? [SelfParameter, .. Parameters] : Parameters;

    public abstract TypeSymbol ReturnType { get; }

    public abstract bool IsReturnMut { get; }

    public abstract FunBody Body { get; }

    public bool IsOperator => SyntaxFacts.IsOperator(Name);

    protected override FunSymbol Create(Symbol? owner, ImmutableArray<TypeSymbol>? typeArguments)
        => new(this, owner, typeArguments);
}