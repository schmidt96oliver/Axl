using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class ParameterSymbol(string name, FunSymbol parent, TypeSymbol type, ParamSyntax? declarationSyntax = null) 
    : VariableSymbol(name, parent, isReadOnly: true, type)
{
    public ParamSyntax? DeclarationSyntax { get; } = declarationSyntax;
}