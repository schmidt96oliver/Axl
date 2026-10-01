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
    private readonly FunSymbol _fun;
    private readonly Dictionary<FunDeclSyntax, FunSymbol> _funSymbolsByDecl = [];
    private bool _inLoop = false;
    
    /// <summary>
    /// We need to keep a mapping of resolved symbols to their location in
    /// text for the LSP. Searching the bound tree is not pragmatic, since
    /// it would force us to emit a bound tree that's very close to syntax
    /// which would confuse lowering.
    /// </summary>
    private readonly Dictionary<SourceLocation, Symbol> _resolvedSymbols = [];
    
    
    private Binder(Scope scope, FunSymbol fun, BaseModuleSymbol baseModule)
    {
        _baseModule = baseModule;

        _scope = scope;
        _fun = fun;
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
        var scriptFun = new FunSymbol("", null, [], baseModule.Unit);
        
        var scope = new Scope(parent: CreateGlobalScope(baseModule));
        var binder = new Binder(scope, scriptFun, baseModule);
        
        // Forward-declare all fun symbols
        var block = binder.BindBlock(syntax);
        
        //TODO: Move to BindFunBody, so that everything follows the same logic.
        var returnStmt = new BoundReturn(null);
        block = new BoundBlock([..block.Stmts, returnStmt], block.LocalFuns, block.Type, block.Syntax);
        scriptFun.Body = block;
        
        return new BoundFile(scriptFun, binder._diagnostics.Drain(), binder._resolvedSymbols.ToFrozenDictionary());
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
        if (source is NeverTypeSymbol) return true;

        return source == target;
    }

    private FunSymbol? LookupMethod(TypeSymbol instanceType, string name, ImmutableArray<TypeSymbol> argumentTypes)
    {
        if (argumentTypes.OfType<ErrorTypeSymbol>().Any())
            return null;
        
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


    private void BindFunBody(FunDeclSyntax funDecl)
    {
        var funSymbol = _funSymbolsByDecl[funDecl];
        
        var funScope = new Scope(parent: _scope);
        foreach (var param in funSymbol.Parameters)
            funScope.Declare(param);

        var funBinder = new Binder(funScope, funSymbol, _baseModule);

        var body = BindFunBodyBlock(funSymbol, funBinder);
        
        // Check that all code-paths return a value
        if (!body.IsDiverging)
        {
            if (funSymbol.ReturnType == _baseModule.Unit)
            {
                // For unit return type, insert an empty return at the end.
                var returnStmt = new BoundReturn(null);
                body = new BoundBlock([.. body.Stmts, returnStmt], body.LocalFuns, body.Type, body.Syntax);
            }
            else
            {
                _diagnostics.ReportError(new Diagnostic.MissingReturn(body.Syntax!, funDecl.ReturnTypeAnnotation!));
            }
        }
        
        funSymbol.Body = body;
        
        // Transfer state to current binder
        funBinder._diagnostics.DrainInto(_diagnostics);
        foreach (var resolvedSymbol in funBinder._resolvedSymbols)
            _resolvedSymbols.Add(resolvedSymbol.Key, resolvedSymbol.Value);
    }

    private BoundBlock BindFunBodyBlock(FunSymbol fun, Binder funBinder)
    {
        var syntax = fun.DeclarationSyntax ??
                     throw new ArgumentException($"{nameof(fun)} must be code-declared.", nameof(fun));
        if (syntax.Body.Expr is null)
        {
            // The fun has no body at all, so it returns unit.
            throw new NotImplementedException("Funs without body not supported yet.");
        }

        if (syntax.Body.IsExpressionBodied)
        {
            var expr = funBinder.BindExpr(syntax.Body.Expr);
            if (!CheckTypeAndReportMismatch(expr, fun.ReturnType))
                expr = new BoundErrorExpr([expr], syntax.Body.Expr);
            
            return new BoundBlock([new BoundReturn(expr)], [], _baseModule.Unit);
        }

        if (syntax.Body.Expr is not BlockExprSyntax blockSyntax)
            throw new UnreachableException();
        return funBinder.BindBlock(blockSyntax);
    }


    #region Declarations

    private ImmutableArray<FunSymbol> BindFunSymbols(IEnumerable<FunDeclSyntax> syntaxes)
    {
        // Declare funs with a valid name and group them if necessary.
        // Funs with empty names cannot be referenced from source and
        // would just clutter lookup.

        var funs = syntaxes.Select(BindFunSymbol).ToImmutableArray();
        
        var groupsOrSingleByName = funs
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
        foreach (var funGroup in groupsOrSingleByName.OfType<FunGroupSymbol>())
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
        foreach (var symbol in groupsOrSingleByName)
            _scope.Declare(symbol);

        return funs;
    }

    private FunSymbol BindFunSymbol(FunDeclSyntax syntax)
    {
        var parameters = syntax.Parameters
            .Select(BindParameter)
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
        
        _funSymbolsByDecl.Add(syntax, funSymbol);
        AddResolvedSymbol(syntax.Name.Location, funSymbol);
        return funSymbol;
    }

    private ParameterSymbol BindParameter(ParamSyntax syntax)
    {
        var symbol = new ParameterSymbol(syntax.Name.Identifier,
            BindTypeName(syntax.TypeAnnotation),
            syntax);
        AddResolvedSymbol(syntax.Name.Location, symbol);
        return symbol;
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
        var variable = new VariableSymbol(syntax.Name.Identifier, isReadOnly, variableType, _fun);
        _scope.Declare(variable);
        AddResolvedSymbol(syntax.Name.Location, variable);
    
        return new BoundVarDecl(variable, boundInitializer, syntax);
    }
    
    private BoundExpr BindVarDeclInitializer(VarDeclSyntax syntax)
    {
        if (syntax.Initializer is null)
        {
            _diagnostics.ReportError(new Diagnostic.MissingInitializer(syntax));
            return new BoundErrorExpr(recoveredExprs: []);    
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
    
    private BoundBlock BindBlock(SyntaxNode syntax)
    {
        _scope = new Scope(parent: _scope);

        // Forward-declare all fun symbols
        var funs = BindFunSymbols(syntax.Children.OfType<FunDeclSyntax>());

        var stmts = ImmutableArray.CreateBuilder<BoundStmt>();
        foreach (var node in syntax.SyntaxNodes())
        {
            switch (node)
            {
                case StmtSyntax stmt:
                    stmts.Add(BindStmt(stmt));
                    break;
                case FunDeclSyntax funDecl:
                    BindFunBody(funDecl);
                    break;

                default:
                    _diagnostics.ReportError(new Diagnostic.UnsupportedFeature(node));
                    break;
            }
        }
        
        _scope = _scope.Parent!;
        return new BoundBlock(stmts.DrainToImmutable(), funs, type: _baseModule.Unit, syntax);
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

        // In some cases (e.g. Never type), one branch is assignable to
        // the other but not vice versa. Pick the one that works.
        TypeSymbol type;
        if (IsAssignableTo(source: @else.Type, target: body.Type))
            type = body.Type;
        else if (IsAssignableTo(source: body.Type, target: @else.Type))
            type = @else.Type;
        else
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
            ? new BoundBreak(syntax)
            : new BoundContinue(syntax);
    }

    private BoundExpr BindReturn(ReturnExprSyntax syntax)
    {
        var expr = syntax.Expr is not null ? BindExpr(syntax.Expr) : null;

        if (expr is not null)
        {
            if (!CheckTypeAndReportMismatch(expr, _fun.ReturnType))
                expr = new BoundErrorExpr([expr], syntax.Expr);
        }
        else if (_fun.ReturnType != _baseModule.Unit)
        {
            _diagnostics.ReportError(new Diagnostic.MissingReturnValue(syntax));
            expr = new BoundErrorExpr([]);
        }

        return new BoundReturn(expr, syntax);
    }
    
    #endregion
    
    #region Names, Assign, Call, GetMember
    
    private BoundExprOrSymbol BindIdName(IdNameSyntax syntax)
    {
        if (BindSymbol(syntax) is not { } symbol)
            return new BoundErrorExpr([], syntax);

        // Reject captures variables
        if (symbol is VariableSymbol variable && variable.Owner != _fun)
        {
            _diagnostics.ReportError(new Diagnostic.CannotCapture(syntax));
            return new BoundErrorExpr([], syntax);
        }
        
        return new BoundSymbol(syntax, symbol);
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
        var bound = BindExprOrSymbol(syntax);
        switch (bound)
        {
            case BoundSymbol(_, VariableSymbol { IsReadOnly: false } variable):
                return variable;
            case BoundSymbol(_, var symbol):
                _diagnostics.ReportError(new Diagnostic.InvalidAssignTarget(syntax, symbol));
                return null;

            case BoundInstanceMember(_, var (_, symbol)):
                _diagnostics.ReportError(new Diagnostic.InvalidAssignTarget(syntax, symbol));
                return null;
            
            case null:
            case BoundErrorExpr:
                return null;

            default:
                _diagnostics.ReportError(new Diagnostic.InvalidAssignTarget(syntax));
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

    private BoundExpr BindCall(CallExprSyntax syntax)
    {
        var arguments = syntax.ArgumentExprs.Select(BindExpr).ToImmutableArray();
        var callee = BindExprOrSymbol(syntax.Callee);
        if (callee is BoundExpr expr)
        {
            if (callee is not BoundErrorExpr)
                _diagnostics.ReportError(new Diagnostic.InvalidCallee(syntax.Callee));
            return new BoundErrorExpr(recoveredExprs: [expr, .. arguments], syntax);
        }

        var (calleeSymbol, receiver) = callee switch
        {
            BoundSymbol boundSymbol => (boundSymbol.Symbol, null),
            BoundInstanceMember instanceMember => (instanceMember.Member.Symbol, instanceMember.Expr),
            _ => throw new UnreachableException()
        };
        
        if (calleeSymbol is not (FunGroupSymbol or FunSymbol))
        {
            _diagnostics.ReportError(new Diagnostic.UnexpectedSymbolKind(syntax.Callee, calleeSymbol, SymbolKind.Fun));
            return new BoundErrorExpr(
                recoveredExprs: receiver is not null ? [receiver, .. arguments] : arguments,
                syntax);
        }

        var candidates = calleeSymbol switch
        {
            FunGroupSymbol group => group.Funs,
            FunSymbol fun => [fun],
            _ => throw new UnreachableException()
        };

        var funName = candidates[0].Name;
        Debug.Assert(candidates.All(fun => fun.Name == funName));

        return SelectCallee(candidates, receiver, arguments, funName, syntax) is { } selectedFun
            ? new BoundCall(selectedFun, receiver, arguments, syntax)
            : new BoundErrorExpr(
                recoveredExprs: receiver is not null ? [receiver, .. arguments] : arguments
                , syntax);
    }

    private FunSymbol? SelectCallee(ImmutableArray<FunSymbol> initialCandidates, BoundExpr? receiver, ImmutableArray<BoundExpr> arguments,
        string funName, CallExprSyntax callSyntax)
    {
        // Filter based on receiver 
        var candidates = initialCandidates
            .Where(AreReceiversCompatible)
            .ToList();
        if (candidates.Count == 0)
        {
            // Either all are static and called with a receiver or vice versa.
            if (initialCandidates[0].ReceiverType is not null)
            {
                _diagnostics.ReportError(new Diagnostic.CannotCallWithoutReceiver(funName, 
                    IsOverloaded: initialCandidates.Length > 1,
                    callSyntax.Callee));
            }
            else
            {
                _diagnostics.ReportError(new Diagnostic.CannotCallWithReceiver(funName,
                    IsOverloaded: initialCandidates.Length > 1,
                    callSyntax.Callee));
            }
            return null;
        }
        
        // Filter arity
        candidates.RemoveAll(fun => fun.Parameters.Length != arguments.Length);
        if (candidates.Count == 0)
        {
            _diagnostics.ReportError(new Diagnostic.ArityMismatch(callSyntax.ArgList,
                funName,
                ParameterCount: initialCandidates.Length == 1 ? initialCandidates[0].Parameters.Length : null,
                ArgumentCount: arguments.Length));
            return null;
        }

        // If argument have an error, only check arity and don't go deeper.
        // It cannot be resolved without valid types.
        if (arguments.Any(arg => arg.Type is ErrorTypeSymbol))
            return null;
        
        // Filter assignable arguments
        if (candidates.Count == 1)
        {
            // Only one candidate left, so report TypeMismatch errors.
            var hadError = false;
            for (var i = 0; i < arguments.Length; i++)
            {
                if (arguments[i].Type is ErrorTypeSymbol ||
                    !CheckTypeAndReportMismatch(arguments[i], candidates[0].Parameters[i].Type))
                {
                    hadError = true;
                }
            }
            if (hadError)
                return null;
        }
        else
        {
            // Multiple candidates to select from, so report CannotResolveFun.
            var candidatesWithMatchingArity = candidates.ToImmutableArray();
            candidates.RemoveAll(fun => Enumerable.Range(0, arguments.Length)
                .Any(i => !IsAssignableTo(arguments[i].Type, fun.ParameterTypes[i])));
            if (candidates.Count != 1)
            {
                _diagnostics.ReportError(new Diagnostic.CannotResolveFun(callSyntax.ArgList, funName,
                    candidatesWithMatchingArity, [.. arguments.Select(arg => arg.Type)]));
                return null;
            }
        }

        return candidates[0];

        bool AreReceiversCompatible(FunSymbol fun)
            => receiver is null && fun.ReceiverType is null ||
               receiver is not null && fun.ReceiverType is not null &&
               IsAssignableTo(receiver.Type, fun.ReceiverType);
    }

    #endregion
}