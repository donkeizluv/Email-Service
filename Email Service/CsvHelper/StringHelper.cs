namespace CsvHelper
{
    /// <summary>
    ///     Common string tasks.
    /// </summary>
    internal static class StringHelper
    {
        /// <summary>
        ///     Tests is a string is null or whitespace.
        /// </summary>
        /// <param name="s">The string to test.</param>
        /// <returns>True if the string is null or whitespace, otherwise false.</returns>
        public static bool IsNullOrWhiteSpace(string s)
        {
            if (s == null)
                return true;
            for (int i = 0; i < s.Length; i++)
                if (!char.IsWhiteSpace(s[i]))
                    return false;
            return true;
        }
    }
}