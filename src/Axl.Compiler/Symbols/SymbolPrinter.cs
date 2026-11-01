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
        public string ParameterTypeDisplayText
            => parameter.IsMut ? $"mut {parameter.Type.FullName}" : parameter.Type.FullName;
    }
    extension(BoundArgument argument)
    {
        public string ArgumentTypeDisplayText
            => argument.IsMut ? $"mut {argument.Type.FullName}" : argument.Type.FullName;
    }

    extension(ImmutableArray<BoundArgument> args)
    {
        public string ArgumentTypesDisplayText
            => string.Join(", ", args.Select(get_ArgumentTypeDisplayText));
    }
}