namespace Axl.Compiler.Diagnostics;

public static class StringExtensions
{
    extension(string str)
    {
        /// <summary>
        /// Returns a string, where the first letter is converted to upper.
        /// E.g. "letter" -> "Letter".
        /// </summary>
        public string Capitalize() =>
            str is "" ? "" : string.Concat(str[0].ToString().ToUpper(), str.AsSpan(1, str.Length - 1));

    }
}