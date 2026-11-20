using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Text;

namespace AxlCompiler.Symbols;

public static class SymbolPrinter
{
    extension(Symbol symbol)
    {
        public string KindName => symbol switch
        {
            ErrorTypeSymbol => "error",

            ParameterSymbol => "parameter",
            VariableSymbol => "variable",
            FieldSymbol => "field",
            ConstSymbol => "const",

            FunSymbol { IsOperator: false } => "fun",
            FunSymbol { IsOperator: true } => "operator",

            TypeSymbol type => type switch
            {
                NamedTypeSymbol { Definition: var definition } =>  definition switch
                {
                    StructDefinition => "struct",
                    ClassDefinition => "class"
                },
                
                TypeParameterSymbol => "type parameter",
                ErrorTypeSymbol => "error",
                RefTypeSymbol refType => $"ref {refType.Inner.KindName}",
            },
            
            NamespaceSymbol => "namespace",
        };

        /// <summary>
        /// The qualified name with type parameters/arguments
        /// if generic.
        /// </summary>
        public string FullName
        {
            get
            {
                if (symbol is ErrorTypeSymbol) return "???";
                if (symbol is TypeParameterSymbol) return symbol.Name;
                return string.Join(".", symbol.SelfAndOwners()
                    .Where(s => s.CanBeReferencedByName)
                    .Reverse()
                    .Select(s => s switch
                    {
                        TypeSymbol type => $"{type.Name}{type.TypeArguments.FullTypeListDisplayText}",
                        FunSymbol fun => $"{fun.Name}{fun.TypeArguments.FullTypeListDisplayText}",
                        _ => s.Name
                    }));
            }
        }
        
        /// <summary>
        /// The full name including symbols in the path that are not
        /// referencable by name.
        /// </summary>
        public string FullInternalName
        {
            get
            {
                if (symbol is ErrorTypeSymbol) return "???";
                if (symbol is TypeParameterSymbol) return symbol.Name;
                return string.Join(".", symbol.SelfAndOwners()
                    .Reverse()
                    .Skip(1)        // First symbol is always root or script
                    .Select(s => s switch
                    {
                        TypeSymbol type => $"{type.Name}{type.TypeArguments.FullTypeListDisplayText}",
                        FunSymbol fun => $"{fun.Name}{fun.TypeArguments.FullTypeListDisplayText}",
                        _ => s.Name
                    }));
            }
        }
        
        /// <summary>
        /// The full name, but without the symbols own type arguments.
        /// </summary>
        public string FullNameWithoutOwnTypeArgs
        {
            get
            {
                if (symbol is ErrorTypeSymbol) return "???";
                if (symbol is TypeParameterSymbol) return symbol.Name;
                return string.Join(".", symbol.Owners()
                    .Where(s => s.CanBeReferencedByName)
                    .Reverse()
                    .Select(s => s switch
                    {
                        TypeSymbol type => $"{type.Name}{type.TypeArguments.FullTypeListDisplayText}",
                        FunSymbol fun => $"{fun.Name}{fun.TypeArguments.FullTypeListDisplayText}",
                        _ => s.Name
                    })
                    .Concat([symbol.Name]));
            }
        }
        
        /// <summary>
        /// A string like <c>fun 'A'</c>.
        /// </summary>
        public string ShortDisplayText => symbol switch
        {
            ErrorTypeSymbol => "error",
            ParameterSymbol parameter => $"parameter {parameter.Name}",
            VariableSymbol { Kind: VariableKind.Var } variable => $"var {variable.Name}",
            VariableSymbol { Kind: VariableKind.Let } variable => $"let {variable.Name}",
            _ => $"{symbol.KindName} '{symbol.FullName}'"
        };

        public string FullDisplayText => symbol switch
        {
            ParameterSymbol parameter => $"parameter {parameter.Name}: {parameter.ParameterTypeFullDisplayText}",
            VariableSymbol { Kind: VariableKind.Var } variable => $"var {variable.Name}: {variable.Type.FullName}",
            VariableSymbol variable => $"let {variable.Name}: {variable.Type.FullName}",
            ConstSymbol constSymbol => $"const {constSymbol.FullName}: {constSymbol.Type.FullName}",
            FieldSymbol fieldSymbol => $"field {fieldSymbol.FullName}: {fieldSymbol.Type.FullName}",
            
            NamespaceSymbol or TypeSymbol => $"{symbol.KindName} {symbol.FullName}",
            
            FunSymbol fun => $"{fun.KindName} {fun.FullName}{ fun.FullParameterDisplayText }",
        };

        public string MinimalDisplayText(SemanticModel model, SourceLocation location) => symbol switch
        {
            ParameterSymbol parameter => $"parameter {parameter.Name}: {parameter.ParameterTypeMinimalDisplayText(model, location)}",
            VariableSymbol { Kind: VariableKind.Var } variable => $"var {variable.Name}: {variable.Type.MinimalDisplayName(model, location)}",
            VariableSymbol variable => $"let {variable.Name}: {variable.Type.MinimalDisplayName(model, location)}",
            ConstSymbol constSymbol => $"const {constSymbol.MinimalDisplayName(model, location)}: {constSymbol.Type.MinimalDisplayName(model, location)}",
            FieldSymbol fieldSymbol => $"field {fieldSymbol.MinimalDisplayName(model, location)}: {fieldSymbol.Type.MinimalDisplayName(model, location)}",

            NamespaceSymbol or TypeSymbol => $"{symbol.KindName} {symbol.MinimalDisplayName(model, location)}",

            FunSymbol fun => $"{fun.KindName} {fun.MinimalDisplayName(model, location)}{fun.MinimalParameterDisplayText(model, location)}",
        };
        
        public string MinimalDisplayName(SemanticModel semanticModel, SourceLocation location)
        {
            if (symbol is ErrorTypeSymbol) return "???";
            if (symbol is TypeParameterSymbol) return symbol.Name;

            if (semanticModel.TryScopeAtUnmapped(location) is not { } unmappedScope)
                return symbol.FullName;

            return symbol.MinimalDisplayName(unmappedScope);
        }
        
        private string MinimalDisplayName(Scope unmappedScope)
        {
            var symbolParts = symbol.SelfAndOwners()
                .Where(s => s.CanBeReferencedByName)
                .Reverse()
                .ToImmutableArray();

            var binder = new Binder(unmappedScope, new BinderSideEffects());
            for (var partCount = 1; partCount <= symbolParts.Length; partCount++)
            {
                // Lookup each thing A.B.C
                var parts = symbolParts[^partCount..];
                var current = Lookup(parts[0].Name, null);
                for (var i = 1; i < parts.Length && current is not null; i++)
                    current = Lookup(parts[i].Name, current);

                if (current?.OriginalDefinition.FullName == symbol.OriginalDefinition.FullName)
                {
                    // Use those parts!
                    return string.Join('.', parts.Select(s => s switch
                    {
                        TypeSymbol { TypeParameters.Length: > 0 } type =>
                            $"{type.Name}[{string.Join(", ", type.TypeArguments.Select(t => t.MinimalDisplayName(unmappedScope)))}]",
                        FunSymbol { TypeParameters.Length: > 0 } fun =>
                            $"{fun.Name}[{string.Join(", ", fun.TypeArguments.Select(p => p.MinimalDisplayName(unmappedScope)))}]",
                        _ => s.Name
                    }));
                }
            }

            return symbol.FullName;

            Symbol? Lookup(string name, Symbol? parent)
            {
                var result = parent is null
                    ? binder.LookupName(name, LookupOptions.Any)
                    : binder.LookupMembers(parent, name, LookupOptions.Any);
                if (result.SingleViable is not null)
                    return result.SingleViable;
                if (result.Viability is LookupViability.Viable && result.Symbols.All(s => s is FunSymbol))
                    return result.Symbols[0];
                return null;
            }
        }
    }

    extension(FunSymbol fun)
    {
        public string ShortDisplayTextWithParameterTypes => $"{fun.KindName} {fun.FullName}({
            string.Join(", ", fun.Parameters.Select(get_ParameterTypeFullDisplayText))})";

        private string FullParameterDisplayText
            => $"({string.Join(", ", fun.Parameters.Select(p => $"{p.Name}: {p.ParameterTypeFullDisplayText}"))})";
        private string MinimalParameterDisplayText(SemanticModel model, SourceLocation location)
            => $"({string.Join(", ", fun.Parameters.Select(p => $"{p.Name}: {p.ParameterTypeMinimalDisplayText(model, location)}"))})";
    }

    extension(ImmutableArray<TypeSymbol> typeList)
    {
        private string FullTypeListDisplayText
            => typeList.Length > 0
                ? $"[{string.Join(", ", typeList.Select(p => $"{p.FullName}"))}]"
                : "";
    }

    extension(ParameterSymbol parameter)
    {
        public string ParameterTypeFullDisplayText
            => parameter.IsMut ? $"mut {parameter.Type.FullName}" : parameter.Type.FullName;
        
        public string ParameterTypeMinimalDisplayText(SemanticModel model, SourceLocation location)
            => parameter.IsMut ? $"mut {parameter.Type.MinimalDisplayName(model, location)}" : parameter.Type.MinimalDisplayName(model, location);
    }
    extension(BoundArgument argument)
    {
        public string ArgumentTypeFullDisplayText
            => argument.IsMut ? $"mut {argument.Type.FullName}" : argument.Type.FullName;


        public string ArgumentTypeMinimalDisplayText(SemanticModel model, SourceLocation location)
            => argument.IsMut
                ? $"mut {argument.Type.MinimalDisplayName(model, location)}"
                : argument.Type.MinimalDisplayName(model, location);
    }

    extension(ImmutableArray<BoundArgument> args)
    {
        public string ArgumentTypesDisplayText
            => string.Join(", ", args.Select(get_ArgumentTypeFullDisplayText));
    }
}