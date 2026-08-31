using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class FnSymbol(Compilation compilation, SymbolName name, 
    FnDeclSyntax syntax, Symbol? parent)
    : Symbol(compilation, name, parent)
{
    public FnDeclSyntax Syntax { get; } = syntax;

    public override ImmutableArray<SyntaxNode> DeclaringSyntaxes
    {
        get
        {
            if (field.IsDefault)
                field = [Syntax];
            return field;
        }
    }

    public override ImmutableArray<Diagnostic> Diagnostics => [];


    public ImmutableArray<LocalSymbol> GetParameters()
    {
        throw new NotImplementedException();
    }
}