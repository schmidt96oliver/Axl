using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class ParameterSymbol(string name, TypeSymbol type, ParamSyntax? declarationSyntax = null) 
    : VariableSymbol(name, isReadOnly: true, type)
{
    public ParamSyntax? DeclarationSyntax { get; } = declarationSyntax;
    public override SymbolKind Kind => SymbolKind.Parameter;
}