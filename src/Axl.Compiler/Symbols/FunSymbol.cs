using System.Collections.Immutable;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public union FunBody(Intrinsic, BoundBlock);

public closed class FunSymbol(string name, Symbol? owner, bool isPublic) : Symbol(name, owner)
{
    public override bool IsPublic { get; } = isPublic;


    public bool IsOperator => SyntaxFacts.IsOperator(Name);

    public bool IsStatic => SelfParameter is null;

    public abstract FunDeclSyntax? DeclarationSyntax { get; }


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
    
    
    public ImmutableArray<TypeSymbol> ParameterTypes => [.. Parameters.Select(parameter => parameter.Type)];

    public abstract TypeSymbol ReturnType { get; }

    /// <summary>
    /// Whether the return type was declared 'mut', which means that
    /// it returns a place rather than a value.
    /// </summary>
    public abstract bool IsReturnMut { get; }


    public abstract FunBody Body { get; }
}