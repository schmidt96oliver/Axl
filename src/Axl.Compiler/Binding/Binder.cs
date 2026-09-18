using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Permissions;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Binding;

public sealed class Binder
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly BaseModuleSymbol _baseModule;
    
    private Scope _scope;
    private bool _inLoop = false;
    
    
    private Binder(Scope scope, BaseModuleSymbol baseModule)
    {
        _baseModule = baseModule;

        _scope = scope;
    }

    private static Scope CreateGlobalScope(BaseModuleSymbol baseModule)
    {
        var global = new Scope();
        
        global.Declare(baseModule);
        
        // 'Base' is implicitly used
        foreach (var member in baseModule.Members)
            global.Declare(member);
        
        return global;
    }
    
    public static BoundFile BindFile(FileSyntax syntax, BaseModuleSymbol baseModule)
    {
        var scope = new Scope(parent: CreateGlobalScope(baseModule));
        var binder = new Binder(scope, baseModule);
        
        // Everything other than stmts is not supported yet.
        foreach (var node in syntax.SyntaxNodes().Where(n => n is not StmtSyntax))
            binder._diagnostics.ReportError(new Diagnostic.UnsupportedFeature(node));

        var stmts = syntax.Stmts.Select(binder.BindStmt).ToImmutableArray();
        var block = new BoundBlock(stmts, type: baseModule.Unit, syntax);

        return new BoundFile(block, binder._diagnostics.Drain());
    }
    
    
    /// <summary>
    /// Whether a value of type <paramref name="source"/> can be
    /// assigned to a target of type <paramref name="target"/>.
    /// </summary>
    public bool IsAssignableTo(TypeSymbol source, TypeSymbol target)
    {
        // Errors are silent
        if (source == _baseModule.Error || target == _baseModule.Error) return true;
        
        // Never assigns to anything
        if (source == _baseModule.Never) return true;

        return source == target;
    }
    
    
    /// <summary>
    /// Checks, whether <paramref name="expr"/> is assignable to
    /// <paramref name="expected"/>. If not, reports a <see cref="Diagnostic.TypeMismatch"/>.
    /// </summary>
    /// <returns><c>true</c>, if types matched. <c>false</c>, otherwise.</returns>
    private bool CheckTypeAndReportMismatch(BoundExpr expr, TypeSymbol expected)
    {
        if (!IsAssignableTo(expr.Type, expected))
        {
            _diagnostics.ReportError(new Diagnostic.TypeMismatch(
                Expr: expr,
                Expected: expected));
            return false;
        }

        return true;
    }

    private Symbol? LookupAndReportUndefined(IdentifierToken syntax)
    {
        if (syntax.IsMissing)
            return null;
        
        var symbol = _scope.Lookup(SymbolName.From(syntax));
        
        if (symbol is null)
            _diagnostics.ReportError(new Diagnostic.UndefinedName(syntax));
        return symbol;
    }

    private Symbol? LookupAndReportUndefined(IdNameSyntax syntax)
        => LookupAndReportUndefined(syntax.Token);
    
    
    
    
    #region Type names

    private TypeSymbol BindTypeName(TypeNameSyntax syntax)
    {
        var parts = syntax.Parts.ToImmutableArray();
        
        var current = LookupAndReportUndefined(parts[0]);
        if (current is null)
            return _baseModule.Error;

        for (var i = 1; i < parts.Length; i++)
        {
            var partName = SymbolName.From(parts[i]);
            if (partName.IsEmpty)
                return _baseModule.Error;

            if (current is BaseModuleSymbol module)
            {
                var member = module.LookupMember(partName);
                if (member is null)
                {
                    _diagnostics.ReportError(new Diagnostic.UndefinedMember(parts[i], current));
                    return _baseModule.Error;
                }

                current = member;
            }
            else
            {
                _diagnostics.ReportError(new Diagnostic.UndefinedMember(parts[i], current));
                return _baseModule.Error;
            }
        }

        Debug.Assert(current is not null);
        if (current is TypeSymbol type)
            return type;
        
        _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(parts[^1], current, SymbolKind.Type));
        return _baseModule.Error;
    }
    
    #endregion
    
    
    #region Stmts
    
    private BoundStmt BindStmt(StmtSyntax syntax) => syntax switch
    {
        VarDeclSyntax varDeclSyntax => BindVarDecl(varDeclSyntax),
        ExprStmtSyntax exprStmt => BindExprStmt(exprStmt.Expr),
        WhileStmtSyntax whileStmtSyntax => BindWhile(whileStmtSyntax),
    };

    private BoundStmt BindExprStmt(ExprSyntax exprSyntax)
    {
        if (exprSyntax is IfExprSyntax ifExprSyntax)
            return BindIfStmt(ifExprSyntax);

        return BindExpr(exprSyntax);
    }
    
    private BoundVarDecl BindVarDecl(VarDeclSyntax syntax)
    {
        var variableType = syntax.TypeAnnotation is not null
            ? BindTypeName(syntax.TypeAnnotation)
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
    
        return new BoundVarDecl(variable, boundInitializer, syntax);
    }
    
    private BoundExpr BindVarDeclInitializer(VarDeclSyntax syntax)
    {
        if (syntax.Initializer is null)
        {
            _diagnostics.ReportError(new Diagnostic.MissingInitializer(syntax));
            
            // LIE and add the entire var decl syntax. This is the only (probably) case,
            // where a null syntax would be nice. But practically, syntax shouldn't be
            // touched on an error expr, si it should be fine. Mark my words in case of
            // oddities :D.
            return new BoundErrorExpr(recoveredExprs: [],
                _baseModule.Error, syntax);    
        }
    
        return BindExpr(syntax.Initializer);
    }
    
    private BoundExpr BindAssign(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.Equal);
        
        var value = BindExpr(syntax.Right);
        var target = BindAssignTarget(syntax.Left);
        
        if (target is null)
            return new BoundErrorExpr(recoveredExprs: [value], _baseModule.Error, syntax);
        
        if (target.Type == _baseModule.Error || value.Type == _baseModule.Error)
            return new BoundAssign(target, value, value.Type, syntax);
        
        var type = CheckTypeAndReportMismatch(value, target.Type)
            ? _baseModule.Unit
            : _baseModule.Error;
        return new BoundAssign(target, value, type, syntax);
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

    private BoundExpr BindWhile(WhileStmtSyntax syntax)
    {
        var condition = BindCondition(syntax.Condition);

        var previousInLoop = _inLoop;
        _inLoop = true;
        var body = BindExpr(syntax.Body);
        _inLoop = previousInLoop;
        
        return new BoundWhile(condition, body, _baseModule.Unit, syntax);
    }
    
    #endregion
    
    private BoundExpr BindExpr(ExprSyntax syntax) => syntax switch
    {
        // Strings and Literals
        IdNameSyntax idNameSyntax => BindVariableRef(idNameSyntax),
        NumberLiteralSyntax numberLiteralSyntax => BindNumberLiteral(numberLiteralSyntax),
        TrueLiteralSyntax => new BoundBoolLiteral(value: true, type: _baseModule.Bool, syntax),
        FalseLiteralSyntax => new BoundBoolLiteral(value: false, type: _baseModule.Bool, syntax),
        StringExprSyntax stringExprSyntax => BindString(stringExprSyntax),
        
        // Operators
        BinaryExprSyntax binaryExprSyntax => BindBinary(binaryExprSyntax),
        UnaryExprSyntax unaryExprSyntax => BindUnary(unaryExprSyntax),
        
        GetMemberExprSyntax => BindUnsupported(syntax),
        CallExprSyntax => BindUnsupported(syntax),
        
        // Blocks and Control Flow
        BlockExprSyntax blockExprSyntax => BindBlock(blockExprSyntax),
        IfExprSyntax ifExprSyntax => BindIfExpr(ifExprSyntax),
        GroupExprSyntax groupExprSyntax => BindExpr(groupExprSyntax.Inner),
        BreakExprSyntax breakExprSyntax => BindBreakOrContinue(breakExprSyntax),
        ContinueExprSyntax or BreakExprSyntax => BindBreakOrContinue(syntax),
        ReturnExprSyntax returnExprSyntax => BindReturn(returnExprSyntax),
        
        // Error
        ErrorExprSyntax errorExprSyntax => BindError(errorExprSyntax),
    };
    
    

    private BoundErrorExpr BindUnsupported(SyntaxNode syntax)
    {
        _diagnostics.ReportError(new Diagnostic.UnsupportedFeature(syntax));
        return new BoundErrorExpr(recoveredExprs: [], type: _baseModule.Error, syntax);
    }

    private BoundErrorExpr BindError(ErrorExprSyntax syntax)
    {
        var recovered = syntax.RecoverableNodes
            .Select(BindExpr)
            .ToImmutableArray();
        return new BoundErrorExpr(recovered, _baseModule.Error, syntax);
    }
    
    
    #region Literals and Strings
    
    private BoundExpr BindVariableRef(IdNameSyntax syntax)
    {
        var symbol = LookupAndReportUndefined(syntax);

        if (symbol is null)
            return new BoundErrorExpr(recoveredExprs: [], type: _baseModule.Error, syntax);

        if (symbol is VariableSymbol variable)
            return new BoundVariableRef(variable, syntax);
        
        _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(syntax, symbol, SymbolKind.Variable));
        return new BoundErrorExpr(recoveredExprs: [], type: _baseModule.Error, syntax);
    }
    
    private BoundStringExpr BindString(StringExprSyntax syntax)
    {
        var parts = syntax.Parts.Select(BindStringPart).ToImmutableArray();
    
        if (parts.Length == 0)
            parts = [new StringPart.Text("")];
        
        return new BoundStringExpr(parts, _baseModule.String, syntax);
    }
    
    private StringPart BindStringPart(StringPartSyntax syntax)
        => syntax switch
        {
            StringTextSyntax textSyntax => new StringPart.Text(textSyntax.TextToken.ProcessedText),
            StringInterpolationSyntax interpolationSyntax => BindStringInterpolation(interpolationSyntax),
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
        if (!CheckTypeAndReportMismatch(boundExpr, _baseModule.String))
        {
            return new StringPart.Interpolation(
                new BoundErrorExpr(recoveredExprs: [boundExpr], type: _baseModule.Error, syntax));
        }
    
        return new StringPart.Interpolation(boundExpr);
    }
    
    private BoundNumberLiteral BindNumberLiteral(NumberLiteralSyntax syntax)
    {
        TypeSymbol type = syntax.Token.Suffix switch
        {
            NumberLiteralSuffix.I32 => _baseModule.I32,
            NumberLiteralSuffix.I64 => _baseModule.I64,
            NumberLiteralSuffix.F32 => _baseModule.F32,
            NumberLiteralSuffix.F64 => _baseModule.F64,
    
            _ => syntax.Token.HasDecimalPoint ? _baseModule.DefaultFloatType : _baseModule.DefaultIntType
        };
        
        // Check the type against literal structure.
        // Literals with a decimal point can only become floating
        // point literals.
        if (syntax.Token.HasDecimalPoint &&
            type != _baseModule.F32 && type != _baseModule.F64)
        {
            _diagnostics.ReportError(new Diagnostic.SuffixInvalidForDecimalNumber(syntax));
        }
        
        return new BoundNumberLiteral(syntax.Token, type, syntax);
    }
    
    #endregion
    
    #region Binary and Unary Exprs

    private BoundExpr BindBinary(BinaryExprSyntax syntax)
        => syntax.Operator.Kind switch
    {
        TokenKind.Equal => BindAssign(syntax),
        _ => BindUnsupported(syntax)
        
        // TokenKind.AndKw or TokenKind.OrKw => BindBooleanOperator(syntax),
        //
        // TokenKind.DoubleEqual or TokenKind.BangEqual => BindEqualityComparison(syntax),
        //
        //
        // _ => BindNativeOperator(syntax.Operator, syntax, syntax.Left, syntax.Right)
    };

    private BoundExpr BindUnary(UnaryExprSyntax syntax)
        => BindUnsupported(syntax);
    // {
    //     return BindNativeOperator(syntax.Operator, syntax, syntax.Operand);
    // }
    
    
    //
    // private BoundExpr BindEqualityComparison(BinaryExprSyntax syntax)
    // {
    //     Debug.Assert(syntax.Operator.Kind is TokenKind.DoubleEqual or TokenKind.BangEqual);
    //     
    //     var boundLeft = BindExpr(syntax.Left);
    //     var boundRight = BindExpr(syntax.Right);
    //     if (boundLeft.Type == _baseModule.Error || boundRight.Type == _baseModule.Error)
    //     {
    //         // Some operands have an error. So don't type-check them
    //         // be silent and wrap in an error expression.
    //         return new BoundErrorExpr(recoveredExprs: [boundLeft, boundRight],
    //             type: _baseModule.Error, syntax);
    //     }
    //     
    //     // Equality type-checks everything
    //     return new BoundEqualityComparison(boundLeft, boundRight,
    //         kind: syntax.Operator.Kind switch
    //         {
    //             TokenKind.DoubleEqual => EqualityComparisonKind.Equals,
    //             TokenKind.BangEqual => EqualityComparisonKind.NotEquals,
    //             _ => throw new UnreachableException()
    //         },
    //         type: _baseModule.Bool, 
    //         syntax);
    // }
    //
    // private BoundExpr BindBooleanOperator(BinaryExprSyntax syntax)
    // {
    //     Debug.Assert(syntax.Operator.Kind is TokenKind.AndKw or TokenKind.OrKw);
    //
    //     var boundLeft = BindExpr(syntax.Left);
    //     var boundRight = BindExpr(syntax.Right);
    //     if (boundLeft.Type == _baseModule.Error || boundRight.Type == _baseModule.Error)
    //     {
    //         // Some operands have an error. So don't type-check them
    //         // be silent and wrap in an error expression.
    //         return new BoundErrorExpr(recoveredExprs: [boundLeft, boundRight],
    //             type: _baseModule.Error, syntax);
    //     }
    //
    //     // Type-check against bool
    //     if (!CheckTypeAndReportMismatch(boundLeft, _baseModule.Bool) ||
    //         !CheckTypeAndReportMismatch(boundRight, _baseModule.Bool))
    //     {
    //         return new BoundErrorExpr(recoveredExprs: [boundLeft, boundRight],
    //             type: _baseModule.Error, syntax);
    //     }
    //
    //     if (syntax.Operator.Kind is TokenKind.AndKw)
    //         return new BoundAnd(boundLeft, boundRight, _baseModule.Bool, syntax);
    //     if (syntax.Operator.Kind is TokenKind.OrKw)
    //         return new BoundOr(boundLeft, boundRight, _baseModule.Bool, syntax);
    //
    //     throw new UnreachableException();
    // }
    //
    // private BoundExpr BindNativeOperator(Token operatorToken, SyntaxNode syntax, params IEnumerable<ExprSyntax> operands)
    // {
    //     var boundOperands = operands
    //         .Select(expr => BindExpr(expr))
    //         .ToImmutableArray();
    //     
    //     var operandTypes = boundOperands
    //         .Select(expr => expr.Type)
    //         .ToImmutableArray();
    //     if (operandTypes.Any(type => type == _baseModule.Error))
    //     {
    //         // Some operands have an error. So don't type-check them
    //         // be silent and wrap in an error expression.
    //         return new BoundErrorExpr(recoveredExprs: boundOperands,
    //             type: _baseModule.Error, syntax);
    //     }
    //
    //     var nativeOperator = _baseModule.TryGetNativeOperator(
    //         operatorToken.Kind,
    //         operandTypes);
    //     
    //     if (nativeOperator is null)
    //     {
    //         _diagnostics.ReportError(
    //             new Diagnostic.UndefinedOperator(operatorToken, [.. boundOperands.Select(expr => expr.Type)], syntax));
    //         return new BoundErrorExpr(recoveredExprs: [.. boundOperands],
    //             type: _baseModule.Error, syntax);
    //     }
    //
    //     return new BoundNativeOperator(nativeOperator, [.. boundOperands], nativeOperator.ReturnType, syntax);
    // }
    //
    #endregion
    
    #region Blocks, Control Flow
    
    private BoundBlock BindBlock(BlockExprSyntax syntax)
    {
        _scope = new Scope(parent: _scope);
        var stmts = syntax.Stmts.Select(BindStmt).ToImmutableArray();
        _scope = _scope.Parent!;
    
        return new BoundBlock(stmts, type: _baseModule.Unit, syntax);
    }

    private BoundExpr BindCondition(ExprSyntax syntax)
    {
        var condition = BindExpr(syntax);
        if (!CheckTypeAndReportMismatch(condition, expected: _baseModule.Bool))
            condition = new BoundErrorExpr(recoveredExprs: [condition], type: _baseModule.Error, syntax);

        return condition;
    }
    
    private BoundExpr BindIfExpr(IfExprSyntax syntax)
    {
        var condition = BindCondition(syntax.Condition);
        var body = BindExpr(syntax.Body);
        var @else = BindElseExpr(syntax);

        var type = body.Type;
        if (!IsAssignableTo(source: @else.Type, target: body.Type))
        {
            _diagnostics.ReportError(new Diagnostic.IncompatibleBranches(body, @else));
            type = _baseModule.Error;
        }
        
        return new BoundIfExpr(condition, body, @else, type, syntax);
    }

    private BoundExpr BindElseExpr(IfExprSyntax ifSyntax)
    {
        var elseSyntax = ifSyntax.ElseBody;
        if (elseSyntax is null)
        {
            _diagnostics.ReportError(new Diagnostic.MissingElse(ifSyntax));
            return new BoundErrorExpr([], _baseModule.Error, ifSyntax);
        }

        return BindExpr(elseSyntax);
    }

    private BoundStmt BindIfStmt(IfExprSyntax syntax)
    {
        // If in stmt position does not require matching arm types
        // and always evaluates to unit.
        
        var condition = BindCondition(syntax.Condition);
        var body = BindExprStmt(syntax.Body);
        var @else = syntax.ElseBody is not null ? BindExprStmt(syntax.ElseBody) : null;
        
        return new BoundIfStmt(condition, body, @else, syntax);
    }
    
    private BoundExpr BindBreakOrContinue(ExprSyntax syntax)
    {
        Debug.Assert(syntax is BreakExprSyntax or ContinueExprSyntax);
        
        if (!_inLoop)
        {
            _diagnostics.ReportError(new Diagnostic.BreakOrContinueOutsideLoop(syntax));
            return new BoundErrorExpr(recoveredExprs: [], _baseModule.Error, syntax);
        }
    
        return syntax is BreakExprSyntax
            ? new BoundBreak(_baseModule.Never, syntax)
            : new BoundContinue(_baseModule.Never, syntax);
    }

    private BoundExpr BindReturn(ReturnExprSyntax syntax)
    {
        var expr = syntax.Expr is not null ? BindExpr(syntax.Expr) : null;

        // For now, we are on script scope. Thus, only allow
        // unit expressions or none.
        if (expr is not null)
            CheckTypeAndReportMismatch(expr, _baseModule.Unit);

        return new BoundReturn(expr, _baseModule.Never, syntax);
    }
    
    #endregion
}