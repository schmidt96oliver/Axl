using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class ParameterSymbol(string name, FunSymbol owner, TypeSymbol type, ParamSyntax? declarationSyntax = null) 
    : VariableSymbol(name, owner, isReadOnly: true, type)
{
    public ParamSyntax? DeclarationSyntax { get; } = declarationSyntax;
}