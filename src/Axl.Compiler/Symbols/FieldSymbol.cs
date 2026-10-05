using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class FieldSymbol(string name, bool isPub, StructSymbol owner, TypeSymbol type, FieldDeclSyntax? declarationSyntax = null) : Symbol(name)
{
    public bool IsPub { get; } = isPub;
    public StructSymbol Owner { get; } = owner;

    public TypeSymbol Type { get; } = type;
    public FieldDeclSyntax? DeclarationSyntax { get; } = declarationSyntax;
}