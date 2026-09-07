using System.Collections.Immutable;
using System.Diagnostics;
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
        public ImmutableArray<Symbol>.Builder LocalMembers { get; } = ImmutableArray.CreateBuilder<Symbol>();

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
        var members = BindMembers(fileSyntax.Members, context);
        
        // Create scope and binder
        var localScope = new LocalScope(members, parent: enclosingScope);
        var binder = new Binder(context, localScope, LoopContext.Outside);
        
        // Bind statements
        var stmts = binder.BindStmts(fileSyntax.Stmts);
        
        // Create and return
        var body = new HirBody(
            stmts, 
            armExpr: null, 
            type: context.TypeContext.None,
            syntax: fileSyntax);
        return new Hir.Hir(body, 
            context.LocalMembers.DrainToImmutable(),
            context.DiagnosticBag.Drain());
    }

    
    #region Helpers
    
    private TypeContext Types => _context.TypeContext;
    
    /// <summary>
    /// Checks, whether <paramref name="expr"/> is assignable to
    /// <paramref name="expected"/>. If not, reports a <see cref="Diagnostic.TypeMismatch"/>.
    /// </summary>
    private bool CheckTypeAndReportMismatch(HirExpr expr, AxlType expected)
    {
        if (!Types.IsAssignableTo(expr.Type, expected))
        {
            _context.DiagnosticBag.ReportError(new Diagnostic.TypeMismatch(
                Expr: expr,
                Expected: expected));
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Looks up a name expecting a single symbol. Reports <see cref="Diagnostic.UndefinedName"/>
    /// or <see cref="Diagnostic.AmbiguousName"/> accordingly.
    /// </summary>
    private Symbol? LookupSingleAndReportUndefinedOrAmbiguous(IdNameSyntax nameSyntax)
    {
        if (nameSyntax.Token.IsMissing)
            return null;
        
        var name = SymbolName.From(nameSyntax);
        var lookupResult = _scope.Lookup(name);

        switch (lookupResult.Length)
        {
            case 0:
                _context.DiagnosticBag.ReportError(new Diagnostic.UndefinedName(nameSyntax));
                return null;
            case > 1:
                _context.DiagnosticBag.ReportError(new Diagnostic.AmbiguousName(nameSyntax, lookupResult));
                return null;
            default:
                return lookupResult[0];
        }
    }
    
    #endregion
    
    
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
        
        context.LocalMembers.AddRange(memberArray);
        
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
        TokenKind.I32Kw => Types.I32,
        TokenKind.I64Kw => Types.I64,
        TokenKind.F32Kw => Types.F32,
        TokenKind.F64Kw => Types.F64,
        TokenKind.BoolKw => Types.Bool,
        TokenKind.StringKw => Types.String,
        TokenKind.NoneKw => Types.None,
        TokenKind.NeverKw => Types.Never,
        _ => throw new UnreachableException($"Unknown {nameof(NativeTypeNameSyntax)}.")
    };

    private AxlType BindPathType(PathSyntax syntax)
    {
        if (syntax.Span?.IsEmpty == false)
        {
            _context.DiagnosticBag.ReportError(new Diagnostic.UnsupportedFeature(
                syntax, "Only native types are supported for now."));
        }

        return Types.Error;
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

    private HirExpr BindExpr(ExprSyntax syntax, AxlType? expectedType) => syntax switch
    {
        // Symbol references
        IdNameSyntax idNameSyntax => BindPlainIdName(idNameSyntax),
        AssignExprSyntax assignExprSyntax => BindAssign(assignExprSyntax),
        
        // Operators
        BinaryExprSyntax binaryExprSyntax => BindBinary(binaryExprSyntax, expectedType),
        UnaryExprSyntax unaryExprSyntax => BindUnary(unaryExprSyntax, expectedType),
        
        // Bodies / Control Flow
        BlockExprSyntax blockExprSyntax => BindBlock(blockExprSyntax),
        IfExprSyntax ifExprSyntax => BindIf(ifExprSyntax),
        ArmSyntax armSyntax => BindExpr(armSyntax.Expr, expectedType: null),
        
        // Strings and Literals
        NumberLiteralSyntax numberLiteralSyntax => BindNumberLiteral(numberLiteralSyntax, expectedType),
        TrueLiteralSyntax => new HirBoolLiteral(value: true, type: Types.Bool, syntax),
        FalseLiteralSyntax => new HirBoolLiteral(value: false, type: Types.Bool, syntax),
        StringExprSyntax stringExprSyntax => BindString(stringExprSyntax),
        
        // Error and unsupported
        ErrorExprSyntax errorExprSyntax => BindError(errorExprSyntax),
        _ => BindUnsupported(syntax)
    };

    

    private HirExpr BindUnsupported(ExprSyntax syntax)
    {
        _context.DiagnosticBag.ReportError(new Diagnostic.UnsupportedFeature(syntax));
        return new HirErrorExpr(recoveredExprs: [], type: Types.Error, syntax);
    }

    private HirErrorExpr BindError(ErrorExprSyntax syntax)
    {
        var recovered = syntax.RecoverableNodes
            .Select(node => BindExpr(node, expectedType: null))
            .ToImmutableArray();
        return new HirErrorExpr(recovered, Types.Error, syntax);
    }
    
    #region Variables

    private HirVarDecl BindVarDecl(VarDeclSyntax syntax)
    {
        var variableType = syntax.TypeAnnotation is not null
            ? BindType(syntax.TypeAnnotation)
            : null;
        
        var boundInitializer = BindVarDeclInitializer(syntax, expectedType: variableType);

        if (variableType is null)
        {
            // Infer type
            variableType = boundInitializer.Type;
        }
        else
        {
            // Check type
            CheckTypeAndReportMismatch(boundInitializer, variableType);
        }

        var local = new LocalSymbol(_context.Compilation,
            SymbolName.From(syntax.Name),
            variableType,
            syntax,
            parent: _context.ParentSymbol);
        _scope.Declare(local);

        return new HirVarDecl(local, boundInitializer, syntax);
    }
    
    private HirExpr BindVarDeclInitializer(VarDeclSyntax syntax, AxlType? expectedType)
    {
        if (syntax.Initializer is null)
        {
            _context.DiagnosticBag.ReportError(new Diagnostic.MissingInitializer(syntax));
            
            // LIE and add the entire var decl syntax. This is the only (probably) case,
            // where a null syntax would be nice. But practically, syntax shouldn't be
            // touched on an error expr, si it should be fine. Mark my words in case of
            // oddities :D.
            return new HirErrorExpr(recoveredExprs: [],
                Types.Error, syntax);    
        }

        return BindExpr(syntax.Initializer, expectedType);
    }

    private HirExpr BindPlainIdName(IdNameSyntax syntax)
    {
        var symbol = LookupSingleAndReportUndefinedOrAmbiguous(syntax);

        switch (symbol)
        {
            case LocalSymbol localSymbol:
                return new HirLocalRef(localSymbol, syntax);
            
            case null:
                return new HirErrorExpr(recoveredExprs: [], type: Types.Error, syntax);
            
            default:
                _context.DiagnosticBag.ReportError(new Diagnostic.InvalidLocalRef(syntax, symbol));
                return new HirErrorExpr(recoveredExprs: [], type: Types.Error, syntax);
        }
    }

    private HirExpr BindAssign(AssignExprSyntax syntax)
    {
        var boundValue = BindExpr(syntax.Value, null);
        var target = BindAssignTarget(syntax.Target);
        
        // Reject compound assignment
        if (syntax.Operator.Kind is not TokenKind.Equal)
        {
            _context.DiagnosticBag.ReportError(
                new Diagnostic.UnsupportedFeature(syntax, "Compound assignment not supported yet."));
            return new HirErrorExpr(recoveredExprs: [boundValue], Types.Error, syntax);
        }

        if (target is null)
            return new HirErrorExpr(recoveredExprs: [boundValue], Types.Error, syntax);
        
        CheckTypeAndReportMismatch(boundValue, target.Type);
        return new HirAssign(target, boundValue, Types.None, syntax);
    }

    private LocalSymbol? BindAssignTarget(ExprSyntax syntax)
    {
        if (syntax is not IdNameSyntax idNameSyntax)
        {
            _context.DiagnosticBag.ReportError(new Diagnostic.InvalidAssignTarget(syntax));
            return null;
        }

        var symbol = LookupSingleAndReportUndefinedOrAmbiguous(idNameSyntax);
        switch (symbol)
        {
            case LocalSymbol localSymbol:
                return localSymbol;
            
            case null:
                return null;
            
            default:
                _context.DiagnosticBag.ReportError(new Diagnostic.InvalidAssignTarget(syntax, symbol));
                return null;
        }
    }
    
    #endregion
    
    #region Literal and String Exprs
    
    private HirStringExpr BindString(StringExprSyntax syntax)
    {
        var parts = syntax.Parts.Select(BindStringPart).ToImmutableArray();

        if (parts.Length == 0)
            parts = [new StringPart.Text("")];
        
        return new HirStringExpr(parts, Types.String, syntax);
    }

    private StringPart BindStringPart(StringPartSyntax syntax)
        => syntax switch
        {
            StringTextSyntax textSyntax => new StringPart.Text(textSyntax.Text.ProcessedText),
            StringInterpolationSyntax interpolationSyntax => BindStringInterpolation(interpolationSyntax),
            _ => throw new UnreachableException($"Unknown {nameof(StringPartSyntax)}")
        };

    private StringPart BindStringInterpolation(StringInterpolationSyntax syntax)
    {
        if (syntax.Expr is null)
        {
            // Empty interpolation means nothing will be added. Just
            // return an empty text then.
            return new StringPart.Text(ProcessedText: "");
        }
                
        var boundExpr = BindExpr(syntax.Expr, expectedType: null);
        
        //TODO: Allow different types according to declared native conversion fns
        
        // For now, we can only accept string exprs
        if (!CheckTypeAndReportMismatch(boundExpr, Types.String))
        {
            return new StringPart.Interpolation(
                new HirErrorExpr(recoveredExprs: [boundExpr], type: Types.Error, syntax));
        }

        return new StringPart.Interpolation(boundExpr);
    }

    private HirNumberLiteral BindNumberLiteral(NumberLiteralSyntax syntax, AxlType? expectedType)
    {
        var type = syntax.Token.Suffix switch
        {
            NumberLiteralSuffix.I32 => Types.I32,
            NumberLiteralSuffix.I64 => Types.I64,
            NumberLiteralSuffix.F32 => Types.F32,
            NumberLiteralSuffix.F64 => Types.F64,

            _ => DetermineTypeWithoutSuffix()
        };
        
        // Check the type against literal structure.
        // Literals with a decimal point can only become floating
        // point literals.
        if (syntax.Token.HasDecimalPoint &&
            type is not (F32Type or F64Type))
        {
            _context.DiagnosticBag.ReportError(new Diagnostic.NumberSuffixMismatch(syntax, type));
        }
        
        return new HirNumberLiteral(syntax.Token, type, syntax);

        AxlType DetermineTypeWithoutSuffix()
        {
            var hasDecimalPoint = syntax.Token.HasDecimalPoint;

            return (hasDecimalPoint, expectedType) switch
            {
                // Without decimal point, the literal can become i32, i64, f32, f64
                (false, I32Type) => Types.I32,
                (false, I64Type) => Types.I64,
                (false, F32Type) => Types.F32,
                (false, F64Type) => Types.F64,
                (false, _) => Types.DefaultIntegralNumberType,
                
                // With decimal point, the literal can become f32, f64
                (true, F32Type) => Types.F32,
                (true, F64Type) => Types.F64,
                (true, _) => Types.DefaultFloatingNumberType,
            };
        }
    }
    
    #endregion
    
    #region Binary and Unary Exprs

    private HirExpr BindBinary(BinaryExprSyntax syntax, AxlType? expectedType) => syntax.Operator.Kind switch
    {
        TokenKind.AndKw or TokenKind.OrKw => BindBooleanOperator(syntax),
        
        TokenKind.DoubleEqual or TokenKind.BangEqual => BindEqualityComparison(syntax),
        
        _ => BindNativeOperator(syntax.Operator, syntax, syntax.Left, syntax.Right)
    };
    
    private HirExpr BindUnary(UnaryExprSyntax syntax, AxlType? expectedType)
    {
        return BindNativeOperator(syntax.Operator, syntax, syntax.Operand);
    }



    private HirExpr BindEqualityComparison(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.DoubleEqual or TokenKind.BangEqual);
        
        var boundLeft = BindExpr(syntax.Left, expectedType: null);
        var boundRight = BindExpr(syntax.Right, expectedType: null);
        if (boundLeft.Type is ErrorType || boundRight.Type is ErrorType)
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new HirErrorExpr(recoveredExprs: [boundLeft, boundRight],
                type: Types.Error, syntax);
        }
        
        // Equality type-checks everything
        return new HirEqualityComparison(boundLeft, boundRight,
            kind: syntax.Operator.Kind switch
            {
                TokenKind.DoubleEqual => EqualityComparisonKind.Equals,
                TokenKind.BangEqual => EqualityComparisonKind.NotEquals,
                _ => throw new UnreachableException()
            },
            type: Types.Bool, 
            syntax);
    }
    
    private HirExpr BindBooleanOperator(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.AndKw or TokenKind.OrKw);

        var boundLeft = BindExpr(syntax.Left, expectedType: null);
        var boundRight = BindExpr(syntax.Right, expectedType: null);
        if (boundLeft.Type is ErrorType || boundRight.Type is ErrorType)
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new HirErrorExpr(recoveredExprs: [boundLeft, boundRight],
                type: Types.Error, syntax);
        }

        // Type-check against bool
        if (!CheckTypeAndReportMismatch(boundLeft, Types.Bool) ||
            !CheckTypeAndReportMismatch(boundRight, Types.Bool))
        {
            return new HirErrorExpr(recoveredExprs: [boundLeft, boundRight],
                type: Types.Error, syntax);
        }

        if (syntax.Operator.Kind is TokenKind.AndKw)
            return new HirAnd(boundLeft, boundRight, Types.Bool, syntax);
        if (syntax.Operator.Kind is TokenKind.OrKw)
            return new HirOr(boundLeft, boundRight, Types.Bool, syntax);

        throw new UnreachableException();
    }

    private HirExpr BindNativeOperator(Token operatorToken, SyntaxNode syntax, params IEnumerable<ExprSyntax> operands)
    {
        var boundOperands = operands
            .Select(expr => BindExpr(expr, expectedType: null))
            .ToImmutableArray();
        
        var operandTypes = boundOperands
            .Select(hir => hir.Type)
            .ToImmutableArray();
        if (operandTypes.OfType<ErrorType>().Any())
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new HirErrorExpr(recoveredExprs: boundOperands,
                type: Types.Error, syntax);
        }

        var nativeOperator = Types.FindNativeOperator(
            operatorToken.Kind,
            operandTypes);
        
        if (nativeOperator is null)
        {
            _context.DiagnosticBag.ReportError(
                new Diagnostic.UndefinedOperator(operatorToken, boundOperands));
            return new HirErrorExpr(recoveredExprs: [.. boundOperands],
                type: Types.Error, syntax);
        }

        return new HirNativeOperator(nativeOperator, [.. boundOperands], nativeOperator.ReturnType, syntax);
    }
    
    #endregion
    
    #region Blocks, If, Loop

    private HirBody BindBlock(BlockExprSyntax syntax)
    {
        // Binding context and loop context stays the same
        
        // Bind block members
        var members = BindMembers(syntax.Members, _context);
        var newScope = new LocalScope(members, parent: _scope);

        var blockBinder = new Binder(_context, newScope, _loopContext);

        var stmts = blockBinder.BindStmts(syntax.Stmts);

        var boundArm = syntax.Arm is not null
            ? blockBinder.BindExpr(syntax.Arm.Expr, expectedType: null)
            : null;

        return new HirBody(stmts, 
            boundArm, 
            type: boundArm?.Type ?? Types.None,
            syntax);
    }

    private HirExpr BindIf(IfExprSyntax syntax)
    {
        var boundPredicate = BindExpr(syntax.Predicate, null);
        var boundBody = BindExpr(syntax.Body, null);
        var boundElse = syntax.ElseBody is not null ? BindExpr(syntax.ElseBody, null) : null;
        
        // Type-check predicate
        if (!CheckTypeAndReportMismatch(boundPredicate, expected: Types.Bool))
            boundPredicate = new HirErrorExpr(recoveredExprs: [boundPredicate], type: Types.Error, syntax.Predicate);
        
        // Type-check body and else body
        // They must have the same type.
        var ifExprType = boundBody.Type;
        if (boundElse is not null)
            CheckTypeAndReportMismatch(boundElse, expected: ifExprType);

        return new HirIf(boundPredicate, boundBody, boundElse, ifExprType, syntax);
    }

    
    
    #endregion
}