namespace Axl.Tests;

public static class CaretStringExtensions
{
    extension(string str)
    {
        /// <summary>
        /// Removes a single `|` from this string and returns its
        /// position into <paramref name="caret"/>. If no `|` was
        /// inside this string, <paramref name="caret"/> will be <c>-1</c>
        /// and this string will be returned. Only single `|` are
        /// allowed. `||` or longer chains are disregarded.
        /// </summary>
        public string RemoveCaret(out int caret)
        {
            // `|` will denote a caret, only if it is not part of a group
            // of vertical bars like `||`. This will denote other things.

            caret = str.IndexOf('|');
            while (caret != -1)
            {
                var prev = caret > 0 ? str[caret - 1] : '\0';
                var next = caret < str.Length - 1 ? str[caret + 1] : '\0';
                if (prev != '|' && next != '|')
                    break;

                if (caret < str.Length - 1)
                    caret = str.IndexOf('|', caret + 1);
                else
                {
                    caret = -1;
                    break;
                }
            }

            return caret >= 0 ? str.Remove(caret, 1)  : str;
        }
    }
}