namespace Axl.Compiler.Diagnostics;

/// <summary>
/// An additional <see cref="SourceLocation"/> belonging to a
/// <see cref="Diagnostic"/>, together with the label to render at it.
/// </summary>
/// <param name="IsPrimary">
/// Whether the code at this location is at fault itself, rather than merely
/// being context for the diagnostic's own <see cref="Diagnostic.Location"/>.
/// A secondary location explains a diagnostic, a primary one is broken
/// in the same way the main location is. Editors underline every primary location, 
/// so only set this when the squiggle would point at genuinely wrong code.
/// </param>
public readonly record struct LabeledSourceLocation(
    SourceLocation Location,
    string Label,
    bool IsPrimary = false);