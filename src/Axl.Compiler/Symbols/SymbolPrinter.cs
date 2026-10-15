namespace Axl.Compiler.Symbols;

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

            FunSymbol { IsOperator: false } => "fun",
            FunSymbol { IsOperator: true } => "operator",

            NamespaceSymbol => "namespace",
            StructSymbol => "struct",
            TypeSymbol => "type",
        };
        
        /// <summary>
        /// A string like <c>fun 'A'</c>.
        /// </summary>
        public string ShortDisplayName => symbol switch
        {
            ErrorTypeSymbol => "error",
            FieldSymbol fieldSymbol => $"field '{fieldSymbol.OwningType?.Name ?? "???"}.{fieldSymbol.Name}'",
            
            _ => $"{symbol.KindName} '{symbol.Name}'"
        };

        public string FullDisplayName => symbol switch
        {
            ParameterSymbol parameter => $"parameter {parameter.Name}: {parameter.Type.Name}",
            VariableSymbol { IsReadOnly: false } variable => $"var {variable.Name}: {variable.Type.Name}",
            VariableSymbol { IsReadOnly: true } variable => $"let {variable.Name}: {variable.Type.Name}",
            FieldSymbol fieldSymbol => $"field {fieldSymbol.Owner?.Name ?? "???"}.{fieldSymbol.Name}: {fieldSymbol.Type.Name}",
            
            NamespaceOrTypeSymbol => $"{symbol.KindName} {symbol.Name}",
            
            FunSymbol fun => $"{fun.KindName} {fun.Name}{ GetParameterText(fun) }{ GetFunAnnotationText(fun) }",
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
        public string DisplayNameWithParameterTypes => $"{fun.KindName} {fun.Name}({fun.ParameterTypes.TypeListString})";
    }

    extension(IEnumerable<TypeSymbol> types)
    {
        public string TypeListString
            => string.Join(", ", types.Select(argType => argType.Name));
    }
}