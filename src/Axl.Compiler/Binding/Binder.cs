using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
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
        
        // Forward-declare all fun symbols
        binder.BindFunSymbols(syntax.Children.OfType<FunDeclSyntax>());
        
        // Everything other than stmts and fun decls is not supported yet.
        foreach (var node in syntax.SyntaxNodes().Where(n => n is not (StmtSyntax or FunDeclSyntax)))
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
        if (source is ErrorTypeSymbol || target is ErrorTypeSymbol) return true;
        
        // Never assigns to anything
        if (source == _baseModule.Never) return true;

        return source == target;
    }

    private FunSymbol? LookupMethod(TypeSymbol instanceType, string name, ImmutableArray<TypeSymbol> argumentTypes)
    {
        var symbol = instanceType.LookupMember(name);
        var fun = symbol switch
        {
            FunSymbol funSymbol => funSymbol,
            FunGroupSymbol funGroupSymbol => funGroupSymbol.LookupFun(instanceType, argumentTypes),
            _ => null
        };
        
        if (fun is { ReceiverType: not null } &&
            IsAssignableTo(instanceType, fun.ReceiverType) &&
            fun.ParameterTypes.SequenceEqual(argumentTypes))
        {
            return fun;
        }

        return null;
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
        
        // Fun groups are resolved during callee binding.
        if (symbol is not FunGroupSymbol)
            AddResolvedSymbol(syntax.Location, symbol);
        return symbol;
    }

    
    #region Declarations

    private void BindFunSymbols(IEnumerable<FunDeclSyntax> syntaxes)
    {
        // Declare funs with a valid name and group them if necessary.
        // Funs with empty names cannot be referenced from source and
        // would just clutter lookup.
        
        var symbols = syntaxes
            .Select(BindFunSymbol)
            .Where(fun => fun.Name.Length > 0)
            .GroupBy(fun => fun.Name)
            .Select(funGrp => (Symbol)(funGrp.ToImmutableArray() switch
            {
                [] => throw new UnreachableException(),
                [var single] => single,
                var multiple => new FunGroupSymbol(funGrp.Key, multiple)
            }))
            .ToImmutableArray();

        // Report errors for duplicate signatures
        foreach (var funGroup in symbols.OfType<FunGroupSymbol>())
        {
            var remaining = funGroup.Funs.ToList();
            while (remaining.Count > 0)
            {
                var first = remaining[0];
                var sameSignature = remaining
                    .Where(fun => fun.ReceiverType == first.ReceiverType &&
                                  fun.ParameterTypes.SequenceEqual(first.ParameterTypes))
                    .ToImmutableArray();
                if (sameSignature.Length > 1)
                {
                    // Errors in the signature should not report another error.
                    var duplicatesToReport = sameSignature.Where(fun =>
                            fun.ParameterTypes.All(paramType => paramType is not ErrorTypeSymbol))
                        .ToImmutableArray();
                    
                    if (duplicatesToReport.Length > 1)
                        _diagnostics.ReportError(new Diagnostic.DuplicateFunDeclarations(sameSignature));
                }

                remaining.RemoveAll(sameSignature.Contains);
            }
        }

        // Declare symbols
        foreach (var symbol in symbols)
            _scope.Declare(symbol);
    }

    private FunSymbol BindFunSymbol(FunDeclSyntax syntax)
    {
        var parameters = syntax.Parameters
            .Select(paramSyntax =>
                new ParameterSymbol(paramSyntax.Name.Identifier, 
                    BindTypeName(paramSyntax.TypeAnnotation),
                    paramSyntax))
            .ToImmutableArray();
        var returnType = syntax.ReturnTypeAnnotation is not null
            ? BindTypeName(syntax.ReturnTypeAnnotation)
            : _baseModule.Unit;
        
        // Report duplicate parameter errors only on parameters
        // with a non-empty name.
        var duplicateParamGroups = parameters
            .GroupBy(param => param.Name)
            .Select(grp => grp.ToImmutableArray())
            .Where(grp => grp.Length > 1 && grp[0].Name.Length > 0);
        foreach (var duplicateGroup in duplicateParamGroups)
        {
            _diagnostics.ReportError(new Diagnostic.DuplicateParameters(duplicateGroup));
        }

        var funSymbol = new FunSymbol(syntax.Name.Identifier,
            receiverType: null,
            parameters: parameters, 
            returnType: returnType, 
            declarationSyntax: syntax);
        
        AddResolvedSymbol(syntax.Name.Location, funSymbol);
        return funSymbol;
    }
    
    #endregion
    
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
                return ErrorTypeSymbol.Instance;
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
    
    private BoundStmt BindVarDecl(VarDeclSyntax syntax)
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

        // Accept shadowing only if it's another variable.
        var lookupResult = _scope.LookupHere(syntax.Name.Identifier);
        if (lookupResult is not (null or VariableSymbol))
        {
            _diagnostics.ReportError(new Diagnostic.CannotShadow(lookupResult, syntax.Name));
            return new BoundErrorExpr([], syntax);
        }
        
        var isReadOnly = syntax.VarOrLetKwToken.Kind is TokenKind.LetKw;
        var variable = new VariableSymbol(syntax.Name.Identifier, isReadOnly, variableType);
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
            return new BoundErrorExpr(recoveredExprs: [], syntax: syntax);    
        }
    
        return BindExpr(syntax.Initializer);
    }
    
    private BoundStmt BindWhile(WhileStmtSyntax syntax)
    {
        var condition = BindCondition(syntax.Condition);

        var previousInLoop = _inLoop;
        _inLoop = true;
        var body = BindExpr(syntax.Body);
        _inLoop = previousInLoop;
        
        return new BoundWhile(condition, body, syntax);
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
        TrueLiteralSyntax => new BoundConst(value: true, type: _baseModule.Bool, syntax),
        FalseLiteralSyntax => new BoundConst(value: false, type: _baseModule.Bool, syntax),
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
        return new BoundErrorExpr(recovered, syntax);
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
                return new BoundErrorExpr(recoveredExprs: [], syntax: syntax);
            
            case BoundInstanceMember instanceMember:
                var member = instanceMember.Member;
                _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(member.Syntax, member.Symbol, SymbolKind.Variable));
                return new BoundErrorExpr(recoveredExprs: [], syntax: syntax);
                
            default:
                throw new UnreachableException();
        }
    }

    #region Literals and Strings

    private BoundStringExpr BindString(StringExprSyntax syntax)
    {
        var parts = syntax.Parts.Select(BindStringPart).ToImmutableArray();
    
        if (parts.Length == 0)
            parts = [new BoundConst("", _baseModule.String, syntax)];
        
        return new BoundStringExpr(parts, _baseModule.String, syntax);
    }
    
    private BoundExpr BindStringPart(StringPartSyntax syntax)
        => syntax switch
        {
            StringTextSyntax textSyntax => new BoundConst(textSyntax.TextToken.ProcessedText, _baseModule.String, syntax),
            StringInterpolationSyntax interpolationSyntax => BindStringInterpolation(interpolationSyntax),
        };
    
    private BoundExpr BindStringInterpolation(StringInterpolationSyntax syntax)
    {
        if (syntax.Expr is null)
        {
            // Empty interpolation means nothing will be added, which is equivalent
            // to an empty text const.
            return new BoundConst("", _baseModule.String, syntax);
        }
                
        var boundExpr = BindExpr(syntax.Expr);

        if (IsAssignableTo(boundExpr.Type, _baseModule.String))
            return boundExpr;
        
        // Try to find duck-typed ToString
        if (LookupMethod(boundExpr.Type, "ToString", []) is FunSymbol toStringFun
            && IsAssignableTo(toStringFun.ReturnType, _baseModule.String))
        {
            return new BoundCall(toStringFun, receiver: boundExpr, arguments: [], syntax.Expr);
        }
        
        // Could not convert to string
        _diagnostics.ReportError(new Diagnostic.CannotConvert(syntax.Expr, From: boundExpr.Type, To: _baseModule.String));
        return new BoundErrorExpr(recoveredExprs: [boundExpr], syntax: syntax);
    }

    

    private BoundExpr BindNumberLiteral(NumberLiteralSyntax syntax)
    {
        var type = syntax.Token.Suffix switch
        {
            NumberLiteralSuffix.I32 => _baseModule.I32,
            NumberLiteralSuffix.I64 => _baseModule.I64,
            NumberLiteralSuffix.F32 => _baseModule.F32,
            NumberLiteralSuffix.F64 => _baseModule.F64,
    
            _ => syntax.Token.HasDecimalPoint ? _baseModule.DefaultFloatType : _baseModule.DefaultIntType
        };
        
        // Literals like "1.1i32" need to be rejected.
        if (syntax.Token.HasDecimalPoint &&
            type != _baseModule.F32 && type != _baseModule.F64)
        {
            _diagnostics.ReportError(new Diagnostic.SuffixInvalidForDecimalNumber(syntax));
            return new BoundErrorExpr([], syntax);
        }

        if (type == _baseModule.I32)
        {
            if (!int.TryParse(syntax.Token.Body, out var value))
            {
                // The number literal is too big to fit inside an int/i32.
                _diagnostics.ReportError(new Diagnostic.NumberTooBig(syntax, type));
                return new BoundErrorExpr([], syntax);
            }

            return new BoundConst(value, type, syntax);
        }

        if (type == _baseModule.I64)
        {
            if (!long.TryParse(syntax.Token.Body, out var value))
            {
                // The number literal is too big to fit inside an long/i64.
                _diagnostics.ReportError(new Diagnostic.NumberTooBig(syntax, type));
                return new BoundErrorExpr([], syntax);
            }

            return new BoundConst(value, type, syntax);
        }

        if (type == _baseModule.F32)
        {
            var value = float.Parse(syntax.Token.Body, CultureInfo.InvariantCulture);
            return new BoundConst(value, type, syntax);
        }
        
        if (type == _baseModule.F64)
        {
            var value = double.Parse(syntax.Token.Body, CultureInfo.InvariantCulture);
            return new BoundConst(value, type, syntax);
        }

        throw new UnreachableException();
    }
    
    #endregion
    
    #region Operator Exprs

    private BoundExpr BindUnary(UnaryExprSyntax syntax)
        => BindOperatorCall(syntax.Operator.Text.ToString(), receiver: BindExpr(syntax.Operand), arguments: [], syntax);
    
    private BoundExpr BindBinary(BinaryExprSyntax syntax)
    {
        if (syntax.Operator.Kind is TokenKind.DoubleAmpersand or TokenKind.DoubleVerticalBar)
            return BindBooleanOperator(syntax);
        if (syntax.Operator.Kind == TokenKind.Equal)
            return BindAssign(syntax);
        
        var left = BindExpr(syntax.Left);
        var right = BindExpr(syntax.Right);

        return BindOperatorCall(syntax.Operator.Text.ToString(), receiver: left, arguments: [right], syntax);
    }
    
    private BoundExpr BindOperatorCall(string operatorName, BoundExpr receiver, ImmutableArray<BoundExpr> arguments, SyntaxNode syntax)
    {
        if (receiver.Type is ErrorTypeSymbol ||
            arguments.Any(arg => arg.Type is ErrorTypeSymbol))
        {
            return new BoundErrorExpr(arguments, syntax);
        }

        var operatorFun = LookupMethod(receiver.Type, operatorName, [.. arguments.Select(expr => expr.Type)]);
        if (operatorFun is null)
        {
            _diagnostics.ReportError(
                new Diagnostic.UndefinedOperator(operatorName,
                    [receiver.Type, .. arguments.Select(arg => arg.Type)], syntax));
            return new BoundErrorExpr(arguments, syntax);
        }

        return new BoundCall(operatorFun, receiver, arguments, syntax);
    }

    private BoundExpr BindBooleanOperator(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.DoubleAmpersand or TokenKind.DoubleVerticalBar);
    
        var left = BindExpr(syntax.Left);
        var right = BindExpr(syntax.Right);
        if (left.Type is ErrorTypeSymbol || right.Type is ErrorTypeSymbol)
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new BoundErrorExpr(recoveredExprs: [left, right], syntax: syntax);
        }
    
        // Type-check against bool
        if (!CheckTypeAndReportMismatch(left, _baseModule.Bool) ||
            !CheckTypeAndReportMismatch(right, _baseModule.Bool))
        {
            return new BoundErrorExpr(recoveredExprs: [left, right], syntax: syntax);
        }
    
        if (syntax.Operator.Kind is TokenKind.DoubleAmpersand)
            return new BoundAnd(left, right, _baseModule.Bool, syntax);
        if (syntax.Operator.Kind is TokenKind.DoubleVerticalBar)
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
            condition = new BoundErrorExpr(recoveredExprs: [condition], syntax: syntax);

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
            type = ErrorTypeSymbol.Instance;
        }
        
        return new BoundIfExpr(condition, body, @else, type, syntax);
    }

    private BoundExpr BindElseExpr(IfExprSyntax ifSyntax)
    {
        var elseSyntax = ifSyntax.ElseBody;
        if (elseSyntax is null)
        {
            _diagnostics.ReportError(new Diagnostic.MissingElse(ifSyntax));
            return new BoundErrorExpr([], ifSyntax);
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
            return new BoundErrorExpr(recoveredExprs: [], syntax: syntax);
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

        return new BoundErrorExpr([], syntax);
    }
    
    private BoundExpr BindAssign(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.Equal);
        
        var value = BindExpr(syntax.Right);
        var target = BindAssignTarget(syntax.Left);
        
        if (target is null)
            return new BoundErrorExpr(recoveredExprs: [value], syntax: syntax);
        
        if (target.Type is ErrorTypeSymbol || value.Type is ErrorTypeSymbol)
            return new BoundAssign(target, value, value.Type, syntax);
        
        var type = CheckTypeAndReportMismatch(value, target.Type)
            ? _baseModule.Unit
            : ErrorTypeSymbol.Instance;
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
            case VariableSymbol { IsReadOnly: false } variable:
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
                    : new BoundErrorExpr([], syntax),

            BoundExpr expr => BindInstanceMember(expr),

            BoundInstanceMember instanceMember
                => new BoundErrorExpr([instanceMember.Expr], syntax)
        };
        
        BoundExprOrSymbol BindInstanceMember(BoundExpr expr)
        {
            var type = expr.Type;
            if (type is ErrorTypeSymbol)
                return new BoundErrorExpr([expr], syntax.Member);

            var member = BindSymbol(syntax.Member, parent: type);
            if (member is null)
                return new BoundErrorExpr([expr], syntax);
            var boundMember = new BoundSymbol(syntax.Member, member);

            return new BoundInstanceMember(expr, boundMember);
        }
    }

    private readonly record struct BoundCallee(FunSymbol Fun, BoundExpr? Receiver);
    
    private BoundExpr BindCall(CallExprSyntax syntax)
    {
        var arguments = syntax.ArgumentExprs.Select(BindExpr).ToImmutableArray();
        
        // Bind Callee
        if (BindCallee(syntax.Callee, [.. arguments.Select(arg => arg.Type)]) is not { } callee || 
            arguments.Any(arg => arg.Type is ErrorTypeSymbol))
        {
            return new BoundErrorExpr(recoveredExprs: [.. arguments], syntax: syntax);
        }

        // Check arity
        if (arguments.Length != callee.Fun.Parameters.Length)
        {
            _diagnostics.ReportError(new Diagnostic.ArityMismatch(syntax.Children.FirstOfType<ArgListSyntax>(),
                callee.Fun, Got: arguments.Length));
            return new BoundErrorExpr(recoveredExprs: arguments, syntax: syntax);
        }

        // Check parameter types
        var hadError = false;
        for (var i = 0; i < arguments.Length; i++)
        {
            if (!CheckTypeAndReportMismatch(arguments[i], callee.Fun.Parameters[i].Type))
                hadError = true;
        }
        if (hadError)
            return new BoundErrorExpr(recoveredExprs: arguments, syntax: syntax);

        return new BoundCall(callee.Fun, callee.Receiver, arguments, syntax);
    }

    private BoundCallee? BindCallee(ExprSyntax syntax, ImmutableArray<TypeSymbol> argumentTypes)
    {
        var callee = BindExprOrSymbol(syntax);
        if (callee is BoundErrorExpr)
            return null;
        if (callee is BoundExpr)
        {
            _diagnostics.ReportError(new Diagnostic.InvalidCallee(syntax));
            return null;
        }

        var (receiver, symbol, symbolRefSyntax) = callee switch
        {
            BoundSymbol boundSymbol => (null, boundSymbol.Symbol, boundSymbol.Syntax),
            BoundInstanceMember instanceMember => (instanceMember.Expr, instanceMember.Member.Symbol, instanceMember.Member.Syntax),
            _ => throw new UnreachableException()
        };

        var funSymbol = symbol as FunSymbol;
        if (symbol is FunGroupSymbol funGroupSymbol)
        {
            var candidates = funGroupSymbol.Funs
                .Where(fun => fun.ReceiverType == receiver?.Type &&
                              fun.ParameterTypes.SequenceEqual(argumentTypes))
                .ToImmutableArray();
            if (candidates.Length != 1)
            {
                _diagnostics.ReportError(new Diagnostic.CannotResolveFun(funGroupSymbol, argumentTypes, syntax));
                return null;
            }

            funSymbol = candidates[0];
            AddResolvedSymbol(symbolRefSyntax.Location, funSymbol);
        }
        
        if (funSymbol is null)
        {
            _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(symbolRefSyntax, symbol, SymbolKind.Fun));
            return null;
        }

        var compatible = receiver is null && funSymbol.ReceiverType is null ||
                         receiver is not null && funSymbol.ReceiverType is not null &&
                         IsAssignableTo(receiver.Type, funSymbol.ReceiverType);
        if (!compatible)
        {
            if (funSymbol.ReceiverType is not null)
                _diagnostics.ReportError(new Diagnostic.CannotCallWithoutReceiver(funSymbol, syntax));
            else 
                _diagnostics.ReportError(new Diagnostic.CannotCallWithReceiver(funSymbol, syntax));
            return null;
        }
        
        return new BoundCallee(funSymbol, receiver);
    }

    #endregion
}