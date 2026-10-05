namespace Axl.Compiler.Symbols;

public sealed class ErrorTypeSymbol : TypeSymbol
{
    public static ErrorTypeSymbol Instance = new();
    
    private ErrorTypeSymbol()
        : base(name: "???")
    {
        
    }
}