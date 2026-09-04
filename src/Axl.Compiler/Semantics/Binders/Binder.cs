using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Hir;
using Axl.Compiler.Semantics.Scopes;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Binders;

/// <summary>
/// Lowers local, executable code to its HIR representation.
/// Does name resolution and type-checking together.
/// </summary>
public sealed class Binder
{
    private readonly record struct LoopContext
    {
        public static readonly LoopContext Outside = new(null);

        public List<HirBreak>? BreakExprs { get; }
        
        public bool IsInsideLoop => BreakExprs is not null;
        
        private LoopContext(List<HirBreak>? breakExprs)
        {
            BreakExprs = breakExprs;
        }

        public static LoopContext Inside()
            => new([]);
        
    
        public void AddBreak(HirBreak breakExpr)
            => BreakExprs?.Add(breakExpr);
    }

    private abstract record BindingContext
    {
        public DiagnosticBag DiagnosticBag { get; } = new();
        public ImmutableArray<FnSymbol>.Builder LocalFns { get; } = ImmutableArray.CreateBuilder<FnSymbol>();

        public Compilation Compilation
            => this switch
            {
                Script(var scriptSymbol) => scriptSymbol.Compilation,
                FnBody(var fnSymbol) => fnSymbol.Compilation,
                _ => throw new UnreachableException()
            };

        public TypeContext TypeContext
            => Compilation.TypeContext;

        public Symbol ParentSymbol => this switch
        {
            Script(var scriptSymbol) => scriptSymbol,
            FnBody(var fnSymbol) => fnSymbol,
            _ => throw new UnreachableException()
        };


        public sealed record Script(ScriptSymbol ScriptSymbol) : BindingContext;

        public sealed record FnBody(FnSymbol FnSymbol) : BindingContext;
    }
    
    // Consistent across all binders that bind one specific Hir (script or fn body).
    private readonly BindingContext _context;
    
    // Scope and loop context change among different bodies.
    private readonly LocalScope _scope;
    private readonly LoopContext _loopContext;


    private Binder(BindingContext context, LocalScope localScope, LoopContext loopContext)
    {
        _context = context;
        _scope = localScope;
        _loopContext = loopContext;
    }

    public static Hir.Hir Bind(ScriptSymbol scriptSymbol, Scope enclosingScope)
    {
        var fileSyntax = scriptSymbol.FileSyntax;

        var context = new BindingContext.Script(scriptSymbol);
        
        var localFns = CreateLocalFns(fileSyntax.Members.OfType<FnDeclSyntax>(), context);
        context.LocalFns.AddRange(localFns);
        
        // Reject all non-fn members
        foreach (var member in fileSyntax.Members)
        {
            if (member is FnDeclSyntax)
                break;
            
            context.DiagnosticBag.ReportError(new Diagnostic.UnsupportedFeature(member));
        }
        
        // Create local scope
        var localScope = new LocalScope(localFns, parent: enclosingScope);
        var binder = new Binder(context, localScope, LoopContext.Outside);
        
        // Bind statements
        var stmts = binder.BindStmts(fileSyntax.Stmts);
        
        // Return
        var body = new HirBody(stmts, armExpr: null, type: context.TypeContext.None);
        return new Hir.Hir(body, 
            context.LocalFns.DrainToImmutable(),
            context.DiagnosticBag.Drain());
    }

    private static ImmutableArray<FnSymbol> CreateLocalFns(IEnumerable<FnDeclSyntax> syntaxes, BindingContext context)
    {
        var localFns = syntaxes.Select(syntax => new FnSymbol(context.Compilation,
            SymbolName.From(syntax.Name),
            syntax,
            context.ParentSymbol)).ToImmutableArray();

        // Report duplicate declarations
        var fnGroupsByName = localFns.GroupBy(symbol => symbol.Name);
        foreach (var fnGroup in fnGroupsByName)
        {
            var fnsInGroup = fnGroup.ToImmutableArray();
            if (fnsInGroup.Length > 1)
                context.DiagnosticBag.ReportError(new Diagnostic.DuplicateFnDecls(fnsInGroup));
        }
        
        // Drain symbol diagnostics
        //TODO: This will backfire, when functions actually bind their HIR
        foreach (var fnSymbol in localFns)
            fnSymbol.CollectDiagnosticsInto(context.DiagnosticBag);

        return localFns;
    }


    private ImmutableArray<HirStmt> BindStmts(IEnumerable<StmtSyntax> syntaxes)
    {
        return [];
    }
    
    
}