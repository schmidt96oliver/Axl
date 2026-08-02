using System.Text;

namespace Axl.Tests;

public static class StringExtensions
{
    extension(ReadOnlySpan<char> str)
    {
        public string ToLiteralString()
        {
            var sb = new StringBuilder(str.Length);
            foreach (var c in str)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append(@"\\");  break;
                    case '\0': sb.Append(@"\0");  break;
                    case '\a': sb.Append(@"\a");  break;
                    case '\b': sb.Append(@"\b");  break;
                    case '\f': sb.Append(@"\f");  break;
                    case '\n': sb.Append(@"\n");  break;
                    case '\r': sb.Append(@"\r");  break;
                    case '\t': sb.Append(@"\t");  break;
                    case '\v': sb.Append(@"\v");  break;
                    default:
                        if (c is < ' ' or > '~')
                            sb.Append("\\u").Append(((int)c).ToString("X4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}