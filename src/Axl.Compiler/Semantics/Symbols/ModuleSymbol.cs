using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Declarations;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class ModuleSymbol(
    Compilation compilation,
    ModuleDecl decl,
    Symbol? parent)
    : Symbol(compilation, decl.Name, parent)
{
    public ModuleDecl Decl { get; } = decl;

    public override ImmutableArray<SyntaxNode> DeclaringSyntaxes
    {
        get
        {
            if (field.IsDefault)
                field = Decl.Syntaxes.CastArray<SyntaxNode>();
            return field;
        }
    }

    /// <inheritdoc />
    public override ImmutableArray<Diagnostic> Diagnostics => Decl.Diagnostics;


    public ImmutableArray<Symbol> Members
    {
        get
        {
            if (field.IsDefault)
                field = MakeMembers();
            return field;
        }
    }

    public ImmutableArray<Symbol> MakeMembers()
    {
        // Create Module Symbols
        var moduleMembers =
            Decl.ChildModules.Select(mergedDecl => new ModuleSymbol(Compilation, mergedDecl, parent: this));
        var otherMembers = Decl.Syntaxes
            .SelectMany(syntax => syntax.Members)
            .Where(syntax => syntax is not BaseModuleDeclSyntax)
            .Select(MakeSymbol);

        return [.. moduleMembers, .. otherMembers];
    }

    private Symbol MakeSymbol(MemberSyntax syntax) => syntax switch
    {
        FnDeclSyntax fnDeclSyntax => new FnSymbol(Compilation, 
            SymbolName.From(fnDeclSyntax.Name),
            fnDeclSyntax, 
            parent: this),
        _ => throw new UnreachableException()
    };
}