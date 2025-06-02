using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Text
{
    public static class FuzzyTextSearch
    {
        public static bool Contains(string thisText, string target)
        {
            if (thisText == null || target == null) { return false; }
            var info = CultureInfo.CurrentCulture.CompareInfo;
            return -1 != info.IndexOf(thisText, target, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);
        }

    }
}
