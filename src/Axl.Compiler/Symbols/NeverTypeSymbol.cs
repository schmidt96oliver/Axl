namespace Axl.Compiler.Symbols;

public sealed class NeverTypeSymbol : TypeSymbol
{
    public static NeverTypeSymbol Instance = new();
    
    private NeverTypeSymbol()
        : base(name: "Never")
    {
        
    }
}