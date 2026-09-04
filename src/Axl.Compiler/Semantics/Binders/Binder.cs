using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Hir;
using Axl.Compiler.Semantics.Scopes;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;
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
    
    
    #region Types

    private AxlType BindType(TypeNameSyntax syntax) => syntax switch
    {
        NativeTypeNameSyntax nativeTypeNameSyntax => BindNativeType(nativeTypeNameSyntax),
        PathSyntax pathSyntax => BindPathType(pathSyntax),
        _ => throw new UnreachableException($"Unknown {nameof(TypeNameSyntax)}")
    };

    private AxlType BindNativeType(NativeTypeNameSyntax syntax) => syntax.Token.Kind switch
    {
        TokenKind.I32Kw => _context.TypeContext.I32,
        TokenKind.I64Kw => _context.TypeContext.I64,
        TokenKind.F32Kw => _context.TypeContext.F32,
        TokenKind.F64Kw => _context.TypeContext.F64,
        TokenKind.BoolKw => _context.TypeContext.Bool,
        TokenKind.StringKw => _context.TypeContext.String,
        TokenKind.NoneKw => _context.TypeContext.None,
        TokenKind.NeverKw => _context.TypeContext.Never,
        _ => throw new UnreachableException($"Unknown {nameof(NativeTypeNameSyntax)}.")
    };

    private AxlType BindPathType(PathSyntax syntax)
    {
        if (syntax.Span?.IsEmpty == false)
        {
            _context.DiagnosticBag.ReportError(new Diagnostic.UnsupportedFeature(
                syntax, "Only native types are supported for now."));
        }

        return _context.TypeContext.Error;
    }
    
    #endregion
    
    
    private ImmutableArray<HirStmt> BindStmts(IEnumerable<StmtSyntax> syntaxes)
    {
        return [..syntaxes.Select(BindStmt)];
    }
    
    private HirStmt BindStmt(StmtSyntax syntax) => syntax switch
    {
        VarDeclSyntax varDeclSyntax => BindVarDecl(varDeclSyntax),
        ExprStmtSyntax exprStmt => BindExpr(exprStmt.Expr, expectedType: null),
        _ => throw new UnreachableException($"Unknown {nameof(StmtSyntax)}")
    };

    private HirVarDecl BindVarDecl(VarDeclSyntax syntax)
    {
        var boundTypeAnnotation = syntax.TypeAnnotation is not null
            ? BindType(syntax.TypeAnnotation)
            : null;
        
        var boundInitializer = BindVarDeclInitializer(syntax, expectedType: boundTypeAnnotation);

        if (boundTypeAnnotation is null)
        {
            // Infer type
            boundTypeAnnotation = boundInitializer.Type;
        }
        else
        {
            // Check type
            if (!_context.TypeContext.IsAssignableTo(source: boundInitializer.Type, target: boundTypeAnnotation))
            {
                Debug.Assert(syntax.Initializer is not null, "Initializer must be given, because error type never fails type checking.");
                
                _context.DiagnosticBag.ReportError(new Diagnostic.TypeMismatch(
                    SourceType: boundInitializer.Type,
                    TargetType: boundTypeAnnotation, 
                    SourceSyntax: syntax.Initializer,
                    TargetSyntax: syntax.TypeAnnotation!));
            }
        }

        var local = new LocalSymbol(_context.Compilation,
            SymbolName.From(syntax.Name),
            boundTypeAnnotation,
            syntax,
            parent: _context.ParentSymbol);
        _scope.Declare(local);

        return new HirVarDecl(local, boundInitializer);
    }

    private HirExpr BindVarDeclInitializer(VarDeclSyntax varDeclSyntax, AxlType? expectedType)
    {
        if (varDeclSyntax.Initializer is null)
        {
            _context.DiagnosticBag.ReportError(new Diagnostic.MissingInitializer(varDeclSyntax));
            return new HirErrorExpr(recoveredExprs: [],
                _context.TypeContext.Error);
        }

        return BindExpr(varDeclSyntax.Initializer, expectedType);
    }

    private HirExpr BindExpr(ExprSyntax syntax, AxlType? expectedType) => syntax switch
    {
        NumberLiteralSyntax numberLiteralSyntax => BindNumberLiteral(numberLiteralSyntax, expectedType),
        
        _ => new HirErrorExpr(recoveredExprs: [], _context.TypeContext.Error)
    };

    private HirNumberLiteral BindNumberLiteral(NumberLiteralSyntax numberLiteralSyntax, AxlType? expectedType)
    {
        var type = numberLiteralSyntax.Token.Suffix switch
        {
            NumberLiteralSuffix.I32 => _context.TypeContext.I32,
            NumberLiteralSuffix.I64 => _context.TypeContext.I64,
            NumberLiteralSuffix.F32 => _context.TypeContext.F32,
            NumberLiteralSuffix.F64 => _context.TypeContext.F64,

            _ => DetermineTypeWithoutSuffix()
        };
        
        // Check the type against literal structure.
        // Literals with a decimal point can only become floating
        // point literals.
        if (numberLiteralSyntax.Token.HasDecimalPoint &&
            type is not (F32Type or F64Type))
        {
            _context.DiagnosticBag.ReportError(new Diagnostic.NumberSuffixMismatch(numberLiteralSyntax, type));
        }
        
        return new HirNumberLiteral(numberLiteralSyntax.Token, type);

        AxlType DetermineTypeWithoutSuffix()
        {
            var hasDecimalPoint = numberLiteralSyntax.Token.HasDecimalPoint;

            return (hasDecimalPoint, expectedType) switch
            {
                // Without decimal point, the literal can become i32, i64, f32, f64
                (false, I32Type) => _context.TypeContext.I32,
                (false, I64Type) => _context.TypeContext.I64,
                (false, F32Type) => _context.TypeContext.F32,
                (false, F64Type) => _context.TypeContext.F64,
                (false, _) => _context.TypeContext.DefaultIntegralNumberType,
                
                // With decimal point, the literal can become f32, f64
                (true, F32Type) => _context.TypeContext.F32,
                (true, F64Type) => _context.TypeContext.F64,
                (true, _) => _context.TypeContext.DefaultFloatingNumberType,
            };
        }
    }
}