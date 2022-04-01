using System;
using System.Linq;

namespace DatasetRefactor.Extensions
{
    internal static class StringExtensions
    {
        public static bool HasSuffix(this string text, string preffix, out string suffix)
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(preffix))
            {
                if (text.StartsWith(preffix, StringComparison.Ordinal))
                {
                    suffix = text.Substring(preffix.Length);
                    return true;
                }
            }

            suffix = null;
            return false;
        }
    }
}
