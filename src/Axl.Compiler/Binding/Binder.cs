using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;
using Axl.Compiler.Text;

namespace Axl.Compiler.Binding;

public sealed class Binder
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly BaseModuleSymbol _baseModule;
    
    private Scope _scope;
    private bool _inLoop = false;
    
    /// <summary>
    /// We need to keep a mapping of resolved symbols to their location in
    /// text for the LSP. Searching the bound tree is not pragmatic, since
    /// it would force us to emit a bound tree that's very close to syntax
    /// which would confuse lowering.
    /// </summary>
    private readonly Dictionary<SourceLocation, Symbol> _resolvedSymbols = [];
    
    
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

        return new BoundFile(block, binder._diagnostics.Drain(), binder._resolvedSymbols.ToFrozenDictionary());
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
    
    private void AddResolvedSymbol(SourceLocation location, Symbol? symbol)
    {
        if (symbol is null) return;
        
        Debug.Assert(!_resolvedSymbols.ContainsKey(location));
        _resolvedSymbols.Add(location, symbol);
    }

    private Symbol? BindSymbol(IdNameSyntax syntax, Symbol? parent = null, SymbolKind? expectedKind = null)
    {
        if (syntax.Token.IsMissing)
            return null;

        var name = syntax.Token.Identifier;

        Symbol? symbol;
        switch (parent)
        {
            case null:
            {
                symbol = _scope.Lookup(name);

                if (symbol is null)
                    _diagnostics.ReportError(new Diagnostic.UndefinedName(syntax));
                break;
            }
            case ModuleOrTypeSymbol moduleOrType:
            {
                symbol = moduleOrType.LookupMember(name);

                if (symbol is null)
                    _diagnostics.ReportError(new Diagnostic.UndefinedMember(syntax, parent));
                break;
            }
            default:
                symbol = null;
                _diagnostics.ReportError(new Diagnostic.UndefinedMember(syntax, parent));
                break;
        }

        if (expectedKind is not null && symbol is not null && symbol.Kind != expectedKind)
        {
            _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(syntax, symbol, expectedKind.Value));
            return null;
        }
        
        AddResolvedSymbol(syntax.Location, symbol);
        return symbol;
    }

    
    #region Type names

    private TypeSymbol BindTypeName(TypeNameSyntax syntax)
    {
        var parts = syntax.Parts.ToImmutableArray();

        Symbol? current = null;
        Debug.Assert(parts.Length >= 1);
        for (var i = 0; i < parts.Length; i++)
        {
            current = BindSymbol(parts[i], parent: current,
                expectedKind: i == parts.Length - 1 ? SymbolKind.Type : null);
            if (current is null)
                return _baseModule.Error;
        }

        return (TypeSymbol)current!;
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
    
        var variable = new VariableSymbol(syntax.Name.Identifier, variableType);
        _scope.Declare(variable);
        AddResolvedSymbol(syntax.Name.Location, variable);
    
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

    
    private readonly record struct BoundInstanceMember(BoundExpr Expr, BoundSymbol Member);
    private readonly record struct BoundSymbol(SyntaxNode Syntax, Symbol Symbol);
    private union BoundExprOrSymbol(BoundSymbol, BoundExpr, BoundInstanceMember);

    private BoundExprOrSymbol BindExprOrSymbol(ExprSyntax syntax) => syntax switch
    {
        GetMemberExprSyntax getMemberExprSyntax => BindGetMember(getMemberExprSyntax),
        IdNameSyntax idNameSyntax => BindIdName(idNameSyntax),
        CallExprSyntax callExprSyntax => BindCall(callExprSyntax),
        
        // Strings and Literals
        NumberLiteralSyntax numberLiteralSyntax => BindNumberLiteral(numberLiteralSyntax),
        TrueLiteralSyntax => new BoundBoolLiteral(value: true, type: _baseModule.Bool, syntax),
        FalseLiteralSyntax => new BoundBoolLiteral(value: false, type: _baseModule.Bool, syntax),
        StringExprSyntax stringExprSyntax => BindString(stringExprSyntax),
        
        // Operators
        BinaryExprSyntax binaryExprSyntax => BindBinary(binaryExprSyntax),
        UnaryExprSyntax unaryExprSyntax => BindUnary(unaryExprSyntax),
        
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

    private BoundErrorExpr BindError(ErrorExprSyntax syntax)
    {
        var recovered = syntax.RecoverableNodes
            .Select(BindExpr)
            .ToImmutableArray();
        return new BoundErrorExpr(recovered, _baseModule.Error, syntax);
    }
    
    private BoundExpr BindExpr(ExprSyntax syntax)
    {
        var exprOrSymbol = BindExprOrSymbol(syntax);
        switch (exprOrSymbol)
        {
            case BoundExpr boundExpr:
                return boundExpr;
            
            case BoundSymbol(_, VariableSymbol variable):
                return new BoundVariableRef(variable, syntax);
            
            case BoundSymbol boundSymbol:
                _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(boundSymbol.Syntax, boundSymbol.Symbol, SymbolKind.Variable));
                return new BoundErrorExpr(recoveredExprs: [], _baseModule.Error, syntax);
            
            case BoundInstanceMember instanceMember:
                var member = instanceMember.Member;
                _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(member.Syntax, member.Symbol, SymbolKind.Variable));
                return new BoundErrorExpr(recoveredExprs: [], _baseModule.Error, syntax);
                
            default:
                throw new UnreachableException();
        }
    }

    #region Literals and Strings

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

        if (IsAssignableTo(boundExpr.Type, _baseModule.String))
            return new StringPart.Interpolation(boundExpr);
        
        // Try to find duck-typed ToString
        if (boundExpr.Type is ModuleOrTypeSymbol moduleOrType
            && moduleOrType.LookupFun("ToString", [boundExpr.Type]) is IntrinsicFunSymbol toStringFun
            && IsAssignableTo(toStringFun.ReturnType, _baseModule.String))
        {
            var conversion = new BoundCall(toStringFun, [boundExpr], toStringFun.ReturnType, syntax.Expr);
            return new StringPart.Interpolation(conversion);
        }
        
        // Could not convert to string
        _diagnostics.ReportError(new Diagnostic.CannotConvert(syntax.Expr, From: boundExpr.Type, To: _baseModule.String));
        return new StringPart.Interpolation(new BoundErrorExpr(recoveredExprs: [boundExpr], type: _baseModule.Error, syntax));
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
    
    #region Operator Exprs

    private BoundExpr BindBinary(BinaryExprSyntax syntax) => syntax.Operator.Kind switch
    {
        TokenKind.AndKw or TokenKind.OrKw => BindBooleanOperator(syntax),
        TokenKind.Equal => BindAssign(syntax),
        _ => BindBinaryOperator(syntax)
    };

    private BoundExpr BindBinaryOperator(BinaryExprSyntax syntax)
    {
        var left = BindExpr(syntax.Left);
        var right = BindExpr(syntax.Right);

        return BindOperatorCall(syntax.Operator.Text, [left, right], syntax);
    }
    
    private BoundExpr BindOperatorCall(ReadOnlySpan<char> operatorText, ImmutableArray<BoundExpr> arguments, SyntaxNode syntax)
    {
        Debug.Assert(arguments.Length >= 1);
        
        if (arguments.Any(arg => arg.Type == _baseModule.Error))
            return new BoundErrorExpr(arguments, _baseModule.Error, syntax);

        var fun = arguments[0].Type.LookupFun(operatorText.ToString(), [.. arguments.Select(arg => arg.Type)]);
        if (fun is null)
        {
            _diagnostics.ReportError(
                new Diagnostic.UndefinedOperator(operatorText.ToString(), [.. arguments.Select(arg => arg.Type)], syntax));
            return new BoundErrorExpr(arguments, _baseModule.Error, syntax);
        }

        return new BoundCall(fun, arguments, fun.ReturnType, syntax);
    }

    private BoundExpr BindUnary(UnaryExprSyntax syntax)
        => BindOperatorCall(syntax.Operator.Text, [BindExpr(syntax.Operand)], syntax);
    
    private BoundExpr BindBooleanOperator(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.AndKw or TokenKind.OrKw);
    
        var left = BindExpr(syntax.Left);
        var right = BindExpr(syntax.Right);
        if (left.Type == _baseModule.Error || right.Type == _baseModule.Error)
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new BoundErrorExpr(recoveredExprs: [left, right],
                type: _baseModule.Error, syntax);
        }
    
        // Type-check against bool
        if (!CheckTypeAndReportMismatch(left, _baseModule.Bool) ||
            !CheckTypeAndReportMismatch(right, _baseModule.Bool))
        {
            return new BoundErrorExpr(recoveredExprs: [left, right],
                type: _baseModule.Error, syntax);
        }
    
        if (syntax.Operator.Kind is TokenKind.AndKw)
            return new BoundAnd(left, right, _baseModule.Bool, syntax);
        if (syntax.Operator.Kind is TokenKind.OrKw)
            return new BoundOr(left, right, _baseModule.Bool, syntax);
    
        throw new UnreachableException();
    }
    
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
    
    #region Names, Assign, Call, GetMember
    
    private BoundExprOrSymbol BindIdName(IdNameSyntax syntax)
    {
        if (BindSymbol(syntax) is { } symbol)
            return new BoundSymbol(syntax, symbol);

        return new BoundErrorExpr([], _baseModule.Error, syntax);
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
    
        var symbol = BindSymbol(idNameSyntax);
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

    private BoundExprOrSymbol BindGetMember(GetMemberExprSyntax syntax)
    {
        var left = BindExprOrSymbol(syntax.Left);
        if (left is BoundInstanceMember instMember)
        {
            _diagnostics.ReportError(
                new Diagnostic.UndefinedMember(syntax.Member, Symbol: instMember.Member.Symbol));
        }

        return left switch
        {
            BoundSymbol(var variableSyntax, VariableSymbol variable)
                => BindInstanceMember(new BoundVariableRef(variable, variableSyntax)),

            BoundSymbol symbol
                => BindSymbol(syntax.Member, parent: symbol.Symbol) is { } member
                    ? new BoundSymbol(syntax.Member, member)
                    : new BoundErrorExpr([], _baseModule.Error, syntax),

            BoundExpr expr => BindInstanceMember(expr),

            BoundInstanceMember instanceMember
                => new BoundErrorExpr([instanceMember.Expr], _baseModule.Error, syntax)
        };
        
        BoundExprOrSymbol BindInstanceMember(BoundExpr expr)
        {
            var type = expr.Type;
            if (type == _baseModule.Error)
                return new BoundErrorExpr([expr], _baseModule.Error, syntax.Member);

            var member = BindSymbol(syntax.Member, parent: type);
            if (member is null)
                return new BoundErrorExpr([expr], _baseModule.Error, syntax);
            var boundMember = new BoundSymbol(syntax.Member, member);

            return new BoundInstanceMember(expr, boundMember);
        }
    }

    
    private union BoundCallee(IntrinsicFunSymbol, MethodCall);

    private readonly record struct MethodCall(BoundExpr Expr, IntrinsicFunSymbol MemberFun);
    
    private BoundExpr BindCall(CallExprSyntax syntax)
    {
        var arguments = syntax.ArgumentExprs.Select(BindExpr).ToImmutableArray();
        
        // Bind Callee
        var callee = BindCallee(syntax.Callee);
        if (callee is null || arguments.Any(arg => arg.Type == _baseModule.Error))
            return new BoundErrorExpr(recoveredExprs: [..arguments], _baseModule.Error, syntax);

        var fun = (BoundCallee)callee! switch
        {
            IntrinsicFunSymbol funSymbol => funSymbol, 
            MethodCall(_, var memberFun) => memberFun
        };

        if (callee is MethodCall methodCallee)
            arguments = [methodCallee.Expr, .. arguments];
        
        // Check arity
        if (arguments.Length != fun.ParameterTypes.Length)
        {
            _diagnostics.ReportError(new Diagnostic.ArityMismatch(syntax.Children.FirstOfType<ArgListSyntax>(),
                fun, Got: arguments.Length, IsMethodCall: callee is MethodCall));
            return new BoundErrorExpr(recoveredExprs: arguments, _baseModule.Error, syntax);
        }

        // Check parameter types
        var hadError = false;
        for (var i = 0; i < arguments.Length; i++)
        {
            if (!CheckTypeAndReportMismatch(arguments[i], fun.ParameterTypes[i]))
                hadError = true;
        }
        if (hadError)
            return new BoundErrorExpr(recoveredExprs: arguments, _baseModule.Error, syntax);

        return new BoundCall(fun, arguments, fun.ReturnType, syntax);
    }

    private BoundCallee? BindCallee(ExprSyntax syntax)
    {
        var callee = BindExprOrSymbol(syntax);
        switch (callee)
        {
            case BoundSymbol boundSymbol:
            {
                if (boundSymbol.Symbol is not IntrinsicFunSymbol funSymbol)
                {
                    _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(boundSymbol.Syntax, boundSymbol.Symbol,
                        SymbolKind.Fun));
                    return null;
                }

                return funSymbol;
            }
            
            case BoundInstanceMember instanceMember:
            {
                if (instanceMember.Member.Symbol is not IntrinsicFunSymbol funSymbol)
                {
                    _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(instanceMember.Member.Syntax, instanceMember.Member.Symbol, SymbolKind.Fun));
                    return null;
                }

                if (funSymbol.ParameterTypes.Length == 0 ||
                    !IsAssignableTo(instanceMember.Expr.Type, funSymbol.ParameterTypes[0]))
                {
                    _diagnostics.ReportError(new Diagnostic.CannotCallAsMethod(MethodSyntax: instanceMember.Member.Syntax, 
                        MethodSymbol: funSymbol,
                        ExprType: instanceMember.Expr.Type));
                    return null;
                }

                return new MethodCall(instanceMember.Expr, funSymbol);
            }
            
            case BoundErrorExpr:
                return null;
            
            case BoundExpr:
                _diagnostics.ReportError(new Diagnostic.InvalidCallee(syntax));
                return null;
            
            default:
                throw new UnreachableException();
        }
    }

    #endregion
}