using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed record TypeAnnotation(
    SourceLocation FullLocation,
    SourceLocation PrefixLocation,
    SourceLocation ReferencedLocation,
    string TypeName)
    : Annotation(FullLocation, PrefixLocation);