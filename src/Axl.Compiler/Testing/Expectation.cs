using System.Collections.Immutable;
using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed record Expectation(string Text, SourceLocation Location, ImmutableArray<SourceLocation> PrefixLocations);