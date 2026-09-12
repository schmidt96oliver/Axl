using System.Collections.Immutable;
using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

/// <param name="Text">The <see cref="SourceText"/> that this fragment defines.</param>
public sealed record Fragment(SourceText Text, string Name, bool IsOutput, ImmutableArray<Annotation> Annotations);