using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class ErrorSymbol(Compilation compilation, 
    SymbolName name, 
    SyntaxNode? syntax, 
    Symbol? parent = null)
    : Symbol(compilation, name, parent)
{
    public SyntaxNode? Syntax { get; } = syntax;


    public override ImmutableArray<SyntaxNode> DeclaringSyntaxes
    {
        get
        {
            if (field.IsDefault)
                field = Syntax is null ? [] : [Syntax];
            return field;
        }
    }
}