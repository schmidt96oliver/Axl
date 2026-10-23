using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;

namespace Axl.Compiler;

public enum BaseTypeKind
{
    // This entry must match the expected name inside Base namespace.
    
    Bool,
    I32,
    I64,
    F32,
    F64,
    
    String,
    Unit
}

public sealed class BaseLibrary(Compilation compilation)
{
    private readonly Dictionary<BaseTypeKind, TypeSymbol> _baseTypes = [];
    private readonly DiagnosticBag _diagnosticBag = new();

    public NamespaceSymbol BaseNamespace
    {
        get
        {
            if (field is not null)
                return field;

            var lookupResult = compilation.RootNamespace.LookupMembers(Names.BaseNamespace)
                .Concat(compilation
                    .ReferencedCompilations
                    .SelectMany(c => c.RootNamespace.LookupMembers(Names.BaseNamespace)))
                .OfType<NamespaceSymbol>()
                .ToImmutableArray();
            if (lookupResult.Length != 1)
            {
                _diagnosticBag.ReportError(new Diagnostic.CannotFindBaseNamespace(lookupResult));
                
                if (lookupResult.Length == 0)
                {
                    field = new MissingBaseNamespaceSymbol(compilation.RootNamespace);
                    return field;
                }
            }

            field = lookupResult[0];
            return field;
        }
    }

    public TypeSymbol I32 => Get(BaseTypeKind.I32);
    public TypeSymbol I64 => Get(BaseTypeKind.I64);
    public TypeSymbol F32 => Get(BaseTypeKind.F32);
    public TypeSymbol F64 => Get(BaseTypeKind.F64);
    public TypeSymbol Bool => Get(BaseTypeKind.Bool);
    public TypeSymbol String => Get(BaseTypeKind.String);
    public TypeSymbol Unit => Get(BaseTypeKind.Unit);

    public TypeSymbol DefaultIntType => I32;
    public TypeSymbol DefaultFloatType => F64;
    
    public TypeSymbol Get(BaseTypeKind kind)
    {
        if (_baseTypes.TryGetValue(kind, out var symbol))
            return symbol;

        var name = kind.ToString();
        var lookupResult = BaseNamespace.LookupMembers(name);
        if (lookupResult is not [TypeSymbol])
        {
            if (BaseNamespace is not MissingBaseNamespaceSymbol)
                _diagnosticBag.ReportError(new Diagnostic.CannotFindBaseType(kind, lookupResult));
            _baseTypes[kind] = ErrorTypeSymbol.Instance;
            return ErrorTypeSymbol.Instance;
        }

        symbol = (TypeSymbol)lookupResult[0];
        _baseTypes[kind] = symbol;
        return symbol;
    }

    public void Complete(DiagnosticBag diagnosticBag)
    {
        _ = BaseNamespace;
        foreach (var baseKind in Enum.GetValues<BaseTypeKind>())
            _ = Get(baseKind);
        
        _diagnosticBag.DrainInto(diagnosticBag);
    }
}