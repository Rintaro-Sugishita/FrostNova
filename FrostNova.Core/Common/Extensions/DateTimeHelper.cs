using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Common.Extensions
{
    public static class DateTimeHelper
    {
        public static int GetMonthDifference(DateTime from, DateTime to)
        {
            // 日付を1日に揃える
            from = new DateTime(from.Year, from.Month, 1);
            to = new DateTime(to.Year, to.Month, 1);

            // 年と月の差を計算
            int diff = (to.Year - from.Year) * 12 + (to.Month - from.Month);

            return diff;
        }
    }
}
