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

        /// <summary>
        /// The full path to this symbol. Excluding type parameters/arguments.
        /// </summary>
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
        /// The qualified name with type parameters/arguments
        /// if generic.
        /// </summary>
        public string FullName => symbol switch
        {
            FunSymbol { TypeParameters.Length: > 0 } fun => $"{symbol.QualifiedName}{fun.FullTypeParameterDisplayText}",
            _ => symbol.QualifiedName
        };

        /// <summary>
        /// A string like <c>fun 'A'</c>.
        /// </summary>
        public string ShortDisplayText => symbol switch
        {
            ErrorTypeSymbol => "error",
            ParameterSymbol parameter => $"parameter {parameter.Name}",
            VariableSymbol { IsReadOnly: false } variable => $"var {variable.Name}",
            VariableSymbol { IsReadOnly: true } variable => $"let {variable.Name}",
            _ => $"{symbol.KindName} '{symbol.FullName}'"
        };

        public string FullDisplayText => symbol switch
        {
            ParameterSymbol parameter => $"parameter {parameter.Name}: {parameter.ParameterTypeDisplayText}",
            VariableSymbol { IsReadOnly: false } variable => $"var {variable.Name}: {variable.Type.FullName}",
            VariableSymbol { IsReadOnly: true } variable => $"let {variable.Name}: {variable.Type.FullName}",
            FieldSymbol fieldSymbol => $"field {fieldSymbol.FullName}: {fieldSymbol.Type.FullName}",
            
            NamespaceOrTypeSymbol => $"{symbol.KindName} {symbol.FullName}",
            
            FunSymbol fun => $"{fun.KindName} {fun.FullName}{ fun.FullParameterDisplayText }",
        };
    }

    extension(FunSymbol fun)
    {
        public string ShortDisplayTextWithParameterTypes => $"{fun.KindName} {fun.FullName}({
            string.Join(", ", fun.Parameters.Select(get_ParameterTypeDisplayText))})";

        private string FullParameterDisplayText
            => $"({string.Join(", ", fun.Parameters.Select(p => $"{p.Name}: {p.ParameterTypeDisplayText}"))})";

        private string FullTypeParameterDisplayText
            => fun.TypeParameters.Length > 0
                ? fun.TypeArguments.Length == fun.TypeParameters.Length
                    ? $"[{string.Join(", ", fun.TypeArguments.Select(p => $"{p.QualifiedName}"))}]"
                    : $"[{string.Join(", ", fun.TypeParameters.Select(p => $"{p.Name}"))}]"
                : "";
    }

    extension(ParameterSymbol parameter)
    {
        public string ParameterTypeDisplayText
            => parameter.IsMut ? $"mut {parameter.Type.QualifiedName}" : parameter.Type.QualifiedName;
    }
    extension(BoundArgument argument)
    {
        public string ArgumentTypeDisplayText
            => argument.IsMut ? $"mut {argument.Type.QualifiedName}" : argument.Type.QualifiedName;
    }

    extension(ImmutableArray<BoundArgument> args)
    {
        public string ArgumentTypesDisplayText
            => string.Join(", ", args.Select(get_ArgumentTypeDisplayText));
    }
}