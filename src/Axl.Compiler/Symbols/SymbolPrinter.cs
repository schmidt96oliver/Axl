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