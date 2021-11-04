using System;

namespace DatasetRefactor.Extensions
{
    internal static class StringExtensions
    {
        public static string GetSuffix(this string text, string preffix)
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(preffix))
            {
                var pos = preffix.IndexOf(text, StringComparison.Ordinal);
                if (pos == 0)
                {
                    return text.Substring(preffix.Length);
                }
            }

            return string.Empty;
        }
    }
}
