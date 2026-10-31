using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class ParameterSymbol(string name, FunSymbol owner, TypeSymbol type, bool isMut, bool isSelf, ParamSyntax? declarationSyntax = null) 
    : VariableSymbol(name, owner, isReadOnly: !isMut, type)
{
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
}