using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class LocalSymbol(
    Compilation compilation,
    SymbolName name,
    AxlType type,
    VarDeclSyntax syntax,
    Symbol? parent)
    : Symbol(compilation, name, parent)
{
    public VarDeclSyntax Syntax { get; } = syntax;
    
    public override ImmutableArray<SyntaxNode> DeclaringSyntaxes
    {
        get
        {
            if (field.IsDefault)
                field = [Syntax];
            return field;
        }
    }

    public AxlType Type { get; } = type;
}