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

        public string QualifiedName
        {
            get
            {
                if (symbol is NeverTypeSymbol) return "never";
                if (symbol is ErrorTypeSymbol) return "???";
                return string.Join(".", symbol.SelfAndOwners()
                    .Where(s => s.CanBeReferencedByName)
                    .Reverse()
                    .Select(s => s.Name));
            }
        }

        /// <summary>
        /// A string like <c>fun 'A'</c>.
        /// </summary>
        public string ShortDisplayName => symbol switch
        {
            ErrorTypeSymbol => "error",
            ParameterSymbol parameter => $"parameter {parameter.Name}",
            VariableSymbol { IsReadOnly: false } variable => $"var {variable.Name}",
            VariableSymbol { IsReadOnly: true } variable => $"let {variable.Name}",
            _ => $"{symbol.KindName} '{symbol.QualifiedName}'"
        };

        public string FullDisplayName => symbol switch
        {
            ParameterSymbol parameter => $"parameter {parameter.Name}: {parameter.Type.QualifiedName}",
            VariableSymbol { IsReadOnly: false } variable => $"var {variable.Name}: {variable.Type.QualifiedName}",
            VariableSymbol { IsReadOnly: true } variable => $"let {variable.Name}: {variable.Type.QualifiedName}",
            FieldSymbol fieldSymbol => $"field {fieldSymbol.QualifiedName}: {fieldSymbol.Type.QualifiedName}",
            
            NamespaceOrTypeSymbol => $"{symbol.KindName} {symbol.QualifiedName}",
            
            FunSymbol fun => $"{fun.KindName} {fun.QualifiedName}{ GetParameterText(fun) }{ GetFunAnnotationText(fun) }",
        };

        private static string GetParameterText(FunSymbol fun)
            => $"({string.Join(", ", fun.Parameters.Select(param => $"{param.Name}: {param.Type.QualifiedName}"))})";

        private static string GetFunAnnotationText(FunSymbol fun)
            => (fun.Body is Intrinsic intrinsic ? $" @intrinsic {intrinsic.ToString()}" : "") +
               (fun is SyntheticFunSymbol ? " @synthetic" : "");
    }

    extension(FunSymbol fun)
    {
        public string DisplayNameWithParameterTypes => $"{fun.KindName} {fun.QualifiedName}({fun.ParameterTypes.TypeListString})";
    }

    extension(IEnumerable<TypeSymbol> types)
    {
        public string TypeListString
            => string.Join(", ", types.Select(argType => argType.Name));
    }
}