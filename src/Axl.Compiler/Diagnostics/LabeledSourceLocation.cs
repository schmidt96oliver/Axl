namespace Axl.Compiler.Diagnostics;

public readonly record struct LabeledSourceLocation(SourceLocation Location, string Label);