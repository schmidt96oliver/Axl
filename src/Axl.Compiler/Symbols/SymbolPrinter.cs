namespace Axl.Compiler.Symbols;

public static class SymbolPrinter
{
    extension(Symbol symbol)
    {
        /// <summary>
        /// A string like <c>fun 'A'</c>.
        /// </summary>
        public string ShortDisplayName => symbol switch
        {
            ErrorTypeSymbol => "error",
            
            ParameterSymbol => $"parameter '{symbol.Name}'",
            VariableSymbol => $"variable '{symbol.Name}'",
            FieldSymbol fieldSymbol => $"field '{fieldSymbol.OwningType?.Name ?? "???"}.{fieldSymbol.Name}'",
            
            FunSymbol => $"fun '{symbol.Name}'",
            
            NamespaceSymbol => $"namespace '{symbol.Name}'",
            StructSymbol => $"struct '{symbol.Name}'",
            TypeSymbol => $"type '{symbol.Name}'",
        };

        public string FullDisplayName => symbol switch
        {
            ParameterSymbol parameter => $"parameter {parameter.Name}: {parameter.Type.Name}",
            VariableSymbol { IsReadOnly: false } variable => $"var {variable.Name}: {variable.Type.Name}",
            VariableSymbol { IsReadOnly: true } variable => $"let {variable.Name}: {variable.Type.Name}",
            FieldSymbol fieldSymbol => $"field {fieldSymbol.Owner?.Name ?? "???"}.{fieldSymbol.Name}: {fieldSymbol.Type.Name}",
            
            NamespaceSymbol ns => $"namespace {ns.Name}",
            StructSymbol @struct => $"struct {@struct.Name}",
            TypeSymbol type => $"type {type.Name}",
            
            FunSymbol fun => $"fun {fun.Name}{ GetParameterText(fun) }{ GetFunAnnotationText(fun) }",
            _ => "??? Hover not implemented"
        };

        private static string GetParameterText(FunSymbol fun)
            => $"({string.Join(", ", fun.Parameters.Select(param => $"{param.Name}: {param.Type.Name}"))})";

        private static string GetFunAnnotationText(FunSymbol fun)
            => fun.Body is Intrinsic intrinsic
                ? $" @intrinsic {intrinsic.ToString()}"
                : fun.DeclarationSyntax is null
                    ? " @compiler-generated"
                    : "";
    }

    extension(FunSymbol fun)
    {
        public string DisplayNameWithParameterTypes => $"fun {fun.Name}({ fun.ParameterTypes.TypeListString })";
    }

    extension(IEnumerable<TypeSymbol> types)
    {
        public string TypeListString
            => string.Join(", ", types.Select(argType => argType.Name));
    }
}