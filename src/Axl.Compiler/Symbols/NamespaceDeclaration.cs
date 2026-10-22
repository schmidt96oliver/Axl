using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

public sealed record NamespaceDeclaration(
    string Name,
    List<NamespaceDeclaration> Children,
    List<SyntaxTree> Syntaxes);