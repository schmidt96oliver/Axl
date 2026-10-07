using System.Collections;
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
    private readonly FunSymbol _fun;
    
    private readonly Dictionary<FunDeclSyntax, FunSymbol> _funSymbolsByDecl = [];
    private Scope _scope;
    private bool _inLoop = false;
    
    private Binder(FunSymbol fun, Scope scope, BaseNamespaceSymbol @base, DiagnosticBag diagnosticBag, SemanticSideTable semanticSideTable)
    {
        _base = @base;
        _diagnostics = diagnosticBag;
        _semanticSideTable = semanticSideTable;

        _scope = scope;
        _fun = fun;
    }

    
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

    private bool IsVisible(Symbol symbol)
    {
        //TODO: Implement proper owner lookup
        
        // This will need to walk up owners of _fun, to see if
        // symbol is a member of something that owns _fun.
        return symbol is FieldSymbol { IsPub: true };
    }

    private bool CheckVisibility(Symbol symbol, SyntaxNode syntax)
    {
        if (!IsVisible(symbol))
        {
            _diagnostics.ReportError(new Diagnostic.NotVisible(symbol, syntax));
            return false;
        }

        return true;
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
    
    
    #region Global

    private static Scope CreateGlobalScope(NamespaceSymbol baseNamespace)
    {
        var global = new Scope();

        global.Declare(baseNamespace);

        // 'Base' is implicitly used
        foreach (var member in baseNamespace.Members)
            global.Declare(member);

        return global;
    }

    public static BoundFile BindFile(FileSyntax syntax, BaseNamespaceSymbol baseNamespace)
    {
        var scriptFun = new FunSymbol("", null, [], baseNamespace.Unit);

        var diagnostics = new DiagnosticBag();
        var semanticSideTable = new SemanticSideTable();
        var scope = new Scope(parent: CreateGlobalScope(baseNamespace));
        var binder = new Binder(scriptFun, scope, baseNamespace, diagnostics, semanticSideTable);

        var types = binder.BindStructs([.. syntax.Children.OfType<StructDeclSyntax>()]);
        var block = binder.BindBlockBody(
            [.. syntax.SyntaxNodes().Where(node => node is not StructDeclSyntax)], syntax);

        //TODO: Move to BindFunBody, so that everything follows the same logic.
        var returnStmt = new BoundReturn(null);
        block = new BoundBlock([.. block.Stmts, returnStmt], block.LocalFuns, block.Type, block.Syntax);
        
        scriptFun.SetBody(block);
        return new BoundFile(scriptFun, types.CastArray<TypeSymbol>(), diagnostics.Drain(), semanticSideTable);
    }
    
    #endregion
    
    #region Member Declarations

    private ImmutableArray<Symbol> BindFuns(IEnumerable<FunDeclSyntax> syntaxes)
    {
        var funs = syntaxes.Select(BindFunSymbol).ToImmutableArray();

        // Puts funs with the same name into a FunGroupSymbol
        var singleOrGroupSymbols = funs
            .Where(fun => fun.Name.Length > 0)
            .GroupBy(fun => fun.Name)
            .Select(funGrp => (Symbol)(funGrp.ToImmutableArray() switch
            {
                [] => throw new UnreachableException(),
                [var single] => single,
                var multiple => new FunGroupSymbol(funGrp.Key, multiple)
            }))
            .ToImmutableArray();
        
        foreach (var symbol in singleOrGroupSymbols)
            _scope.Declare(symbol);

        return singleOrGroupSymbols;
    }

    private FunSymbol BindFunSymbol(FunDeclSyntax syntax)
    {
        var parameters = syntax.Parameters
            .Select(BindParameter)
            .ToImmutableArray();
        var returnType = syntax.ReturnTypeAnnotation is not null
            ? BindType(syntax.ReturnTypeAnnotation)
            : _base.Unit;

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
        _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, funSymbol);
        return funSymbol;
    }

    private ParameterSymbol BindParameter(ParamSyntax syntax)
    {
        var symbol = new ParameterSymbol(syntax.Name.Identifier,
            BindType(syntax.TypeAnnotation),
            syntax);
        _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, symbol);
        return symbol;
    }
    
    
    private ImmutableArray<StructSymbol> BindStructs(IEnumerable<StructDeclSyntax> syntaxes)
    {
        var structs = syntaxes.Select(BindStructSymbol).ToImmutableArray();

        foreach (var structSymbol in structs)
            BindStructMembers(structSymbol);

        foreach (var structSymbol in structs)
            CheckRecursiveStructLayout(structSymbol);
        
        //TODO: Bind struct method/static method bodies here

        return structs;
    }

    private StructSymbol BindStructSymbol(StructDeclSyntax syntax)
    {
        var symbol = new StructSymbol(syntax.Name.Identifier, isPrimitive: false, syntax);
        if (symbol.Name.Length > 0)
        {
            _scope.Declare(symbol);
            _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, symbol);
        }
        return symbol;
    }

    private void BindStructMembers(StructSymbol structSymbol)
    {
        Debug.Assert(structSymbol.DeclarationSyntax is not null);
        var members = structSymbol.DeclarationSyntax.Body?.Children
            .OfType<FieldDeclSyntax>()
            .Select(Symbol (fieldSyntax) => BindField(structSymbol, fieldSyntax))
            .ToImmutableArray() ?? [];
        structSymbol.SetMembers(members);

        CheckDuplicateDeclarations(members);
    }

    private FieldSymbol BindField(StructSymbol structSymbol, FieldDeclSyntax syntax)
    {
        var isPub = syntax.PubKw is not null;
        var type = BindType(syntax.TypeExpr);
        var symbol = new FieldSymbol(syntax.Name.Token.Identifier, isPub, structSymbol, type, syntax);
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
            // For FunGroups, report each fun individually
            var toReport = duplicateArray
                .SelectMany(IEnumerable<Symbol> (symbol)
                    => symbol is FunGroupSymbol funGroup
                        ? funGroup.Funs
                        : [symbol])
                .ToImmutableArray();
            
            _diagnostics.ReportError(new Diagnostic.AlreadyDeclared(toReport));
        }
        
        foreach (var funGroup in symbolsByName
                     .Where(grp => grp is [FunGroupSymbol])
                     .Select(grp => (FunGroupSymbol)grp[0]))
        {
            CheckDuplicateDeclarationsInFunGroup(funGroup);
        }
    }

    private void CheckDuplicateDeclarationsInFunGroup(FunGroupSymbol funGroup)
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
    
    
    private void BindFunBody(FunDeclSyntax funDecl)
    {
        var funSymbol = _funSymbolsByDecl[funDecl];
        
        var funScope = new Scope(parent: _scope);
        foreach (var param in funSymbol.Parameters)
            funScope.Declare(param);

        var funBinder = new Binder(funSymbol, funScope, _base, _diagnostics, _semanticSideTable);

        var body = funBinder.BindFunBodyBlock(funSymbol);
        
        // Check that all code-paths return a value
        if (!body.IsDiverging)
        {
            if (funSymbol.ReturnType == _base.Unit)
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

        funSymbol.SetBody(body);
    }

    private BoundBlock BindFunBodyBlock(FunSymbol fun)
    {
        var syntax = fun.DeclarationSyntax ??
                     throw new ArgumentException($"{nameof(fun)} must be source-declared.", nameof(fun));
        
        if (syntax.Body.Expr is null)
            throw new NotImplementedException("Funs without body not supported yet.");

        if (syntax.Body.IsExpressionBodied)
        {
            var expr = BindValue(syntax.Body.Expr);
            if (!CheckTypeAndReportMismatch(expr, fun.ReturnType))
                expr = new BoundError(syntax.Body.Expr);
            
            return new BoundBlock([new BoundReturn(expr)], [], _base.Unit);
        }

        if (syntax.Body.Expr is not BlockExprSyntax blockSyntax)
            throw new UnreachableException();
        return BindBlock(blockSyntax);
    }

    
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
        var lookupResult = _scope.LookupHere(syntax.Name.Identifier);
        if (lookupResult is not (null or VariableSymbol))
        {
            _diagnostics.ReportError(new Diagnostic.CannotShadow(lookupResult, syntax.Name));
            return new BoundError(syntax);
        }
        
        var isReadOnly = syntax.VarOrLetKwToken.Kind is TokenKind.LetKw;
        var variable = new VariableSymbol(syntax.Name.Identifier, isReadOnly, variableType, _fun);
        _scope.Declare(variable);
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

        var previousInLoop = _inLoop;
        _inLoop = true;
        var body = BindValue(syntax.Body);
        _inLoop = previousInLoop;
        
        return new BoundWhile(condition, body, syntax);
    }
    
    #endregion

    private BoundNode BindExpr(ExprSyntax syntax) => syntax switch
    {
        GetMemberExprSyntax getMemberExprSyntax => BindGetMember(getMemberExprSyntax),
        IdNameSyntax idNameSyntax => BindIdName(idNameSyntax),
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
        _scope = new Scope(parent: _scope);

        var block = BindBlockBody([.. syntax.SyntaxNodes()], syntax);
        
        _scope = _scope.Parent!;
        return block;
    }

    private BoundBlock BindBlockBody(ImmutableArray<SyntaxNode> childNodes, SyntaxNode syntaxRef)
    {
        var funs = BindFuns(childNodes.OfType<FunDeclSyntax>());
        
        CheckDuplicateDeclarations(_scope.DeclaredHere);

        var stmts = ImmutableArray.CreateBuilder<BoundStmt>();
        foreach (var node in childNodes)
        {
            switch (node)
            {
                case StmtSyntax stmt:
                    stmts.Add(BindStmt(stmt));
                    break;
                case FunDeclSyntax funDecl:
                    BindFunBody(funDecl);
                    break;

                case StructDeclSyntax:
                    throw new UnreachableException("Structs are handled in script/global scope binding.");

                default:
                    _diagnostics.ReportError(new Diagnostic.UnsupportedFeature(node));
                    break;
            }
        }

        return new BoundBlock(stmts.DrainToImmutable(), funs, type: _base.Unit, syntaxRef);
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
        var expr = syntax.Expr is not null ? BindValue(syntax.Expr) : null;

        if (expr is not null)
        {
            if (!CheckTypeAndReportMismatch(expr, _fun.ReturnType))
                expr = new BoundError(syntax.Expr);
        }
        else if (_fun.ReturnType != _base.Unit)
        {
            _diagnostics.ReportError(new Diagnostic.MissingReturnValue(syntax));
            expr = new BoundError();
        }

        return new BoundReturn(expr, syntax);
    }
    
    #endregion
    
    #region Names, Assign, Call, GetMember

    private BoundNode BindIdName(IdNameSyntax syntax)
    {
        if (syntax.Token.IsMissing)
            return new BoundError(syntax);

        var symbol = _scope.Lookup(syntax.Token.Identifier);
        if (symbol is not null)
            _semanticSideTable.AddResolvedSymbol(syntax.Location, symbol);
        
        return symbol switch
        {
            NamespaceSymbol boundNamespace => new BoundNamespaceRef(boundNamespace, syntax, syntax),
            TypeSymbol type => new BoundTypeRef(type, syntax, syntax),

            FunSymbol fun => new BoundFunRef(fun, receiver: null, syntax, syntax),
            FunGroupSymbol funGroup => new BoundFunGroupRef(funGroup, receiver: null, syntax, syntax),

            VariableSymbol variableSymbol => BindVariable(variableSymbol),
            FieldSymbol => throw new UnreachableException("Fields can only be accessed through types."),
            
            null => BindUndefinedName()
        };

        BoundNode BindVariable(VariableSymbol variable)
        {
            // Captured variables need to be rejected.

            if (variable.Owner != _fun)
            {
                _diagnostics.ReportError(new Diagnostic.CannotCapture(syntax));
                return new BoundError(syntax);
            }

            return new BoundVariable(variable, syntax);
        }

        BoundError BindUndefinedName()
        {
            _diagnostics.ReportError(new Diagnostic.UndefinedName(syntax));
            return new BoundError(syntax);
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
            BoundFunGroupRef boundFunGroupRef => BindUndefined(boundFunGroupRef.FunGroup),
                BoundFunRef boundFunRef => BindUndefined(boundFunRef.Fun),

            BoundNamespaceRef boundNamespaceRef => BindNamespaceOrTypeMember(boundNamespaceRef.Namespace),
            BoundTypeRef boundTypeRef => BindNamespaceOrTypeMember(boundTypeRef.Type),

            BoundValue boundValue => BindValueMember(boundValue),

            BoundStmt => throw new UnreachableException("Stmt can never be the left side of GetMember.")
        };

        BoundNode BindNamespaceOrTypeMember(NamespaceOrTypeSymbol parent)
        {
            if (syntax.Member.Token.IsMissing)
                return new BoundError(syntax);

            var memberName = syntax.Member.Token.Identifier;
            var memberSymbol = parent.LookupMember(memberName);

            if (memberSymbol is null)
            {
                _diagnostics.ReportError(new Diagnostic.UndefinedMember(syntax.Member, parent));
                return new BoundError(syntax);
            }

            if (memberSymbol is FieldSymbol)
            {
                _diagnostics.ReportError(new Diagnostic.CannotAccessFieldWithoutReceiver(syntax.Member, (FieldSymbol)memberSymbol));
                return new BoundError(syntax);
            }

            _semanticSideTable.AddResolvedSymbol(syntax.Member.Location, memberSymbol);

            return memberSymbol switch
            {
                NamespaceSymbol @namespace => new BoundNamespaceRef(@namespace, syntax, syntax.Member),
                TypeSymbol type => new BoundTypeRef(type, syntax, syntax.Member),

                FunSymbol fun => new BoundFunRef(fun, receiver: null, syntax, syntax.Member),
                FunGroupSymbol funGroup => new BoundFunGroupRef(funGroup, receiver: null, syntax, syntax.Member),

                VariableSymbol => throw new UnreachableException("Variables are not members."),
                FieldSymbol => throw new UnreachableException("Already handled.")
            };
        }

        BoundNode BindUndefined(Symbol parent)
        {
            _diagnostics.ReportError(new Diagnostic.UndefinedMember(syntax.Member, parent));
            return new BoundError(syntax);
        }

        BoundNode BindValueMember(BoundValue value)
        {
            var type = value.Type;
            if (type is ErrorTypeSymbol)
                return new BoundError(syntax.Member);

            if (syntax.Member.Token.IsMissing)
                return new BoundError(syntax);

            var memberName = syntax.Member.Token.Identifier;
            var memberSymbol = type.LookupMember(memberName);

            if (memberSymbol is null)
            {
                _diagnostics.ReportError(new Diagnostic.UndefinedMember(syntax.Member, type));
                return new BoundError(syntax);
            }

            _semanticSideTable.AddResolvedSymbol(syntax.Member.Location, memberSymbol);

            if (memberSymbol is TypeSymbol)
            {
                // Tried access of a nested type, which must not be accessed through a value.
                _diagnostics.ReportError(new Diagnostic.CannotAccessThroughInstance(syntax.Member, type));
                return new BoundError(syntax);
            }

            return memberSymbol switch
            {
                FunSymbol fun => new BoundFunRef(fun, receiver: value, syntax, syntax.Member),
                FunGroupSymbol funGroup => new BoundFunGroupRef(funGroup, receiver: value, syntax, syntax.Member),
                FieldSymbol field => BindFieldAccess(value, field, syntax),
                
                VariableSymbol or NamespaceSymbol => throw new UnreachableException("Variables and namespaces are not type members."),
                TypeSymbol => throw new UnreachableException(),
            };
        }
    }

    private BoundValue BindFieldAccess(BoundValue value, FieldSymbol field, GetMemberExprSyntax syntax)
    {
        if (!CheckVisibility(field, syntax.Member))
            return new BoundError(syntax);

        return new BoundFieldAccess(value, field, syntax);
    }

    private BoundValue BindCall(CallExprSyntax syntax)
    {
        var callee = BindExpr(syntax.Callee);
        if (callee is BoundTypeRef { Type: StructSymbol } structRef)
            return BindStructInit((StructSymbol)structRef.Type, syntax, structRef.MemberSyntax!);
        
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
        
        ImmutableArray<FunSymbol> calleeCandidates;
        BoundValue? receiver;

        if (callee is BoundFunRef boundFunRef)
        {
            calleeCandidates = [boundFunRef.Fun];
            receiver = boundFunRef.Receiver;
        }
        else if (callee is BoundFunGroupRef boundFunGroupRef)
        {
            calleeCandidates = boundFunGroupRef.FunGroup.Funs;
            receiver = boundFunGroupRef.Receiver;
        }
        else
        {
            if (callee is not BoundError)
                _diagnostics.ReportError(new Diagnostic.CannotCall(callee));
            return new BoundError(syntax);
        }
        
        var funName = calleeCandidates[0].Name;
        Debug.Assert(calleeCandidates.All(fun => fun.Name == funName));

        return SelectCallee(calleeCandidates, receiver, arguments, funName, syntax) is { } selectedFun
            ? new BoundCall(selectedFun, receiver, arguments, syntax)
            : new BoundError(syntax);
    }

    private FunSymbol? SelectCallee(ImmutableArray<FunSymbol> initialCandidates, BoundValue? receiver, ImmutableArray<BoundValue> arguments,
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

        if (!CheckVisibility(field, argSyntax.IsNamed ? argSyntax.Name! : argSyntax) ||
            !CheckTypeAndReportMismatch(value, field.Type))
            return new BoundError(argSyntax);

        return new BoundFieldInit(field, value, argSyntax);
    }

    #endregion
}