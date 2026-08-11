using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit.Sdk;
using Xunit.v3;

namespace Axl.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CorpusAttribute() 
    : DataAttribute, IBeforeAfterTestAttribute
{
    private static string ThisFilePath([CallerFilePath] string sourceFile = "")
        => Path.GetDirectoryName(sourceFile)!;
    
    public static string Root
        => Path.Combine(ThisFilePath(), "Corpus");
    
    
    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
    {
        var taxlFiles = Directory.EnumerateFiles(Root, "*.taxl", SearchOption.AllDirectories);
        var rows = taxlFiles.Select(path => new TheoryDataRow<string>(path)
        {
            TestDisplayName = GetDisplayName(path),
            Label = ""
        }).ToList();
        return new(rows);

        string GetDisplayName(string path)
        {
            var relativePath = Path.GetRelativePath(Root, path);
            var directory = Path.GetDirectoryName(relativePath) ?? "";
            directory = directory.Replace(Path.DirectorySeparatorChar, '/');
            var fileName = Path.GetFileNameWithoutExtension(relativePath);
            return string.Concat(directory, "/", fileName);
        }
    }

    public override bool SupportsDiscoveryEnumeration()
        => true;

    
    public void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        
    }
    
    public void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        TestContext.Current.TestOutputHelper?.WriteLine($"at {
            test.TestMethodArguments[0]?.ToString() ?? ""}");
    }
}