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
        
        // Bind members
        var members = BindMembers(fileSyntax.Members.OfType<FnDeclSyntax>(), context);
        Debug.Assert(members.All(s => s is FnSymbol), "There are only fns currently.");

        var localFns = members.OfType<FnSymbol>().ToImmutableArray();
        context.LocalFns.AddRange(localFns);
        
        // Create scope and binder
        var localScope = new LocalScope(localFns, parent: enclosingScope);
        var binder = new Binder(context, localScope, LoopContext.Outside);
        
        // Bind statements
        var stmts = binder.BindStmts(fileSyntax.Stmts);
        
        // Create and return
        var body = new HirBody(stmts, armExpr: null, type: context.TypeContext.None);
        return new Hir.Hir(body, 
            context.LocalFns.DrainToImmutable(),
            context.DiagnosticBag.Drain());
    }

    
    #region Member Binding

    private static ImmutableArray<Symbol> BindMembers(IEnumerable<MemberSyntax> syntaxes, BindingContext context)
    {
        var members = ImmutableArray.CreateBuilder<Symbol>();

        foreach (var memberSyntax in syntaxes)
        {
            switch (memberSyntax)
            {
                case FnDeclSyntax fnDeclSyntax:
                    members.Add(new FnSymbol(context.Compilation,
                        SymbolName.From(fnDeclSyntax.Name),
                        fnDeclSyntax,
                        context.ParentSymbol));
                    break;

                default:
                    context.DiagnosticBag.ReportError(new Diagnostic.UnsupportedFeature(memberSyntax));
                    break;
            }
        }

        var memberArray = members.DrainToImmutable();
        ReportDuplicateMembers(memberArray, context.DiagnosticBag);
        
        return memberArray;
    }

    private static void ReportDuplicateMembers(ImmutableArray<Symbol> memberSymbols, DiagnosticBag diagnosticBag)
    {
        Debug.Assert(memberSymbols.All(s => s is FnSymbol), "There are only fns currently.");
        
        var fnsByName = memberSymbols.OfType<FnSymbol>().GroupBy(symbol => symbol.Name);
        foreach (var fnGroup in fnsByName)
        {
            var fnsInGroup = fnGroup.ToImmutableArray();
            if (fnsInGroup.Length > 1)
                diagnosticBag.ReportError(new Diagnostic.DuplicateFnDecls(fnsInGroup));
        }
    }
    
    #endregion
    
    
    private ImmutableArray<HirStmt> BindStmts(IEnumerable<StmtSyntax> syntaxes)
    {
        return [];
    }
    
    
}