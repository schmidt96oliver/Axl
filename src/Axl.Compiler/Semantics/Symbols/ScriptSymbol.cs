using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Binders;
using Axl.Compiler.Semantics.Scopes;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class ScriptSymbol(Compilation compilation, FileSyntax fileSyntax)
    : Symbol(compilation, SymbolName.Empty, parent: null)
{
    private LazyField<Hir.Hir> _lazyHir;
    
    
    private readonly DiagnosticBag _diagnosticBag = new();

    public FileSyntax FileSyntax { get; } = fileSyntax;
    
    /// <summary>
    /// Scripts have no declaration syntax.
    /// </summary>
    public override ImmutableArray<SyntaxNode> DeclaringSyntaxes
        => [];

    public Hir.Hir Hir
        => _lazyHir.GetOrCreate(BindScript);


    private Hir.Hir BindScript()
    {
        var globalScope = new GlobalScope(Compilation.GlobalSymbol);
        var fileScope = new FileScope(FileSyntax, parent: globalScope);

        return Binder.Bind(this, enclosingScope: fileScope);
    }
    
    public override void CollectDiagnosticsInto(DiagnosticBag diagnosticBag)
    {
        foreach (var node in FileSyntax.SyntaxNodes().OfType<ModuleDeclSyntax>())
            _diagnosticBag.ReportError(new Diagnostic.ModuleDeclAfterCode(node));
        
        _diagnosticBag.DrainInto(diagnosticBag);
        
        diagnosticBag.AddRange(Hir.Diagnostics);
    }
}