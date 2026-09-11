using System.Collections.Immutable;
using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed record Fragment(SourceFileView Source, string Name, bool IsOutput, ImmutableArray<Annotation> Annotations);