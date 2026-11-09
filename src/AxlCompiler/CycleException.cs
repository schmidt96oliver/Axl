namespace AxlCompiler;

public sealed class CycleException() : Exception(message: "A cycle occured during symbol binding.")
{
    
}