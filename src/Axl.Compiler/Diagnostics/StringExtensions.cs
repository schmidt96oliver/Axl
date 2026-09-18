namespace Axl.Compiler.Diagnostics;

public static class StringExtensions
{
    extension(string str)
    {
        public string FirstLetterToUpper() =>
            str is "" ? "" : string.Concat(str[0].ToString().ToUpper(), str.AsSpan(1, str.Length - 1));

    }
}