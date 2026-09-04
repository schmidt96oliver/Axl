using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class ModuleSymbol(
    Compilation compilation,
    SymbolName name,
    ImmutableArray<ModuleFragment> fragments,
    Symbol? parent)
    : Symbol(compilation, name, parent)
{
    private DiagnosticBag _diagnosticBag = new();
    
    private LazyField<ImmutableArray<SyntaxNode>> _lazyDeclaringSyntaxes;
    private LazyField<ImmutableArray<Symbol>> _lazyMembers;
    
    internal ImmutableArray<ModuleFragment> Fragments { get; } = fragments;


    public override ImmutableArray<SyntaxNode> DeclaringSyntaxes
        => _lazyDeclaringSyntaxes.GetOrCreate(CreateDeclaringSyntaxes);

    public ImmutableArray<Symbol> Members
        => _lazyMembers.GetOrCreate(CreateMembers);

    
    private ImmutableArray<Symbol> CreateMembers()
    {
        var members = ImmutableArray.CreateBuilder<Symbol>();
        
        // All prefix fragments create module symbols
        var modules = Fragments
            .OfType<ModuleFragment.Prefix>()
            .Select(fragment => fragment.Child)
            .GroupBy(fragment => fragment.Name)
            .Select(fragmentsWithSameName => new ModuleSymbol(
                Compilation, 
                name: fragmentsWithSameName.Key,
                fragments: [.. fragmentsWithSameName], 
                parent: this));
        members.AddRange(modules);
        
        // All body fragments create members
        foreach (var bodyFragment in Fragments.OfType<ModuleFragment.Body>())
        foreach (var node in bodyFragment.Nodes)
        {
            switch (node)
            {
                case MemberSyntax memberSyntax:
                    members.Add(CreateSymbol(memberSyntax));
                    break;
                
                case UsingDirectiveSyntax:
                    // Usings are allowed everywhere.
                    
                    break;
                
                case ModuleDeclSyntax moduleDeclSyntax:
                    // The declaring module declaration is already filtered, so
                    // this must be another one which is invalid.
                    
                    _diagnosticBag.ReportError(new Diagnostic.MultipleModuleDecls(moduleDeclSyntax));
                    break;

                case StmtSyntax stmtSyntax:
                    // Stmts are not allowed inside module files.
                    
                    _diagnosticBag.ReportError(new Diagnostic.StmtInModuleFile(stmtSyntax));
                    break;
                
                default:
                    throw new UnreachableException();
            }
        }

        return members.DrainToImmutable();
    }

    private Symbol CreateSymbol(MemberSyntax syntax) => syntax switch
    {
        FnDeclSyntax fnDeclSyntax => new FnSymbol(Compilation, 
            SymbolName.From(fnDeclSyntax.Name),
            fnDeclSyntax, 
            parent: this),
        
        _ => throw new UnreachableException()
    };

    private ImmutableArray<SyntaxNode> CreateDeclaringSyntaxes()
        => [.. Fragments.OfType<ModuleFragment.Body>().Select(bodyFragment => bodyFragment.Syntax)];


    public override void CollectDiagnosticsInto(DiagnosticBag diagnosticBag)
    {
        // Make sure to evaluate members first.
        var members = Members;
        
        _diagnosticBag.DrainInto(diagnosticBag);
        foreach (var member in members)
            member.CollectDiagnosticsInto(diagnosticBag);
    }
}