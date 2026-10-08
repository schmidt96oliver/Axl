using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;
using Axl.Compiler.Text;

namespace Axl.Compiler.Diagnostics;

public partial record Diagnostic
{
    public sealed record TypeMismatch(BoundValue Value, TypeSymbol Expected) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Value.Syntax!.Location];

        public override string Message
            => $"Expected type '{Expected.Name}' but got '{Value.Type.Name}'.";
    }

    public sealed record MissingInitializer(VarDeclSyntax VarDeclSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [VarDeclSyntax.SyntaxElements().First().Location];

        public override string Message
            => "Initializer must be specified.";
    }

    public sealed record SuffixInvalidForDecimalNumber(NumberLiteralSyntax NumberLiteralSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations 
            => [NumberLiteralSyntax.Tree.SourceText.GetLocation(NumberLiteralSyntax.Token.SuffixRange)];

        public override string Message
            => $"Suffix '{NumberLiteralSyntax.Token.Suffix.ToString().ToLower()}' describes an integral type. Expected a type that describes a decimal number.";
    }

    public sealed record UndefinedName(IdNameSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.Location];
    
        public override string Message
            => $"Undefined name '{Syntax.Token.Identifier}'.";
    }

    public sealed record UndefinedOperator(string OperatorName, ImmutableArray<TypeSymbol> OperandTypes, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                if (Syntax is ExprStmtSyntax)
                {
                    // Do not mark the semicolon at the end.
                    var nonSemicolonElements = Syntax.SyntaxElements()
                        .TakeWhile(el => el is not Token { Kind: TokenKind.Semicolon })
                        .ToList();
                    var range = SourceRange.FromTo(nonSemicolonElements[0].Range!.Value, nonSemicolonElements[^1].Range!.Value);
                    return [Syntax.Tree.SourceText.GetLocation(range)];
                }

                return [Syntax.Location];
            }
        }

        public override string Message
            => $"Operator '{OperatorName}' is not defined for {GetTypeString()}.";

        private string GetTypeString()
            => OperandTypes.Length == 1
                ? $"type '{OperandTypes[0].Name}'"
                : $"types {string.Join(", ", OperandTypes[..^1].Select(expr => $"'{expr.Name}'"))} and '{OperandTypes[^1].Name}'";
    }

    public sealed record CannotAssign(BoundNode BoundTarget) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [BoundTarget.Syntax!.Location];

        public override string Message
            => BoundTarget switch
            {
                BoundSelf => "Cannot assign to 'self' directly.",
                BoundValue { IsPlace: false } 
                    => "Cannot assign to a temporary value.",
                BoundValue { IsPlace: true, IsMutablePlace: false } value when GetFirstPlace(value) is BoundSelf
                    => "Cannot mutate through 'self', because this fun is not 'var'. Consider declaring it 'var'.",
                BoundValue { IsPlace: true, IsMutablePlace: false } 
                    => "Cannot assign to through 'let' binding.",
                BoundValue => "Cannot assign this for unknown reason.",
                    
                BoundFunGroupRef boundFunGroup => $"Cannot assign to '{boundFunGroup.FunGroup.Name}', because it is a group of overloaded funs.",
                BoundFunRef boundFun => $"Cannot assign to '{boundFun.Fun.Name}', because it is a function.",
                BoundNamespaceRef boundNamespace => $"Cannot assign to '{boundNamespace.Namespace.Name}', because it is a namespace.",
                BoundTypeRef boundType => $"Cannot assign to '{boundType.Type.Name}', because it is a type.",
                
                BoundStmt => throw new UnreachableException("Non-value stmts can never be in assignment target position.")
            };

        private static BoundValue GetFirstPlace(BoundValue value) => value switch
        {
            BoundFieldAccess fieldAccess => GetFirstPlace(fieldAccess.Receiver),
            _ => value
        };
    }

    public sealed record CannotMutateReceiver(BoundValue Receiver, FunSymbol Fun, SyntaxNode FunSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [FunSyntax.Location];

        public override string Message => Receiver switch
        {
            { IsPlace: false } => $"'{Fun.Name}' is a var fun, but '{Receiver.Type.Name}' cannot be mutated through a temporary value.",
            { IsPlace: true, IsAssignable: false } => $"'{Fun.Name}' is a var fun, but '{Receiver.Type.Name}' cannot be mutated through a 'let' binding.",
            _ => throw new UnreachableException()
        };
    }

    public sealed record BreakOrContinueOutsideLoop(ExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.SyntaxElements().First().Location];

        public override string Message => Syntax is BreakExprSyntax
            ? "Break is only valid inside loops."
            : "Continue is only valid inside loops.";
    }

    public sealed record MissingElse(IfExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.SyntaxElements().First().Location];

        public override string Message
            => "'if' must have an 'else' branch if used as an expression.";
    }

    public sealed record IncompatibleBranches(BoundValue First, BoundValue Second) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [First.Syntax!.Location, Second.Syntax!.Location];

        public override string Message
            => $"Both 'if' and 'else' branches must have the same type. Got '{First.Type.Name}' and '{Second.Type.Name}'.";
    }

    public sealed record UndefinedMember(IdNameSyntax MemberSyntax, Symbol OwnerSymbol) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [MemberSyntax.Location];

        public override string Message =>
            $"'{OwnerSymbol.Name}' has no member '{MemberSyntax.Token.Identifier}'.";

    }

    public sealed record CannotConvert(ExprSyntax Syntax, TypeSymbol From, TypeSymbol To) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"Cannot convert from type '{From.Name}' to '{To.Name}'.";
    }

    public sealed record ArityMismatch(ArgListSyntax Syntax, string FunName, int? ParameterCount, int ArgumentCount) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                var argExprs = Syntax.Arguments.ToList();
                if (argExprs.Count == 0) return [Syntax.Location];
                
                var range = SourceRange.FromTo(argExprs[0].Location.Range, argExprs[^1].Location.Range);
                return [Syntax.Tree.SourceText.GetLocation(range)];
            }
        }

        public override string Message => ParameterCount is int parameterCount
            ? $"'{FunName}' has {parameterCount} parameter(s), but was called with {ArgumentCount} argument(s)."
            : $"'{FunName}' has no overload that takes {ArgumentCount} parameter(s).";
    }

    public sealed record CannotCall(BoundNode BoundCallee) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundCallee.Syntax!.Location];

        public override string Message => BoundCallee switch
        {
            BoundNamespaceRef boundNamespaceRef => $"Cannot call '{boundNamespaceRef.Namespace.Name}', because it is a namespace.",
            BoundTypeRef boundTypeRef => $"Cannot call '{boundTypeRef.Type.Name}', because it is a type.",
            BoundValue => $"Expected a function.",

            BoundFunGroupRef or BoundFunRef or BoundStmt => throw new UnreachableException(),
        };
    }

    public sealed record NumberTooBig(NumberLiteralSyntax Syntax, TypeSymbol TargetType) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message
            => $"The integral is too big to fit into '{TargetType.Name}'.";
    }

    public sealed record CannotCallWithoutReceiver(string FunName, bool IsOverloaded, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => IsOverloaded
            ? $"All overloads of '{FunName}' are methods. They cannot be called from a static context."
            : $"'{FunName}' is a method. It cannot be called from a static context.";
    }
    
    public sealed record CannotCallWithReceiver(string FunName, bool IsOverloaded, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => IsOverloaded
            ? $"All overloads of '{FunName}' are static. They cannot be called from an instance."
            : $"'{FunName}' is static. It cannot be called from an instance.";
    }

    public sealed record DuplicateFunDeclarations(ImmutableArray<FunSymbol> Duplicates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle the name and parameter list, since they are at fault.
            =>
            [
                .. Duplicates.Where(fun => fun.DeclarationSyntax is not null)
                    .Select(fun =>
                        fun.DeclarationSyntax!.Location.SourceText.GetLocation(SourceRange.FromTo(fun.DeclarationSyntax!.Name.Location.Range,
                            fun.DeclarationSyntax!.ParameterList.Location.Range)))
            ];

        public override string LocationLabel => "Also declared here.";

        public override string Message => $"'{Duplicates[0].Name}' with the same signature is already declared.";
    }

    public sealed record DuplicateParameters(ImmutableArray<ParameterSymbol> Duplicates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle all parameter names
            => [.. Duplicates.Where(param => param.DeclarationSyntax is not null).Select(param => param.DeclarationSyntax!.Name.Location)];

        public override string LocationLabel => "Also declared here.";

        public override string Message => $"Duplicate parameter '{Duplicates[0].Name}'.";
    }

    public sealed record CannotResolveFun(ArgListSyntax Syntax, string FunName, ImmutableArray<FunSymbol> Candidates, ImmutableArray<TypeSymbol> ArgumentTypes) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                var argExprs = Syntax.Arguments.ToList();
                if (argExprs.Count == 0) return [Syntax.Location];

                var range = SourceRange.FromTo(argExprs[0].Location.Range, argExprs[^1].Location.Range);
                return [Syntax.Tree.SourceText.GetLocation(range)];
            }
        }

        public override string Message => $"""
                                           No overload of '{FunName}' takes arguments {GetTypeListString(ArgumentTypes)}'.
                                           Candidates:
                                           {GetCandidateString()}
                                           """;

        private string GetTypeListString(IEnumerable<TypeSymbol> types)
            => $"({string.Join(", ", types.Select(argType => argType.Name))})";
        
        private string GetCandidateString()
        {
            return string.Join('\n', Candidates.Take(5).Select(Single));
            
            string Single(FunSymbol fun)
                => string.Concat(fun.ReceiverType is null ? "static fun " : "fun ",
                    $"'{FunName} '",
                    GetTypeListString(fun.ParameterTypes));
        }
    }

    public sealed record CannotShadow(Symbol ShadowedSymbol, IdentifierToken NameSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [NameSyntax.Location];

        public override string Message
            => $"'{ShadowedSymbol.Name}' is already declared on this scope. Only variables can be shadowed.";
    }

    public sealed record MissingReturnValue(ReturnExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.SyntaxElements().First().Location];
        public override string Message => "Return value is missing.";
    }

    public sealed record CannotCapture(IdNameSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"Cannot capture variable '{Syntax.Token.Identifier}' from outside of this fun.";
    }

    public sealed record MissingReturn(SyntaxNode BlockSyntax, ExprSyntax ReturnTypeAnnotationSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                if (BlockSyntax is BlockExprSyntax blockExprSyntax &&
                    blockExprSyntax.SyntaxElements().First(el => el is Token { Kind: TokenKind.CloseBrace })
                        is Token { IsMissing: false } closeBraceToken)
                {
                    return [closeBraceToken.Location];
                }

                return [BlockSyntax.Location];
            }
        }

        public override string Message
            => "Not all code-paths return a value.";

        public override ImmutableArray<LabeledSourceLocation> Related
            => [new(ReturnTypeAnnotationSyntax.Location, "Return type declared here.")];
    }

    public sealed record CannotAccessThroughInstance(IdNameSyntax MemberSyntax, TypeSymbol ParentSymbol) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [MemberSyntax.Location];
        public override string Message => $"Cannot access nested type '{MemberSyntax.Token.Identifier}' through a value. Use '{ParentSymbol.Name}.{MemberSyntax.Token.Identifier}' instead.";
    }

    public sealed record NotAValue(BoundNode BoundNode) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundNode.Syntax!.Location];
        public override string Message => $"Expected a value.";
    }

    public sealed record NotAType(BoundNode BoundNode) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundNode.Syntax!.Location];
        public override string Message => $"Expected a value.";
    }

    public sealed record CannotAccessFieldWithoutReceiver(SyntaxNode Syntax, FieldSymbol FieldSymbol) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"Cannot access field '{FieldSymbol.Name}' in static context.";
    }

    public sealed record AlreadyDeclared(ImmutableArray<Symbol> OffendingSymbols) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle the name and parameter list, since they are at fault.
            =>
            [
                .. OffendingSymbols.Where(symbol => GetDeclarationNameSyntax(symbol) is not null)
                    .Select(symbol => GetDeclarationNameSyntax(symbol)!.Location)
            ];

        private static SyntaxElement? GetDeclarationNameSyntax(Symbol symbol) => symbol switch
        {
            StructSymbol { DeclarationSyntax: not null } structSymbol => structSymbol.DeclarationSyntax.Name,
            FunSymbol { DeclarationSyntax: not null } funSymbol => funSymbol.DeclarationSyntax.Name,
            FieldSymbol { DeclarationSyntax: not null } fieldSymbol => fieldSymbol.DeclarationSyntax.Name,
            _ => null
        };
        
        public override string LocationLabel => "Also declared here.";

        public override string Message => $"'{OffendingSymbols[0].Name}' is already declared.";
    }

    public sealed record RecursiveStructLayout(StructSymbol StructSymbol, ImmutableArray<FieldSymbol> FieldPath) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [FieldPath[0].DeclarationSyntax!.TypeExpr.Location];
        public override string Message => $"Struct '{StructSymbol.Name}' has a cycle in it's layout. The cycle is through fields {GetCycleString()}";

        private string GetCycleString()
            => string.Join(" -> ", FieldPath.Select(field => $"'{field.Owner!.Name}.{field.Name}: {field.Type.Name}'"));
    }

    public sealed record InvalidFieldName(StructSymbol Struct, ArgSyntax ArgSyntax, FieldSymbol? Field = null) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [ArgSyntax.Name?.Location ?? ArgSyntax.Location];

        public override string Message => Field is not null
            ? $"Field {Struct.Name}.{Field.Name} is private."
            : ArgSyntax.Name is null
                ? "Field name must be specified."
                : $"'{Struct.Name}' has no field with name '{ ArgSyntax.Name!.Text }'.";
    }

    public sealed record MissingFieldInitializers(
        ImmutableArray<FieldSymbol> MissingFields,
        ArgListSyntax ArgListSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [ArgListSyntax.Location];
        public override string Message => $"All fields must be initialized. Missing are {GetFieldNameString()}.";

        private string GetFieldNameString()
            => MissingFields.Length == 1
                ? $"'{MissingFields[0].Name}'"
                : $"{string.Join(", ", MissingFields[..^1].Select(f => f.Name))} and '{MissingFields[^1].Name}'";
    }

    public record FieldAlreadyInitialized(ArgSyntax ArgSyntax, FieldSymbol Field) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [ArgSyntax.IsNamed ? ArgSyntax.Name!.Location : ArgSyntax.Location];
        public override string Message => $"Field '{Field.Name}' is already initialized.";
    }

    public sealed record CannotInitializePrimitive(SyntaxNode StructRefSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [StructRefSyntax.Location];

        public override string Message => $"'{StructRefSyntax.Text}' cannot be initialized, because it is a primitive type.";
    }

    public sealed record NotAccessible(Symbol Symbol, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => Symbol switch
        {
            FieldSymbol fieldSymbol => $"Field '{fieldSymbol.Owner!.Name}.{fieldSymbol.Name}' not accessible, because it is private.", 
            _ => $"'{Symbol.Name}' is not accessible because of its protection level."
        };
    }

    public sealed record InvalidModifier(Token Syntax, Symbol BindingOwner) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => BindingOwner switch
        {
            FunSymbol => $"'{Syntax.Text}' modifier is not valid on local funs.",
            _ => $"'{Syntax.Text}' modifier is not valid here.",
        };
    }


    public sealed record StaticFunCannotBeVar(Token VarKwSyntax, IdentifierToken FunName) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [VarKwSyntax.Location];

        public override string Message =>
            $"'{FunName}' cannot be declared '{VarKwSyntax.Text}', because it is static.";
    }

    public sealed record InvalidSelfRef(SelfSyntax Syntax, Symbol BindingOwner) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message
        {
            get
            {
                if (BindingOwner is FunSymbol { ReceiverType: null }
                    || BindingOwner.OwningType is null)
                    return "Cannot access self in static context.";

                if (BindingOwner is FunSymbol fun && fun.ReceiverType != BindingOwner.OwningType)
                    return "Weirdly enough, receiver of this fun has different type than its owning type.";
                
                if (BindingOwner.Owner != BindingOwner.OwningType)
                    return "Cannot access self from local funs of methods.";

                return "Cannot access self for some reason.";
            }
        }
    }
}