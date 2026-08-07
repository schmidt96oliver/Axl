namespace Axl.Compiler.Diagnostics;

/// <summary>
/// An additional <see cref="SourceLocation"/> belonging to a
/// <see cref="Diagnostic"/>, together with the label to render at it.
/// It explains the diagnostic, it is not broken itself.
/// </summary>
public readonly record struct LabeledSourceLocation(SourceLocation Location, string Label);