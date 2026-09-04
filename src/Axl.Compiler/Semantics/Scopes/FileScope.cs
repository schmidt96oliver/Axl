using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Scopes;

public sealed class FileScope(FileSyntax fileSyntax, Scope? parent)
    : Scope(parent)
{
    public FileSyntax FileSyntax { get; } = fileSyntax;

    protected override ImmutableArray<Symbol> LookupOnThisScope(SymbolName name)
    {
        //TODO: Implement usings
        return [];
    }
}