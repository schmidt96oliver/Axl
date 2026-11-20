using System.Collections.Frozen;
using System.Collections.Immutable;
using AxlCompiler.Diagnostics;
using AxlCompiler.Symbols;
// ReSharper disable InconsistentNaming

namespace AxlCompiler;

public enum KnownName
{
    I32,
    I64,
    F32,
    F64,
    Bool,
    String,
    Unit, 
    Never,
    UnsafeBuffer_T
}

public sealed class BaseLibrary(Compilation compilation)
{
    private static readonly FrozenDictionary<KnownName, string> KnownNames = new Dictionary<KnownName, string>
    {
        [KnownName.Bool] = "Bool",
        [KnownName.String] = "String",
        [KnownName.I32] = "I32",
        [KnownName.I64] = "I64",
        [KnownName.F32] = "F32",
        [KnownName.F64] = "F64",
        [KnownName.Never] = "Never",
        [KnownName.Unit] = "Unit",
        [KnownName.UnsafeBuffer_T] = "UnsafeBuffer"
    }.ToFrozenDictionary();
    
    public static readonly ImmutableHashSet<string> KnownPrimitives =
    [
        "Base.Bool",
        "Base.String",
        "Base.I32",
        "Base.I64",
        "Base.F32",
        "Base.F64",
        "Base.Never",
        "Base.UnsafeBuffer[T]"
    ];
    
    public static readonly FrozenDictionary<string, Intrinsic> KnownIntrinsics = new Dictionary<string, Intrinsic>
    {
        ["Base.Print"] = Intrinsic.Print,
        ["Base.Panic"] = Intrinsic.Panic,
        ["Base.UnsafeBuffer[T].New"] = Intrinsic.BufferNew,
        ["Base.UnsafeBuffer[T].At"] = Intrinsic.BufferAt,
        
        ["Base.Bool.!"] = Intrinsic.NotBool,
        ["Base.Bool.=="] = Intrinsic.EqualsBool,
        ["Base.Bool.ToString"] = Intrinsic.ToStringBool,
        
        ["Base.I32.AddWrapped"] = Intrinsic.AddWrappedI32,
        ["Base.I32.SubWrapped"] = Intrinsic.SubWrappedI32,
        ["Base.I32.MulWrapped"] = Intrinsic.MulWrappedI32,
        ["Base.I32.DivUnchecked"] = Intrinsic.DivUncheckedI32,
        ["Base.I32.<"] = Intrinsic.LessThanI32,
        ["Base.I32.=="] = Intrinsic.EqualsI32,
        ["Base.I32.ToString"] = Intrinsic.ToStringI32,
        
        ["Base.I64.AddWrapped"] = Intrinsic.AddWrappedI64,
        ["Base.I64.SubWrapped"] = Intrinsic.SubWrappedI64,
        ["Base.I64.MulWrapped"] = Intrinsic.MulWrappedI64,
        ["Base.I64.DivUnchecked"] = Intrinsic.DivUncheckedI64,
        ["Base.I64.<"] = Intrinsic.LessThanI64,
        ["Base.I64.=="] = Intrinsic.EqualsI64,
        ["Base.I64.ToString"] = Intrinsic.ToStringI64,
        
        ["Base.F32.+"] = Intrinsic.AddF32,
        ["Base.F32.-"] = Intrinsic.SubF32,
        ["Base.F32.*"] = Intrinsic.MulF32,
        ["Base.F32./"] = Intrinsic.DivF32,
        ["Base.F32.<"] = Intrinsic.LessThanF32,
        ["Base.F32.=="] = Intrinsic.EqualsF32,
        ["Base.F32.ToString"] = Intrinsic.ToStringF32,
        
        ["Base.F64.+"] = Intrinsic.AddF64,
        ["Base.F64.-"] = Intrinsic.SubF64,
        ["Base.F64.*"] = Intrinsic.MulF64,
        ["Base.F64./"] = Intrinsic.DivF64,
        ["Base.F64.<"] = Intrinsic.LessThanF64,
        ["Base.F64.=="] = Intrinsic.EqualsF64,
        ["Base.F64.ToString"] = Intrinsic.ToStringF64,
        
        ["Base.String.=="] = Intrinsic.EqualsString,
        ["Base.String.Concat"] = Intrinsic.ConcatString,
    }.ToFrozenDictionary();
    
    
    private readonly Dictionary<KnownName, TypeSymbol> _baseTypes = [];
    private readonly DiagnosticBag _diagnosticBag = new();

    public ErrorTypeSymbol Error { get; } = new(compilation);


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

    public TypeSymbol I32 => Get(KnownName.I32);
    public TypeSymbol I64 => Get(KnownName.I64);
    public TypeSymbol F32 => Get(KnownName.F32);
    public TypeSymbol F64 => Get(KnownName.F64);
    public TypeSymbol Bool => Get(KnownName.Bool);
    public TypeSymbol String => Get(KnownName.String);
    public TypeSymbol Unit => Get(KnownName.Unit);
    public TypeSymbol Never => Get(KnownName.Never);

    public TypeSymbol DefaultIntType => I32;
    public TypeSymbol DefaultFloatType => F64;
    
    private TypeSymbol Get(KnownName kind)
    {
        if (_baseTypes.TryGetValue(kind, out var symbol))
            return symbol;

        var name = KnownNames[kind];
        var lookupResult = BaseNamespace.LookupMembers(name);
        if (lookupResult is not [TypeSymbol single])
        {
            if (BaseNamespace is not MissingBaseNamespaceSymbol)
                _diagnosticBag.ReportError(new Diagnostic.CannotFindBaseType(kind, lookupResult));
            _baseTypes[kind] = Error;
            return Error;
        }

        symbol = single;
        _baseTypes[kind] = symbol;
        return symbol;
    }

    public void Complete(DiagnosticBag diagnosticBag)
    {
        _ = BaseNamespace;
        foreach (var baseKind in KnownNames.Keys)
            _ = Get(baseKind);
        
        diagnosticBag.AddRange(_diagnosticBag.Diagnostics);
    }
}