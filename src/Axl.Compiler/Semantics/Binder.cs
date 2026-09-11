using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Hir;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics;

public sealed class Binder
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly TypeContext _types;
    
    private Scope _scope;
    
    
    private Binder(Scope scope, TypeContext typeContext)
    {
        _types = typeContext;

        _scope = scope;
    }

    public static HirFile BindFile(FileSyntax syntax, TypeContext typeContext)
    {
        var scope = new Scope();
        var binder = new Binder(scope, typeContext);
        
        // Everything other than stmts is not supported yet.
        foreach (var node in syntax.SyntaxNodes().Where(n => n is not StmtSyntax))
            binder._diagnostics.ReportError(new Diagnostic.UnsupportedFeature(node));

        var stmts = syntax.Stmts.Select(binder.BindStmt).ToImmutableArray();
        var body = new HirBody(stmts, armExpr: null, type: typeContext.None, syntax);

        return new HirFile(body, binder._diagnostics.Drain());
    }
    
    
    /// <summary>
    /// Checks, whether <paramref name="expr"/> is assignable to
    /// <paramref name="expected"/>. If not, reports a <see cref="Diagnostic.TypeMismatch"/>.
    /// </summary>
    private bool CheckTypeAndReportMismatch(HirExpr expr, TypeSymbol expected)
    {
        if (!_types.IsAssignableTo(expr.Type, expected))
        {
            _diagnostics.ReportError(new Diagnostic.TypeMismatch(
                Expr: expr,
                Expected: expected));
            return false;
        }

        return true;
    }

    private Symbol? LookupAndReportUndefined(IdNameSyntax syntax)
    {
        var symbol = _scope.Lookup(SymbolName.From(syntax));
        
        if (symbol is null)
            _diagnostics.ReportError(new Diagnostic.UndefinedName(syntax));
        return symbol;
    }
    
    #region Type names

    private TypeSymbol BindType(TypeNameSyntax syntax) => syntax switch
    {
        NativeTypeNameSyntax nativeTypeNameSyntax => BindNativeType(nativeTypeNameSyntax),
        _ => BindUnsupportedType(syntax)
    };

    private TypeSymbol BindNativeType(NativeTypeNameSyntax syntax) => syntax.Token.Kind switch
    {
        TokenKind.I32Kw => _types.I32,
        TokenKind.I64Kw => _types.I64,
        TokenKind.F32Kw => _types.F32,
        TokenKind.F64Kw => _types.F64,
        TokenKind.BoolKw => _types.Bool,
        TokenKind.StringKw => _types.String,
        TokenKind.NoneKw => _types.None,
        TokenKind.NeverKw => _types.Never,
        _ => throw new UnreachableException($"Unknown {nameof(NativeTypeNameSyntax)}.")
    };

    private TypeSymbol BindUnsupportedType(TypeNameSyntax syntax)
    {
        _diagnostics.ReportError(new Diagnostic.UnsupportedFeature(syntax));
        return _types.Error;
    }
    
    #endregion
    
    
    private HirStmt BindStmt(StmtSyntax syntax) => syntax switch
    {
        VarDeclSyntax varDeclSyntax => BindVarDecl(varDeclSyntax),
        ExprStmtSyntax exprStmt => BindExpr(exprStmt.Expr),
        _ => throw new UnreachableException($"Unknown {nameof(StmtSyntax)}")
    };

    private HirExpr BindExpr(ExprSyntax syntax) => syntax switch
    {
        // Symbol references
        IdNameSyntax idNameSyntax => BindPlainIdName(idNameSyntax),
        AssignExprSyntax assignExprSyntax => BindAssign(assignExprSyntax),
        
        // Operators
        BinaryExprSyntax binaryExprSyntax => BindBinary(binaryExprSyntax),
        UnaryExprSyntax unaryExprSyntax => BindUnary(unaryExprSyntax),
        
        // Bodies / Control Flow
        BlockExprSyntax blockExprSyntax => BindBlock(blockExprSyntax),
        IfExprSyntax ifExprSyntax => BindIf(ifExprSyntax),
        ArmSyntax armSyntax => BindExpr(armSyntax.Expr),
        GroupExprSyntax groupExprSyntax => BindExpr(groupExprSyntax.Inner),
        
        // Strings and Literals
        NumberLiteralSyntax numberLiteralSyntax => BindNumberLiteral(numberLiteralSyntax),
        TrueLiteralSyntax => new HirBoolLiteral(value: true, type: _types.Bool, syntax),
        FalseLiteralSyntax => new HirBoolLiteral(value: false, type: _types.Bool, syntax),
        StringExprSyntax stringExprSyntax => BindString(stringExprSyntax),
        
        // Error and unsupported
        ErrorExprSyntax errorExprSyntax => BindError(errorExprSyntax),
        _ => BindUnsupported(syntax)
    };

    

    private HirExpr BindUnsupported(ExprSyntax syntax)
    {
        _diagnostics.ReportError(new Diagnostic.UnsupportedFeature(syntax));
        return new HirErrorExpr(recoveredExprs: [], type: _types.Error, syntax);
    }

    private HirErrorExpr BindError(ErrorExprSyntax syntax)
    {
        var recovered = syntax.RecoverableNodes
            .Select(node => BindExpr(node))
            .ToImmutableArray();
        return new HirErrorExpr(recovered, _types.Error, syntax);
    }
    
    
    #region Variables

    private HirVarDecl BindVarDecl(VarDeclSyntax syntax)
    {
        var variableType = syntax.TypeAnnotation is not null
            ? BindType(syntax.TypeAnnotation)
            : null;
        
        var boundInitializer = BindVarDeclInitializer(syntax);

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

        var variable = new VariableSymbol(SymbolName.From(syntax.Name), variableType);
        _scope.Declare(variable);

        return new HirVarDecl(variable, boundInitializer, syntax);
    }
    
    private HirExpr BindVarDeclInitializer(VarDeclSyntax syntax)
    {
        if (syntax.Initializer is null)
        {
            _diagnostics.ReportError(new Diagnostic.MissingInitializer(syntax));
            
            // LIE and add the entire var decl syntax. This is the only (probably) case,
            // where a null syntax would be nice. But practically, syntax shouldn't be
            // touched on an error expr, si it should be fine. Mark my words in case of
            // oddities :D.
            return new HirErrorExpr(recoveredExprs: [],
                _types.Error, syntax);    
        }

        return BindExpr(syntax.Initializer);
    }

    private HirExpr BindPlainIdName(IdNameSyntax syntax)
    {
        var symbol = LookupAndReportUndefined(syntax);

        switch (symbol)
        {
            case VariableSymbol variable:
                return new HirVariableRef(variable, syntax);
            
            case null:
                return new HirErrorExpr(recoveredExprs: [], type: _types.Error, syntax);
            
            default:
                throw new UnreachableException($"Unknown symbol kind {symbol.GetType().Name}.");
        }
    }

    private HirExpr BindAssign(AssignExprSyntax syntax)
    {
        var boundValue = BindExpr(syntax.Value);
        var target = BindAssignTarget(syntax.Target);
        
        // Reject compound assignment
        if (syntax.Operator.Kind is not TokenKind.Equal)
        {
            _diagnostics.ReportError(
                new Diagnostic.UnsupportedFeature(syntax, "Compound assignment not supported yet."));
            return new HirErrorExpr(recoveredExprs: [boundValue], _types.Error, syntax);
        }

        if (target is null)
            return new HirErrorExpr(recoveredExprs: [boundValue], _types.Error, syntax);
        
        CheckTypeAndReportMismatch(boundValue, target.Type);
        return new HirAssign(target, boundValue, _types.None, syntax);
    }

    private VariableSymbol? BindAssignTarget(ExprSyntax syntax)
    {
        if (syntax is not IdNameSyntax idNameSyntax)
        {
            _diagnostics.ReportError(new Diagnostic.InvalidAssignTarget(syntax));
            return null;
        }

        var symbol = LookupAndReportUndefined(idNameSyntax);
        switch (symbol)
        {
            case VariableSymbol variable:
                return variable;
            
            case null:
                return null;
            
            default:
                _diagnostics.ReportError(new Diagnostic.InvalidAssignTarget(syntax, symbol));
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
        
        return new HirStringExpr(parts, _types.String, syntax);
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
                
        var boundExpr = BindExpr(syntax.Expr);
        
        //TODO: Allow different types according to declared native conversion fns
        
        // For now, we can only accept string exprs
        if (!CheckTypeAndReportMismatch(boundExpr, _types.String))
        {
            return new StringPart.Interpolation(
                new HirErrorExpr(recoveredExprs: [boundExpr], type: _types.Error, syntax));
        }

        return new StringPart.Interpolation(boundExpr);
    }

    private HirNumberLiteral BindNumberLiteral(NumberLiteralSyntax syntax)
    {
        TypeSymbol type = syntax.Token.Suffix switch
        {
            NumberLiteralSuffix.I32 => _types.I32,
            NumberLiteralSuffix.I64 => _types.I64,
            NumberLiteralSuffix.F32 => _types.F32,
            NumberLiteralSuffix.F64 => _types.F64,

            _ => syntax.Token.HasDecimalPoint ? _types.DefaultFloatingNumberType : _types.DefaultIntegralNumberType
        };
        
        // Check the type against literal structure.
        // Literals with a decimal point can only become floating
        // point literals.
        if (syntax.Token.HasDecimalPoint &&
            type != _types.F32 && type != _types.F64)
        {
            _diagnostics.ReportError(new Diagnostic.SuffixInvalidForDecimalNumber(syntax));
        }
        
        return new HirNumberLiteral(syntax.Token, type, syntax);
    }
    
    #endregion
    
    #region Binary and Unary Exprs

    private HirExpr BindBinary(BinaryExprSyntax syntax) => syntax.Operator.Kind switch
    {
        TokenKind.AndKw or TokenKind.OrKw => BindBooleanOperator(syntax),
        
        TokenKind.DoubleEqual or TokenKind.BangEqual => BindEqualityComparison(syntax),
        
        _ => BindNativeOperator(syntax.Operator, syntax, syntax.Left, syntax.Right)
    };
    
    private HirExpr BindUnary(UnaryExprSyntax syntax)
    {
        return BindNativeOperator(syntax.Operator, syntax, syntax.Operand);
    }



    private HirExpr BindEqualityComparison(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.DoubleEqual or TokenKind.BangEqual);
        
        var boundLeft = BindExpr(syntax.Left);
        var boundRight = BindExpr(syntax.Right);
        if (boundLeft.Type == _types.Error || boundRight.Type == _types.Error)
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new HirErrorExpr(recoveredExprs: [boundLeft, boundRight],
                type: _types.Error, syntax);
        }
        
        // Equality type-checks everything
        return new HirEqualityComparison(boundLeft, boundRight,
            kind: syntax.Operator.Kind switch
            {
                TokenKind.DoubleEqual => EqualityComparisonKind.Equals,
                TokenKind.BangEqual => EqualityComparisonKind.NotEquals,
                _ => throw new UnreachableException()
            },
            type: _types.Bool, 
            syntax);
    }
    
    private HirExpr BindBooleanOperator(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.AndKw or TokenKind.OrKw);

        var boundLeft = BindExpr(syntax.Left);
        var boundRight = BindExpr(syntax.Right);
        if (boundLeft.Type == _types.Error || boundRight.Type == _types.Error)
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new HirErrorExpr(recoveredExprs: [boundLeft, boundRight],
                type: _types.Error, syntax);
        }

        // Type-check against bool
        if (!CheckTypeAndReportMismatch(boundLeft, _types.Bool) ||
            !CheckTypeAndReportMismatch(boundRight, _types.Bool))
        {
            return new HirErrorExpr(recoveredExprs: [boundLeft, boundRight],
                type: _types.Error, syntax);
        }

        if (syntax.Operator.Kind is TokenKind.AndKw)
            return new HirAnd(boundLeft, boundRight, _types.Bool, syntax);
        if (syntax.Operator.Kind is TokenKind.OrKw)
            return new HirOr(boundLeft, boundRight, _types.Bool, syntax);

        throw new UnreachableException();
    }

    private HirExpr BindNativeOperator(Token operatorToken, SyntaxNode syntax, params IEnumerable<ExprSyntax> operands)
    {
        var boundOperands = operands
            .Select(expr => BindExpr(expr))
            .ToImmutableArray();
        
        var operandTypes = boundOperands
            .Select(hir => hir.Type)
            .ToImmutableArray();
        if (operandTypes.Any(type => type == _types.Error))
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new HirErrorExpr(recoveredExprs: boundOperands,
                type: _types.Error, syntax);
        }

        var nativeOperator = _types.TryGetNativeOperator(
            operatorToken.Kind,
            operandTypes);
        
        if (nativeOperator is null)
        {
            _diagnostics.ReportError(
                new Diagnostic.UndefinedOperator(operatorToken, boundOperands));
            return new HirErrorExpr(recoveredExprs: [.. boundOperands],
                type: _types.Error, syntax);
        }

        return new HirNativeOperator(nativeOperator, [.. boundOperands], nativeOperator.ReturnType, syntax);
    }
    
    #endregion
    
    #region Blocks, If, Loop

    private HirBody BindBlock(BlockExprSyntax syntax)
    {
        _scope = new Scope(parent: _scope);

        var stmts = syntax.Stmts.Select(BindStmt).ToImmutableArray();

        var arm = syntax.Arm is not null
            ? BindExpr(syntax.Arm.Expr)
            : null;

        var blockType = arm?.Type ?? _types.None;

        return new HirBody(stmts, arm, blockType, syntax);
    }

    private HirExpr BindIf(IfExprSyntax syntax)
    {
        var boundPredicate = BindExpr(syntax.Predicate);
        var boundBody = BindExpr(syntax.Body);
        var boundElse = syntax.ElseBody is not null ? BindExpr(syntax.ElseBody) : null;
        
        // Type-check predicate
        if (!CheckTypeAndReportMismatch(boundPredicate, expected: _types.Bool))
            boundPredicate = new HirErrorExpr(recoveredExprs: [boundPredicate], type: _types.Error, syntax.Predicate);
        
        // Type-check body and else body
        // They must have the same type.
        TypeSymbol ifExprType;
        if (boundElse is not null)
        {
            ifExprType = boundBody.Type;
            CheckTypeAndReportMismatch(boundElse, expected: ifExprType);
        }
        else
        {
            // If without else always has type none.
            
            ifExprType = _types.None;
        }

        return new HirIf(boundPredicate, boundBody, boundElse, ifExprType, syntax);
    }

    #endregion
}