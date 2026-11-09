using System.Collections.Immutable;

namespace AxlCompiler.Diagnostics;

public static class HasErrorExtension
{
    extension(IEnumerable<Diagnostic> diagnostics)
    {
        public bool HasError => diagnostics.OfType<Diagnostic.Error>().Any();
    }
}