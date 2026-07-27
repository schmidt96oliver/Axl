namespace Axl.Compiler.Taxl;

public abstract class TestCase
{
    public abstract TestResult Run();
    public abstract TestResult RunAccept();
    
    public static TestCase ParseTaxl(SourceView source)
    {
        var directives = TaxlParser.Parse(source);
        throw new NotImplementedException();
    }
}