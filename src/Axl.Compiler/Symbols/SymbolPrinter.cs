using System.Collections.Immutable;
using Axl.Compiler.Binding.BoundTree;

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
            TypeParameterSymbol => "generic type",
            TypeSymbol => "type",
        };

        public string QualifiedName
        {
            get
            {
                if (symbol is ErrorTypeSymbol) return "???";
                if (symbol is TypeParameterSymbol) return symbol.Name;
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
            ParameterSymbol parameter => $"parameter {parameter.Name}: {parameter.TypeDisplayString}",
            VariableSymbol { IsReadOnly: false } variable => $"var {variable.Name}: {variable.Type.QualifiedName}",
            VariableSymbol { IsReadOnly: true } variable => $"let {variable.Name}: {variable.Type.QualifiedName}",
            FieldSymbol fieldSymbol => $"field {fieldSymbol.QualifiedName}: {fieldSymbol.Type.QualifiedName}",
            
            NamespaceOrTypeSymbol => $"{symbol.KindName} {symbol.QualifiedName}",
            
            FunSymbol fun => $"{fun.KindName} {fun.QualifiedName}{ fun.TypeParameterText }{ fun.ParameterText }",
        };
    }

    extension(FunSymbol fun)
    {
        public string DisplayNameWithParameterTypes => $"{fun.KindName} {fun.QualifiedName}{fun.TypeParameterText}({
            string.Join(", ", fun.Parameters.Select(get_TypeDisplayString))})";

        public string ParameterText
            => $"({string.Join(", ", fun.Parameters.Select(p => $"{p.Name}: {p.TypeDisplayString}"))})";

        public string TypeParameterText
            => fun.TypeParameters.Length > 0
                ? fun.IsConstructed
                    ? $"[{string.Join(", ", fun.TypeArguments.Select(p => $"{p.QualifiedName}"))}]"
                    : $"[{string.Join(", ", fun.TypeParameters.Select(p => $"{p.Name}"))}]"
                : "";
    }

    extension(ParameterSymbol parameter)
    {
        public string TypeDisplayString
            => parameter.IsMut ? $"mut {parameter.Type.QualifiedName}" : parameter.Type.QualifiedName;
    }
    extension(BoundArgument argument)
    {
        public string TypeDisplayString
            => argument.IsMut ? $"mut {argument.Type.QualifiedName}" : argument.Type.QualifiedName;
    }

    extension(ImmutableArray<BoundArgument> args)
    {
        public string TypeListString
            => string.Join(", ", args.Select(get_TypeDisplayString));
    }
}