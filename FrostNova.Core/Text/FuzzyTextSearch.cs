using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Text
{
    /// <summary>
    /// Culture-aware fuzzy text search utilities. Comparisons are case-insensitive and ignore kana type
    /// and full-width / half-width differences, matching Japanese end-user search expectations.
    /// </summary>
    public static class FuzzyTextSearch
    {
        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="thisText"/> contains <paramref name="target"/>,
        /// ignoring case, kana type, and full-width / half-width distinctions.
        /// </summary>
        public static bool Contains(string thisText, string target)
        {
            if (thisText == null || target == null) { return false; }
            var info = CultureInfo.CurrentCulture.CompareInfo;
            return -1 != info.IndexOf(thisText, target, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);
        }

    }
}
