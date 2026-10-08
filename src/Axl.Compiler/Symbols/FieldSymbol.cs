using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class FieldSymbol(
    string name,
    StructSymbol owner,
    bool isPublic,
    TypeSymbol type,
    FieldDeclSyntax? declarationSyntax = null)
    : Symbol(name, owner)
{
    public override bool IsPublic { get; } = isPublic;

    public TypeSymbol Type { get; } = type;
    public FieldDeclSyntax? DeclarationSyntax { get; } = declarationSyntax;
}