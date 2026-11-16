using System.Collections.Frozen;
using AxlCompiler.Symbols;

namespace AxlCompiler.Binding;

public union ConstValue(int, long, float, double, bool, string, ConstStructValue);

public class ConstStructValue(TypeSymbol @struct, FrozenDictionary<FieldSymbol, ConstValue> fieldValues)
{
    /// <summary>
    /// <see cref="TypeSymbol"/>, because it might be specialized.
    /// </summary>
    public TypeSymbol Struct { get; } = @struct;
    public FrozenDictionary<FieldSymbol, ConstValue> FieldValues { get; } = fieldValues;
}