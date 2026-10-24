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


    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }
    
    public abstract ParameterSymbol? SelfParameter { get; }
    
    
    public ImmutableArray<TypeSymbol> ParameterTypes => [.. Parameters.Select(parameter => parameter.Type)];

    public abstract TypeSymbol ReturnType { get; }

    public abstract FunBody Body { get; }
}