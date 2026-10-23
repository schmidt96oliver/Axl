using System.Collections.Immutable;
using Axl.Compiler.Text;

namespace Axl.Compiler.Diagnostics;

public static class DiagnosticPrinter
{
    extension(Diagnostic diagnostic)
    {
        public string GetDisplayString(string? workingDirectory = null)
            => diagnostic.GetDisplayString(workingDirectory, l => l);
        
        public string GetDisplayString(string? workingDirectory,
            Func<SourceLocation, SourceLocation> locationMap)
        {
            var indent = "   ";
            var locationsString = diagnostic.GetLocationsString(workingDirectory, locationMap);

            var message
                = string.Join('\n', diagnostic.Message.Split('\n').Select(s => $"{indent}{s}"));
            var hint = diagnostic.Hint is not null
                ? $"\n{indent}Hint: " +
                  string.Join('\n', diagnostic.Hint.Split('\n').Select(s => $"{indent}{s}"))
                : "";
            
            return $"{diagnostic.DefaultSeverity} in {locationsString}: {
                diagnostic.Id}:\n{message}{hint}";
        }
        
        public string GetLocationsString(string? workingDirectory,
            Func<SourceLocation, SourceLocation> map)
        {
            if (diagnostic.Locations.Length == 0)
                return "<global>";

            var locations = diagnostic.Locations
                .Select(map)
                .OrderBy(l => l.SourceText.FileName)
                .ThenBy(l => l.Start)
                .ToImmutableArray();
            if (locations.All(l => l.SourceText == locations[0].SourceText))
            {
                // They are all in the same text.
                var pathText = GetDisplayPath(locations[0].SourceText);

                var locationsText = string.Join(", ", locations
                    .Select(l => l.StartLine == l.EndLine
                        ? $"l.{l.StartLine + 1}"
                        : $"ll.{l.StartLine + 1}-{l.EndLine + 1}"));
                return $"{pathText}@{locationsText}";
            }

            return string.Join("; ", locations
                .Select(l => l.StartLine == l.EndLine
                    ? $"{GetDisplayPath(l.SourceText)}@l.{l.StartLine + 1}"
                    : $"{GetDisplayPath(l.SourceText)}@ll.{l.StartLine + 1}-{l.EndLine + 1}"));

            string GetDisplayPath(SourceText text)
                => text.FileName is null
                    ? "<virtual>"
                    : workingDirectory is null
                        ? text.FileName
                        : Path.GetRelativePath(workingDirectory, text.FileName);
        }
    }
}