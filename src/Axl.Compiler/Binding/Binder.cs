using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Binding;

public sealed class Binder
{
    private readonly DiagnosticBag _diagnostics;
    private readonly SemanticSideTable _semanticSideTable;
    private readonly BaseNamespaceSymbol _base;
    private readonly Symbol _owner;
    
    private readonly Scope _scope;
    private readonly bool _inLoop = false;
    
    private Binder(Symbol owner, Scope scope, bool inLoop, BaseNamespaceSymbol @base, DiagnosticBag diagnosticBag, SemanticSideTable semanticSideTable)
    {
        _base = @base;
        _diagnostics = diagnosticBag;
        _semanticSideTable = semanticSideTable;

        _scope = scope;
        _owner = owner;
        _inLoop = inLoop;
    }

    private Binder NestedBinderWithOwner(Symbol owner)
        => new(owner, _scope.Nested(), inLoop: false,
            _base, _diagnostics, _semanticSideTable);
    
    private Binder NestedBinderWithVisibleSymbols(params IEnumerable<Symbol> visibleSymbols)
        => new(_owner, _scope.Nested(visibleSymbols), _inLoop,
            _base, _diagnostics, _semanticSideTable);
    
    private Binder NestedBinderInLoop()
        => new(_owner, _scope.Nested(), inLoop: true,
            _base, _diagnostics, _semanticSideTable);
    
    private Binder AdjacentBinderWithVisibleSymbols(params IEnumerable<Symbol> visibleSymbols)
        => new(_owner, _scope.Adjacent(visibleSymbols), _inLoop,
            _base, _diagnostics, _semanticSideTable);
    
    
    /// <summary>
    /// Whether a value of type <paramref name="source"/> can be
    /// assigned to a target of type <paramref name="target"/>.
    /// </summary>
    private static bool IsAssignableTo(TypeSymbol source, TypeSymbol target)
    {
        // Errors are silent
        if (source is ErrorTypeSymbol || target is ErrorTypeSymbol) return true;
        
        // Never assigns to anything
        if (source is NeverTypeSymbol) return true;

        return source == target;
    }

    private bool CheckTypeAndReportMismatch(BoundValue value, TypeSymbol expected)
    {
        if (!IsAssignableTo(value.Type, expected))
        {
            _diagnostics.ReportError(new Diagnostic.TypeMismatch(
                Value: value,
                Expected: expected));
            return false;
        }

        return true;
    }

    private bool IsAccessible(Symbol symbol)
        => _owner.SelfAndOwners().Contains(symbol.AccessibleWithin);
    

    private bool CheckAccessibility(Symbol symbol, SyntaxNode syntax)
    {
        if (!IsAccessible(symbol))
        {
            _diagnostics.ReportError(new Diagnostic.NotAccessible(symbol, syntax));
            return false;
        }

        return true;
    }


    private FunSymbol? LookupMethod(TypeSymbol instanceType, string name, ImmutableArray<TypeSymbol> argumentTypes)
    {
        if (argumentTypes.OfType<ErrorTypeSymbol>().Any())
            return null;

        return instanceType
            .LookupMembers(name)
            .OfType<FunSymbol>()
            .FirstOrDefault(fun => fun is { ReceiverType: not null } &&
                                   IsAssignableTo(instanceType, fun.ReceiverType) &&
                                   fun.ParameterTypes.SequenceEqual(argumentTypes));
    }


    #region File and Script

    public static BoundFile BindFile(FileSyntax syntax)
    {
        var diagnostics = new DiagnosticBag();
        var semanticSideTable = new SemanticSideTable();
        
        var rootNamespace = new RootNamespaceSymbol();
        var baseNamespace = rootNamespace.BaseNamespace;

        var globalBinder = new Binder(owner: rootNamespace, 
            Scope.Root([baseNamespace, .. baseNamespace.Members]), 
            inLoop: false,
            baseNamespace,  diagnostics, semanticSideTable);
        
        var scriptFun = new FunSymbol(name: "", 
            owner: rootNamespace,
            isPublic: false,
            receiverType: null,
            isMutatingReceiver: false);

        var scriptBinder = globalBinder.NestedBinderWithOwner(scriptFun);
        scriptFun.SetParametersAndReturnType([], baseNamespace.Unit);
        scriptFun.SetBody(scriptBinder.BindScriptBody(syntax, out var localTypes));
        
        return new BoundFile(scriptFun, localTypes, diagnostics.Drain(), semanticSideTable);
    }

    private BoundBlock BindScriptBody(FileSyntax syntax, out ImmutableArray<TypeSymbol> localTypes)
    {
        // Bind structs entirely with current scope, so that they
        // don't see any locals or local funs
        localTypes = BindStructs(syntax.Children.OfType<StructDeclSyntax>()).CastArray<TypeSymbol>();
        var withTypesBinder = NestedBinderWithVisibleSymbols(localTypes);

        // Bind local funs
        var localFuns = withTypesBinder.BindFuns(syntax.Children.OfType<FunDeclSyntax>());
        withTypesBinder.CheckDuplicateDeclarations([.. localTypes, .. localFuns]);
        var withTypesAndLocalFunsBinder = withTypesBinder.AdjacentBinderWithVisibleSymbols(localFuns);

        var stmts = withTypesAndLocalFunsBinder.BindStmtsAndLocalFunBodies(
            [.. syntax.SyntaxNodes().Where(node => node is not StructDeclSyntax)], localFuns);
        
        var block = new BoundBlock(stmts, localFuns, _base.Unit, syntax);
        
        // Insert unit return if necessary
        if (!block.IsDiverging)
            block = block.WithAdditionalStmts(new BoundReturn(null));

        return block;
    }
    
    #endregion
    
    #region Funs and Structs

    private ImmutableArray<FunSymbol> BindFuns(IEnumerable<FunDeclSyntax> syntaxes)
    {
        return [.. syntaxes.Select(BindFunSymbol)];
    }

    private FunSymbol BindFunSymbol(FunDeclSyntax syntax)
    {
        Guard.IsState(_owner is FunSymbol or TypeSymbol, $"{nameof(BindFunSymbol)} only valid on types or funs.");

        // --- Bind declaration
        var isPublic = false;
        if (syntax.PubKw is not null)
        {
            if (_owner is not TypeSymbol)
                _diagnostics.ReportError(new Diagnostic.InvalidModifier(syntax.PubKw, _owner));
            else
                isPublic = true;
        }

        TypeSymbol? receiverType;
        var isMutatingReceiver = false;
        
        if (_owner is FunSymbol ownerFun)
        {
            // This is a local fun.
            // Static is not valid. It is just inherited from its parent.
            if (syntax.StaticKw is not null)
                _diagnostics.ReportError(new Diagnostic.InvalidModifier(syntax.StaticKw, _owner));
            receiverType = ownerFun.ReceiverType;

            if (syntax.VarKw is not null)
            {
                // Var on local funs is only allowed, if its parent is var.
                if (receiverType is null)
                    _diagnostics.ReportError(new Diagnostic.InvalidModifier(syntax.VarKw, _owner));
                else if (!ownerFun.IsMutatingReceiver)
                    _diagnostics.ReportError(new Diagnostic.InvalidModifier(syntax.VarKw, _owner)); //TODO: Better diagnostic
                else
                    isMutatingReceiver = true;
            }
        }
        else if (_owner is TypeSymbol ownerType)
        {
            // This fun is on a type
            if (syntax.StaticKw is not null)
            {
                if (syntax.VarKw is not null)
                    _diagnostics.ReportError(new Diagnostic.StaticFunCannotBeVar(syntax.VarKw, syntax.Name));
                receiverType = null;
            }
            else
            {
                receiverType = ownerType;
                isMutatingReceiver = syntax.VarKw is not null;
            }
        }
        else
            throw new UnreachableException();
        
        var funSymbol = new FunSymbol(syntax.Name.Identifier,
            owner: _owner,
            isPublic,
            receiverType,
            isMutatingReceiver,
            declarationSyntax: syntax);

        // Bind signature
        var signatureBinder = NestedBinderWithOwner(funSymbol);
        var returnType = syntax.ReturnTypeAnnotation is not null
            ? signatureBinder.BindType(syntax.ReturnTypeAnnotation)
            : _base.Unit;
        var parameters = signatureBinder.BindParameters(syntax.ParameterList);
        funSymbol.SetParametersAndReturnType(parameters, returnType);

        _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, funSymbol);
        return funSymbol;
    }

    private BoundBlock BindFunBody(FunBodySyntax syntax)
    {
        if (_owner is not FunSymbol fun)
            throw new InvalidOperationException($"{nameof(BindFunBody)} only valid on funs.");

        var withParametersBinder = AdjacentBinderWithVisibleSymbols(fun.Parameters);

        // --- Empty body
        if (syntax.Expr is null)
            throw new NotImplementedException("Funs without body not supported yet.");

        // --- Expression body
        if (syntax.IsExpressionBodied)
        {
            var expr = withParametersBinder.BindValue(syntax.Expr);
            if (!CheckTypeAndReportMismatch(expr, fun.ReturnType))
                expr = new BoundError(syntax.Expr);

            return new BoundBlock([new BoundReturn(expr)], [], _base.Unit);
        }

        // --- Block body
        Debug.Assert(syntax.Expr is BlockExprSyntax);
        var block = withParametersBinder.BindBlock((BlockExprSyntax)syntax.Expr);

        // Check that all code-paths return a value
        if (!block.IsDiverging)
        {
            if (fun.ReturnType == _base.Unit)
                block = block.WithAdditionalStmts(new BoundReturn(null));
            else
                _diagnostics.ReportError(new Diagnostic.MissingReturn(block.Syntax!));
        }

        return block;
    }
    
    private ImmutableArray<ParameterSymbol> BindParameters(ParamListSyntax syntax)
    {
        var parameters = syntax.Parameters
            .Select(BindParameter)
            .ToImmutableArray();

        // Report duplicate parameter errors only on parameters
        // with a non-empty name.
        var duplicateParamGroups = parameters
            .Where(param => param.Name is not "")
            .GroupBy(param => param.Name)
            .Select(grp => grp.ToImmutableArray())
            .Where(grp => grp.Length > 1);
        foreach (var duplicateGroup in duplicateParamGroups)
            _diagnostics.ReportError(new Diagnostic.DuplicateParameters(duplicateGroup));

        return parameters;
    }
    
    private ParameterSymbol BindParameter(ParamSyntax syntax)
    {
        if (_owner is not FunSymbol fun)
            throw new InvalidOperationException($"{nameof(BindParameter)} only valid on funs.");

        var type = BindType(syntax.TypeAnnotation);
        var symbol = new ParameterSymbol(syntax.Name.Identifier, fun, type, syntax);
        _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, symbol);
        return symbol;
    }

    
    private ImmutableArray<StructSymbol> BindStructs(IEnumerable<StructDeclSyntax> syntaxes)
    {
        var structs = syntaxes.Select(BindStructSymbol).ToImmutableArray();
        var withStructsBinder = NestedBinderWithVisibleSymbols(structs);
        var inStructBinders = structs
            .Select(withStructsBinder.NestedBinderWithOwner)
            .ToImmutableArray();
        
        for (var i = 0; i < structs.Length; i++)
            structs[i].SetMembers(inStructBinders[i].BindStructMembers(structs[i].DeclarationSyntax!));

        foreach (var structSymbol in structs)
            CheckRecursiveStructLayout(structSymbol);

        // Bind method bodies
        for (var i = 0; i < structs.Length; i++)
        {
            // Bind struct body with all static members in scope
            var structSymbol = structs[i];
            var withStructMembersBinder = inStructBinders[i].NestedBinderWithVisibleSymbols(structSymbol.Members);
            
            var funs = structSymbol.Members.OfType<FunSymbol>();

            foreach (var fun in funs)
            {
                Debug.Assert(fun.DeclarationSyntax is not null);
                var funBodyBinder = withStructMembersBinder.NestedBinderWithOwner(fun);
                fun.SetBody(funBodyBinder.BindFunBody(fun.DeclarationSyntax.Body));
            }
        }

        return structs;
    }

    private StructSymbol BindStructSymbol(StructDeclSyntax syntax)
    {
        var symbol = new StructSymbol(syntax.Name.Identifier,
            owner: _owner, isPublic: false, isPrimitive: false, syntax);
        if (symbol.Name.Length > 0)
            _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, symbol);
        return symbol;
    }

    private ImmutableArray<Symbol> BindStructMembers(StructDeclSyntax syntax)
    {
        var fields = syntax.Body?.Children
            .OfType<FieldDeclSyntax>()
            .Select(Symbol (fieldSyntax) => BindField(fieldSyntax))
            .ToImmutableArray() ?? [];
        var funs = BindFuns(syntax.Body?.Children
            .OfType<FunDeclSyntax>() ?? []);

        ImmutableArray<Symbol> members = [.. fields, .. funs];
        CheckDuplicateDeclarations(members);
        return members;
    }

    private FieldSymbol BindField(FieldDeclSyntax syntax)
    {
        if (_owner is not StructSymbol structSymbol)
            throw new InvalidOperationException("Can only bind fields inside structs.");
        
        var isPub = syntax.PubKw is not null;
        var type = BindType(syntax.TypeExpr);
        var symbol = new FieldSymbol(syntax.Name.Token.Identifier, structSymbol, isPub, type, syntax);
        if (symbol.Name.Length > 0)
            _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, symbol);
        
        return symbol;
    }


    private void CheckRecursiveStructLayout(StructSymbol symbol)
    {
        Recurse(symbol, []);
        return;
        
        void Recurse(StructSymbol current, FieldSymbol[] path)
        {
            foreach (var field in current.Members.OfType<FieldSymbol>())
            {
                if (field.Type is not StructSymbol fieldStructType)
                    continue;

                if (fieldStructType == symbol)
                {
                    _diagnostics.ReportError(new Diagnostic.RecursiveStructLayout(symbol, [.. path, field]));
                }
                else
                    Recurse(fieldStructType, [.. path, field]);
            }
        }
    }
    
    private void CheckDuplicateDeclarations(ImmutableArray<Symbol> declared)
    {
        var symbolsByName = declared
            .Where(symbol => symbol.Name is not "")
            .GroupBy(symbol => symbol.Name)
            .Select(duplicates => duplicates.ToImmutableArray())
            .ToImmutableArray();

        foreach (var duplicateArray in symbolsByName.Where(duplicates => duplicates.Length > 1))
        {
            // For multiple funs, we still need to check their signatures.
            // If anything non-fun is on the duplicate list, we know its an error.
            if (duplicateArray.Any(s => s is not FunSymbol))
            {
                _diagnostics.ReportError(new Diagnostic.AlreadyDeclared(duplicateArray));
                continue;
            }
            
            // Now it is all funs and we need to check the signatures
            CheckDuplicateDeclarationsInFunGroup([.. duplicateArray.Select(s => (FunSymbol)s)]);
        }
    }

    private void CheckDuplicateDeclarationsInFunGroup(IEnumerable<FunSymbol> funsWithSameName)
    {
        var remaining = funsWithSameName.ToList();
        while (remaining.Count > 0)
        {
            var first = remaining[0];
            var sameSignature = remaining
                .Where(fun => fun.ParameterTypes.SequenceEqual(first.ParameterTypes))
                .ToImmutableArray();
            if (sameSignature.Length > 1)
            {
                // Errors in the signature should not report another error.
                var duplicatesToReport = sameSignature
                    .Where(fun => fun.ParameterTypes.All(paramType => paramType is not ErrorTypeSymbol))
                    .ToImmutableArray();

                if (duplicatesToReport.Length > 1)
                    _diagnostics.ReportError(new Diagnostic.DuplicateFunDeclarations(sameSignature));
            }

            remaining.RemoveAll(sameSignature.Contains);
        }
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

        return BindValue(exprSyntax);
    }
    
    private BoundStmt BindVarDecl(VarDeclSyntax syntax)
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

        // Accept shadowing only if it's another variable.
        var lookupResult = _scope.LookupHere(syntax.Name.Identifier).FirstOrDefault();
        if (lookupResult is not (null or VariableSymbol))
        {
            _diagnostics.ReportError(new Diagnostic.CannotShadow(lookupResult, syntax.Name));
            return new BoundError(syntax);
        }
        
        var isReadOnly = syntax.VarOrLetKwToken.Kind is TokenKind.LetKw;
        var variable = new VariableSymbol(
            syntax.Name.Identifier, 
            _owner as FunSymbol ?? throw new InvalidOperationException("Values can only be bound on fun bodies."),
            isReadOnly, 
            variableType);
        _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, variable);
    
        return new BoundVarDecl(variable, boundInitializer, syntax);
    }
    
    private BoundValue BindVarDeclInitializer(VarDeclSyntax syntax)
    {
        if (syntax.Initializer is null)
        {
            _diagnostics.ReportError(new Diagnostic.MissingInitializer(syntax));
            return new BoundError();    
        }
    
        return BindValue(syntax.Initializer);
    }
    
    private BoundStmt BindWhile(WhileStmtSyntax syntax)
    {
        var condition = BindCondition(syntax.Condition);
        var body = NestedBinderInLoop().BindValue(syntax.Body);
        
        return new BoundWhile(condition, body, syntax);
    }
    
    #endregion

    private BoundNode BindExpr(ExprSyntax syntax) => syntax switch
    {
        GetMemberExprSyntax getMemberExprSyntax => BindGetMember(getMemberExprSyntax),
        IdNameSyntax idNameSyntax => BindIdName(idNameSyntax),
        SelfSyntax selfSyntax => BindSelf(selfSyntax),
        CallExprSyntax callExprSyntax => BindCall(callExprSyntax),
        
        // Strings and Literals
        NumberLiteralSyntax numberLiteralSyntax => BindNumberLiteral(numberLiteralSyntax),
        TrueLiteralSyntax => new BoundConst(value: true, type: _base.Bool, syntax),
        FalseLiteralSyntax => new BoundConst(value: false, type: _base.Bool, syntax),
        StringExprSyntax stringExprSyntax => BindString(stringExprSyntax),
        
        // Operators
        BinaryExprSyntax binaryExprSyntax => BindBinary(binaryExprSyntax),
        UnaryExprSyntax unaryExprSyntax => BindUnary(unaryExprSyntax),
        
        // Blocks and Control Flow
        BlockExprSyntax blockExprSyntax => BindBlock(blockExprSyntax),
        IfExprSyntax ifExprSyntax => BindIfExpr(ifExprSyntax),
        GroupExprSyntax groupExprSyntax => BindValue(groupExprSyntax.Inner),
        BreakExprSyntax breakExprSyntax => BindBreakOrContinue(breakExprSyntax),
        ContinueExprSyntax or BreakExprSyntax => BindBreakOrContinue(syntax),
        ReturnExprSyntax returnExprSyntax => BindReturn(returnExprSyntax),
        
        // Error
        ErrorExprSyntax errorExprSyntax => new BoundError(errorExprSyntax),
    };

    private BoundValue BindValue(ExprSyntax syntax)
    {
        var bound = BindExpr(syntax);
        if (bound is not BoundValue value)
        {
            _diagnostics.ReportError(new Diagnostic.NotAValue(bound));
            return new BoundError(syntax);
        }

        return value;
    }

    private TypeSymbol BindType(ExprSyntax syntax)
    {
        var bound = BindExpr(syntax);
        if (bound is not BoundTypeRef typeRef)
        {
            if (bound is not BoundError)
                _diagnostics.ReportError(new Diagnostic.NotAType(bound));
            return ErrorTypeSymbol.Instance;
        }

        return typeRef.Type;
    }
    
    #region Literals and Strings

    private BoundStringExpr BindString(StringExprSyntax syntax)
    {
        var parts = syntax.Parts.Select(BindStringPart).ToImmutableArray();
    
        if (parts.Length == 0)
            parts = [new BoundConst("", _base.String, syntax)];
        
        return new BoundStringExpr(parts, _base.String, syntax);
    }
    
    private BoundValue BindStringPart(StringPartSyntax syntax)
        => syntax switch
        {
            StringTextSyntax textSyntax => new BoundConst(textSyntax.TextToken.ProcessedText, _base.String, syntax),
            StringInterpolationSyntax interpolationSyntax => BindStringInterpolation(interpolationSyntax),
        };
    
    private BoundValue BindStringInterpolation(StringInterpolationSyntax syntax)
    {
        if (syntax.Expr is null)
        {
            // Empty interpolation means nothing will be added, which is equivalent
            // to an empty text const.
            return new BoundConst("", _base.String, syntax);
        }
                
        var boundExpr = BindValue(syntax.Expr);

        if (IsAssignableTo(boundExpr.Type, _base.String))
            return boundExpr;
        
        // Try to find duck-typed ToString
        if (LookupMethod(boundExpr.Type, "ToString", []) is FunSymbol toStringFun
            && IsAssignableTo(toStringFun.ReturnType, _base.String))
        {
            return new BoundCall(toStringFun, receiver: boundExpr, arguments: [], syntax.Expr);
        }
        
        // Could not convert to string
        _diagnostics.ReportError(new Diagnostic.CannotConvert(syntax.Expr, From: boundExpr.Type, To: _base.String));
        return new BoundError(syntax);
    }

    private BoundValue BindNumberLiteral(NumberLiteralSyntax syntax)
    {
        var type = syntax.Token.Suffix switch
        {
            NumberLiteralSuffix.I32 => _base.I32,
            NumberLiteralSuffix.I64 => _base.I64,
            NumberLiteralSuffix.F32 => _base.F32,
            NumberLiteralSuffix.F64 => _base.F64,
    
            _ => syntax.Token.HasDecimalPoint ? _base.DefaultFloatType : _base.DefaultIntType
        };
        
        // Literals like "1.1i32" need to be rejected.
        if (syntax.Token.HasDecimalPoint &&
            type != _base.F32 && type != _base.F64)
        {
            _diagnostics.ReportError(new Diagnostic.SuffixInvalidForDecimalNumber(syntax));
            return new BoundError(syntax);
        }

        if (type == _base.I32)
        {
            if (!int.TryParse(syntax.Token.Body, out var value))
            {
                // The number literal is too big to fit inside an int/i32.
                _diagnostics.ReportError(new Diagnostic.NumberTooBig(syntax, type));
                return new BoundError(syntax);
            }

            return new BoundConst(value, type, syntax);
        }

        if (type == _base.I64)
        {
            if (!long.TryParse(syntax.Token.Body, out var value))
            {
                // The number literal is too big to fit inside an long/i64.
                _diagnostics.ReportError(new Diagnostic.NumberTooBig(syntax, type));
                return new BoundError(syntax);
            }

            return new BoundConst(value, type, syntax);
        }

        if (type == _base.F32)
        {
            var value = float.Parse(syntax.Token.Body, CultureInfo.InvariantCulture);
            return new BoundConst(value, type, syntax);
        }
        
        if (type == _base.F64)
        {
            var value = double.Parse(syntax.Token.Body, CultureInfo.InvariantCulture);
            return new BoundConst(value, type, syntax);
        }

        throw new UnreachableException();
    }
    
    #endregion
    
    #region Operators

    private BoundValue BindUnary(UnaryExprSyntax syntax)
        => BindOperatorCall(syntax.Operator.Text.ToString(), receiver: BindValue(syntax.Operand), arguments: [], syntax);
    
    private BoundValue BindBinary(BinaryExprSyntax syntax)
    {
        if (syntax.Operator.Kind is TokenKind.DoubleAmpersand or TokenKind.DoubleVerticalBar)
            return BindBooleanOperator(syntax);
        if (syntax.Operator.Kind == TokenKind.Equal)
            return BindAssign(syntax);
        
        var left = BindValue(syntax.Left);
        var right = BindValue(syntax.Right);

        return BindOperatorCall(syntax.Operator.Text.ToString(), receiver: left, arguments: [right], syntax);
    }
    
    private BoundValue BindOperatorCall(string operatorName, BoundValue receiver, ImmutableArray<BoundValue> arguments, SyntaxNode syntax)
    {
        if (receiver.Type is ErrorTypeSymbol ||
            arguments.Any(arg => arg.Type is ErrorTypeSymbol))
        {
            return new BoundError(syntax);
        }

        var operatorFun = LookupMethod(receiver.Type, operatorName, [.. arguments.Select(expr => expr.Type)]);
        if (operatorFun is null)
        {
            _diagnostics.ReportError(
                new Diagnostic.UndefinedOperator(operatorName,
                    [receiver.Type, .. arguments.Select(arg => arg.Type)], syntax));
            return new BoundError(syntax);
        }

        return new BoundCall(operatorFun, receiver, arguments, syntax);
    }

    private BoundValue BindBooleanOperator(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.DoubleAmpersand or TokenKind.DoubleVerticalBar);
    
        var left = BindValue(syntax.Left);
        var right = BindValue(syntax.Right);
        if (left.Type is ErrorTypeSymbol || right.Type is ErrorTypeSymbol)
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new BoundError(syntax);
        }
    
        // Type-check against bool
        if (!CheckTypeAndReportMismatch(left, _base.Bool) ||
            !CheckTypeAndReportMismatch(right, _base.Bool))
        {
            return new BoundError(syntax);
        }
    
        if (syntax.Operator.Kind is TokenKind.DoubleAmpersand)
            return new BoundAnd(left, right, _base.Bool, syntax);
        if (syntax.Operator.Kind is TokenKind.DoubleVerticalBar)
            return new BoundOr(left, right, _base.Bool, syntax);
    
        throw new UnreachableException();
    }
    
    #endregion
    
    #region Blocks, Control Flow
    
    private BoundBlock BindBlock(BlockExprSyntax syntax)
    {
        // Bind types here, because script binding runs through here as well.
        // Bind first types, then funs, then the body.
        
        var inBlockBinder = NestedBinderWithVisibleSymbols();
        
        var localFuns = inBlockBinder.BindFuns(syntax.Children.OfType<FunDeclSyntax>());
        inBlockBinder.CheckDuplicateDeclarations([.. localFuns]);
        inBlockBinder = inBlockBinder.AdjacentBinderWithVisibleSymbols(localFuns);
        
        var stmts = inBlockBinder.BindStmtsAndLocalFunBodies([.. syntax.SyntaxNodes()], localFuns);
        return new BoundBlock(stmts, localFuns, _base.Unit, syntax);
    }

    private ImmutableArray<BoundStmt> BindStmtsAndLocalFunBodies(ImmutableArray<SyntaxNode> childNodes, ImmutableArray<FunSymbol> localFuns)
    {
        var stmts = ImmutableArray.CreateBuilder<BoundStmt>();
        for (var i = 0; i < childNodes.Length; i++)
        {
            var node = childNodes[i];
            switch (node)
            {
                case StmtSyntax stmtSyntax:
                    var boundStmt = BindStmt(stmtSyntax);
                    stmts.Add(boundStmt);
                    if (boundStmt is BoundVarDecl boundVarDecl)
                    {
                        // Create an adjacent scope and continue from there.
                        if (i + 1 < childNodes.Length)
                        {
                            var adjacentBinder = AdjacentBinderWithVisibleSymbols(boundVarDecl.Variable);
                            stmts.AddRange(adjacentBinder.BindStmtsAndLocalFunBodies(childNodes[(i + 1)..], localFuns));
                        }
                        
                        return stmts.DrainToImmutable();
                    }
                    break;

                case FunDeclSyntax funDeclSyntax:
                    var localFunSymbol = localFuns.Single(fun => fun.DeclarationSyntax == funDeclSyntax);
                    var localFunBinder = NestedBinderWithOwner(localFunSymbol);
                    localFunSymbol.SetBody(localFunBinder.BindFunBody(funDeclSyntax.Body));
                    break;

                case StructDeclSyntax:
                    throw new UnreachableException("Structs are handled in script/global scope binding.");

                default:
                    _diagnostics.ReportError(new Diagnostic.UnsupportedFeature(node));
                    break;
            }
        }

        return stmts.DrainToImmutable();
    }

    private BoundValue BindCondition(ExprSyntax syntax)
    {
        var condition = BindValue(syntax);
        if (!CheckTypeAndReportMismatch(condition, expected: _base.Bool))
            condition = new BoundError(syntax);

        return condition;
    }
    
    private BoundValue BindIfExpr(IfExprSyntax syntax)
    {
        var condition = BindCondition(syntax.Condition);
        var body = BindValue(syntax.Body);
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

    private BoundValue BindElseExpr(IfExprSyntax ifSyntax)
    {
        var elseSyntax = ifSyntax.ElseBody;
        if (elseSyntax is null)
        {
            _diagnostics.ReportError(new Diagnostic.MissingElse(ifSyntax));
            return new BoundError();
        }

        return BindValue(elseSyntax);
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
    
    private BoundValue BindBreakOrContinue(ExprSyntax syntax)
    {
        Debug.Assert(syntax is BreakExprSyntax or ContinueExprSyntax);
        
        if (!_inLoop)
        {
            _diagnostics.ReportError(new Diagnostic.BreakOrContinueOutsideLoop(syntax));
            return new BoundError(syntax);
        }
    
        return syntax is BreakExprSyntax
            ? new BoundBreak(syntax)
            : new BoundContinue(syntax);
    }

    private BoundValue BindReturn(ReturnExprSyntax syntax)
    {
        if (_owner is not FunSymbol fun)
            throw new InvalidOperationException("Values can only be bound on fun bodies.");
        
        var expr = syntax.Expr is not null ? BindValue(syntax.Expr) : null;

        if (expr is not null)
        {
            if (!CheckTypeAndReportMismatch(expr, fun.ReturnType))
                expr = new BoundError(syntax.Expr);
        }
        else if (fun.ReturnType != _base.Unit)
        {
            _diagnostics.ReportError(new Diagnostic.MissingReturnValue(syntax));
            expr = new BoundError();
        }

        return new BoundReturn(expr, syntax);
    }
    
    #endregion
    
    #region Names, Self, Assign, Call, GetMember

    private BoundNode BindSelf(SelfSyntax syntax)
    {
        if (_owner is not FunSymbol funOwner ||
            funOwner.ReceiverType is null ||
            _owner.OwningType is null ||
            funOwner.ReceiverType != _owner.OwningType)
        {
            // Currently, self in local funs of methods are rejected.
            _diagnostics.ReportError(new Diagnostic.InvalidSelfRef(syntax, _owner));
            return new BoundError(syntax);
        }

        return new BoundSelf(funOwner.ReceiverType, funOwner.IsMutatingReceiver, syntax);
    }

    private enum LookupFailReason
    {
        // Ordered from worst reason to best reason. In cases with
        // multiple lookup fails, the best reason is picked.
        
        VariableWouldCapture,
        InstanceMemberAccessFromStaticContext,
        InstanceMemberAccessFromDifferentType,
        StaticMemberAccessThroughInstance,
        NotAccessible,
        MethodMayNotMutateReceiver,
    }

    private union LookupResult(BoundNode, LookupFailReason);
    
    private LookupResult ClassifyPlainNameLookup(Symbol symbol, SyntaxNode syntax)
    {
        switch (symbol)
        {
            case NamespaceSymbol @namespace:
                return new BoundNamespaceRef(@namespace, syntax, syntax);
            case TypeSymbol type:
                return new BoundTypeRef(type, syntax, syntax);

            case VariableSymbol variable:
            {
                // Variables could be captured from outside, which is forbidden.

                if (variable.Owner != _owner)
                    return LookupFailReason.VariableWouldCapture;

                return new BoundVariable(variable, syntax);
            }

            case FunSymbol fun:
            {
                // Since we are in a static context, static funs are always ok
                if (fun.ReceiverType is null)
                    return new BoundFunRef(fun, receiver: null, syntax, syntax);
                
                // Since fun is not static, we are binding a method access.
                // This means inserting an implicit self.
                Debug.Assert(_owner.OwningType is not null, 
                    "Methods are only inserted on type binders.");
                
                if (_owner is not FunSymbol ownerFun || ownerFun.ReceiverType is null )
                    return LookupFailReason.InstanceMemberAccessFromStaticContext;
                if (ownerFun.ReceiverType != fun.ReceiverType || _owner.OwningType != fun.ReceiverType)
                    return LookupFailReason.InstanceMemberAccessFromDifferentType;
                if (fun.IsMutatingReceiver && !ownerFun.IsMutatingReceiver)
                    return LookupFailReason.MethodMayNotMutateReceiver;
                
                var implicitSelf = new BoundSelf(_owner.OwningType, mutable: ownerFun.IsMutatingReceiver);
                return new BoundFunRef(fun, receiver: implicitSelf, syntax, syntax);
            }
            
            case FieldSymbol field:
            {
                Debug.Assert(_owner.OwningType == field.Owner,
                    "Fields are only inserted on type binders.");
                Debug.Assert(_owner.OwningType is not null);
                
                // Fields must be accessed through implicit self. For that,
                // we need to be in a non-static fun.
                if (_owner is not FunSymbol ownerFun || ownerFun.ReceiverType is null)
                    return LookupFailReason.InstanceMemberAccessFromStaticContext;
                if (ownerFun.ReceiverType != field.Owner)
                    return LookupFailReason.InstanceMemberAccessFromDifferentType;

                var implicitSelf = new BoundSelf(_owner.OwningType, mutable: ownerFun.IsMutatingReceiver);
                return new BoundFieldAccess(
                    receiver: implicitSelf,
                    field,
                    syntax);
            }
            
            default:
                throw new UnreachableException(
                    $"Unknown symbol type {symbol.GetType().Name} in {nameof(ClassifyPlainNameLookup)}.");
        }
    }

    private BoundNode BindLookup(ImmutableArray<Symbol> symbols, SyntaxNode syntax, Func<Symbol, LookupResult> classify)
    {
        //TODO: Probably not everything needs to be classified
        var lookupResults = symbols.Select(s =>
        {
            if (!IsAccessible(s))
                return LookupFailReason.NotAccessible;
            return classify(s);
        }).ToImmutableArray();

        if (symbols[0] is FunSymbol)
        {
            var funResults = Enumerable.Range(0, symbols.Length)
                .TakeWhile(i => symbols[i] is FunSymbol)
                .Select(i => lookupResults[i])
                .ToImmutableArray();
            var validFuns = funResults
                .Where(result => result is BoundFunRef)
                .Select(result => (BoundFunRef)result.Value!)
                .ToImmutableArray();
            if (validFuns.Length == 1)
            {
                _semanticSideTable.AddResolvedSymbol(syntax.Location, symbols[0]);
                return validFuns[0];
            }
            if (validFuns.Length > 1)
                return new BoundFunGroupRef(validFuns, syntax);
            
            // All funs from the group are invalid. We need to report
            // the best reason. The lookup fail enum is ordered, so that
            // we can just order on the enum.
            //TODO: Order for best reason. Enum is already sorted.
            ReportLookupFail(symbols[0], (LookupFailReason)funResults[0].Value!, syntax);
            _semanticSideTable.AddResolvedSymbol(syntax.Location, symbols[0]);
            return new BoundError(syntax);
        }
        
        switch (lookupResults[0])
        {
            case LookupFailReason failReason:
                ReportLookupFail(symbols[0], failReason, syntax);
                _semanticSideTable.AddResolvedSymbol(syntax.Location, symbols[0]);
                return new BoundError(syntax);

            case BoundFunRef:
                throw new UnreachableException();
            default:
                _semanticSideTable.AddResolvedSymbol(syntax.Location, symbols[0]);
                return (BoundNode)lookupResults[0].Value!;
        }
    }
    
    private BoundNode BindIdName(IdNameSyntax syntax)
    {
        if (syntax.Token.IsMissing)
            return new BoundError(syntax);

        var visibleSymbols = _scope.Lookup(syntax.Token.Identifier);
        if (visibleSymbols.Length == 0)
        {
            _diagnostics.ReportError(new Diagnostic.UndefinedName(syntax));
            return new BoundError(syntax);
        }

        return BindLookup(visibleSymbols, syntax, s => ClassifyPlainNameLookup(s, syntax));
    }

    private void ReportLookupFail(Symbol symbol, LookupFailReason reason, SyntaxNode syntax)
    {
        switch (reason)
        {
            case LookupFailReason.NotAccessible:
                _diagnostics.ReportError(new Diagnostic.NotAccessible(symbol, syntax));
                break;

            case LookupFailReason.VariableWouldCapture:
                _diagnostics.ReportError(new Diagnostic.CannotCapture(syntax));
                break;

            case LookupFailReason.MethodMayNotMutateReceiver:
                //TODO: Do something else than just adding implicit self again :D
                _diagnostics.ReportError(new Diagnostic.CannotMutateReceiver(new BoundSelf(symbol.OwningType!),
                    (FunSymbol)symbol, syntax));
                break;

            case LookupFailReason.InstanceMemberAccessFromStaticContext:
                _diagnostics.ReportError(new Diagnostic.InstanceMemberNotAccessibleFromStaticContext(symbol.Name, syntax));
                break;
            case LookupFailReason.InstanceMemberAccessFromDifferentType:
                throw new NotImplementedException(
                    $"{LookupFailReason.InstanceMemberAccessFromDifferentType}, but there are no nested types yet.");
            
            case LookupFailReason.StaticMemberAccessThroughInstance:
                _diagnostics.ReportError(new Diagnostic.CannotAccessThroughInstance(syntax));
                break;
            
            default:
                throw new UnreachableException($"Unknown lookup fail reason {reason} in {nameof(ReportLookupFail)}.");
        }
    }

    private BoundValue BindAssign(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.Equal);
        
        var target = BindValue(syntax.Left);
        var value = BindValue(syntax.Right);

        if (target is BoundError) 
            return new BoundError(syntax);
        if (value.Type is ErrorTypeSymbol)
            return new BoundAssign(target, value, ErrorTypeSymbol.Instance, syntax);
        
        if (!target.IsPlace || !target.IsAssignable)
        {
            _diagnostics.ReportError(new Diagnostic.CannotAssign(target));
            return new BoundError(syntax);
        }

        TypeSymbol type = CheckTypeAndReportMismatch(value, target.Type)
            ? _base.Unit
            : ErrorTypeSymbol.Instance;
        return new BoundAssign(target, value, type, syntax);
    }

    private BoundNode BindGetMember(GetMemberExprSyntax syntax)
    {
        var left = BindExpr(syntax.Left);

        return left switch
        {
            BoundFunGroupRef or BoundFunRef => BindUndefinedMember(),

            BoundNamespaceRef boundNamespaceRef => BindNamespaceOrTypeMember(boundNamespaceRef.Namespace),
            BoundTypeRef boundTypeRef => BindNamespaceOrTypeMember(boundTypeRef.Type),

            BoundValue boundValue => BindValueMember(boundValue),

            BoundStmt => throw new UnreachableException("Stmt can never be the left side of GetMember.")
        };

        BoundNode BindUndefinedMember()
        {
            _diagnostics.ReportError(new Diagnostic.UndefinedMember(left, syntax.Member));
            return new BoundError(syntax);
        }
        
        BoundNode BindNamespaceOrTypeMember(NamespaceOrTypeSymbol parent)
        {
            if (syntax.Member.Token.IsMissing)
                return new BoundError(syntax);

            var memberName = syntax.Member.Token.Identifier;
            var symbols = parent.LookupMembers(memberName);

            if (symbols.Length == 0)
            {
                _diagnostics.ReportError(new Diagnostic.UndefinedMember(left, syntax.Member));
                return new BoundError(syntax);
            }

            return BindLookup(symbols, syntax.Member, ClassifyMemberLookup);

            LookupResult ClassifyMemberLookup(Symbol symbol) => symbol switch
            {
                FieldSymbol => LookupFailReason.InstanceMemberAccessFromStaticContext,
                NamespaceSymbol @namespace => new BoundNamespaceRef(@namespace, syntax, syntax.Member),
                TypeSymbol type => new BoundTypeRef(type, syntax, syntax.Member),
 
                FunSymbol fun => fun.ReceiverType is null
                    ? new BoundFunRef(fun, receiver: null, syntax, syntax.Member)
                    : LookupFailReason.InstanceMemberAccessFromStaticContext,

                VariableSymbol => throw new UnreachableException("Variables are not members."),
            };
        }

        

        BoundNode BindValueMember(BoundValue value)
        {
            var type = value.Type;
            if (type is ErrorTypeSymbol)
                return new BoundError(syntax.Member);

            if (syntax.Member.Token.IsMissing)
                return new BoundError(syntax);

            var memberName = syntax.Member.Token.Identifier;
            var symbols = type.LookupMembers(memberName);

            if (symbols.Length == 0)
            {
                _diagnostics.ReportError(new Diagnostic.UndefinedMember(Left: value, syntax.Member));
                return new BoundError(syntax);
            }

            return BindLookup(symbols, syntax.Member, ClassifyMemberLookup);

            LookupResult ClassifyMemberLookup(Symbol symbol) => symbol switch
            {
                FieldSymbol field => new BoundFieldAccess(value, field, syntax),

                TypeSymbol => LookupFailReason.StaticMemberAccessThroughInstance,

                FunSymbol fun => fun.ReceiverType is null
                    ? LookupFailReason.StaticMemberAccessThroughInstance
                    : fun.ReceiverType != value.Type
                        ? throw new UnreachableException(
                            $"fun '{fun.Name}' was looked up on '{value.Type.Name}', but its receiver is '{fun.ReceiverType.Name}'.")
                        : !fun.IsMutatingReceiver || value.IsMutablePlace
                            ? new BoundFunRef(fun, receiver: value, syntax, syntax.Member)
                            : LookupFailReason.MethodMayNotMutateReceiver,

                VariableSymbol => throw new UnreachableException("Variables are not members."),
                VariableSymbol or NamespaceSymbol => throw new UnreachableException(
                    "Variables and namespaces are not type members."),
            };
        }
    }

    private BoundValue BindCall(CallExprSyntax syntax)
    {
        var callee = BindExpr(syntax.Callee);
        if (callee is BoundTypeRef { Type: StructSymbol } structRef)
            return BindStructInit((StructSymbol)structRef.Type, syntax, structRef.MemberSyntax);
        
        var arguments = syntax.ArgumentExprs.Select(BindValue).ToImmutableArray();

        //TODO: Implement named arguments
        var hasNamedArg = false;
        foreach (var argSyntax in syntax.ArgList.Arguments)
        {
            if (argSyntax.IsNamed)
            {
                hasNamedArg = true;
                _diagnostics.ReportError(new Diagnostic.UnsupportedFeature(argSyntax, "Named arguments are not yet implemented."));
            }
        }

        if (hasNamedArg)
            return new BoundError(syntax);
        
        ImmutableArray<BoundFunRef> calleeCandidates;

        if (callee is BoundFunRef boundFunRef)
            calleeCandidates = [boundFunRef];
        else if (callee is BoundFunGroupRef boundFunGroupRef)
            calleeCandidates = boundFunGroupRef.BoundFunRefs;
        else
        {
            if (callee is not BoundError)
                _diagnostics.ReportError(new Diagnostic.CannotCall(callee));
            return new BoundError(syntax);
        }

        var selected = SelectCallee(calleeCandidates, arguments, syntax);
        if (selected is null)
            return new BoundError(syntax);

        return new BoundCall(selected.Fun, selected.Receiver, arguments, syntax);
    }

    private BoundFunRef? SelectCallee(ImmutableArray<BoundFunRef> initialCandidates, ImmutableArray<BoundValue> arguments, CallExprSyntax callSyntax)
    {
        var candidates = initialCandidates.ToList();
        var funName = candidates[0].Fun.Name;
        
        // Filter arity
        candidates.RemoveAll(boundFunRef => boundFunRef.Fun.Parameters.Length != arguments.Length);
        if (candidates.Count == 0)
        {
            _diagnostics.ReportError(new Diagnostic.ArityMismatch(callSyntax.ArgList,
                funName,
                ParameterCount: initialCandidates.Length == 1 ? initialCandidates[0].Fun.Parameters.Length : null,
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
                    !CheckTypeAndReportMismatch(arguments[i], candidates[0].Fun.Parameters[i].Type))
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
            candidates.RemoveAll(boundFunRef => Enumerable.Range(0, arguments.Length)
                .Any(i => !IsAssignableTo(arguments[i].Type, boundFunRef.Fun.ParameterTypes[i])));
            if (candidates.Count != 1)
            {
                _diagnostics.ReportError(new Diagnostic.CannotResolveFun(callSyntax.ArgList, funName,
                    candidatesWithMatchingArity, [.. arguments.Select(arg => arg.Type)]));
                return null;
            }
        }

        return candidates[0];
    }

    private BoundValue BindStructInit(StructSymbol @struct, CallExprSyntax syntax, SyntaxNode structRefSyntax)
    {
        var uninitializedFields = @struct.Fields.ToList();
        var fieldInits = ImmutableArray.CreateBuilder<BoundFieldInit>();

        var hadError = false;
        foreach (var argSyntax in syntax.ArgList.Arguments)
        {
            var boundFieldInit = BindFieldInitializer(@struct, argSyntax) as BoundFieldInit;
            if (boundFieldInit is null)
            {
                hadError = true;
                continue;
            }

            if (!uninitializedFields.Contains(boundFieldInit.Field))
            {
                hadError = true;
                _diagnostics.ReportError(new Diagnostic.FieldAlreadyInitialized(argSyntax, boundFieldInit.Field));
                continue;
            }

            uninitializedFields.Remove(boundFieldInit.Field);
            fieldInits.Add(boundFieldInit);
        }

        if (hadError)
            return new BoundError(syntax);

        if (uninitializedFields.Count > 0)
        {
            _diagnostics.ReportError(new Diagnostic.MissingFieldInitializers([.. uninitializedFields], syntax.ArgList));
            return new BoundError(syntax);
        }

        if (@struct.IsPrimitive)
        {
            _diagnostics.ReportError(new Diagnostic.CannotInitializePrimitive(structRefSyntax));
            return new BoundError(syntax);
        }
        
        return new BoundStructInit(@struct, fieldInits.ToImmutable(), syntax);
    }

    private BoundValue BindFieldInitializer(StructSymbol @struct, ArgSyntax argSyntax)
    {
        var value = BindValue(argSyntax.Expr);

        var field = @struct.Fields.FirstOrDefault(field => argSyntax.IsNamed
            ? field.Name.SequenceEqual(argSyntax.Name!.Text)
            : field.Name.SequenceEqual(argSyntax.Text));

        if (field is null)
        {
            _diagnostics.ReportError(new Diagnostic.InvalidFieldName(@struct, argSyntax));
            return new BoundError(argSyntax);
        }

        if (!CheckAccessibility(field, argSyntax.IsNamed ? argSyntax.Name! : argSyntax) ||
            !CheckTypeAndReportMismatch(value, field.Type))
            return new BoundError(argSyntax);

        return new BoundFieldInit(field, value, argSyntax);
    }

    #endregion
}