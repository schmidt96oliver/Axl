using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed record Expectation(string Text, SourceLocation Location, SourceLocation PrefixLocation);