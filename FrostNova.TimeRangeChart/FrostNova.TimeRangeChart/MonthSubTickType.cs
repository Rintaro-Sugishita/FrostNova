using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart
{
    public enum MonthSubTickType
    {
        None,
        Weekly,        // 7日ごと（暦）
        HalfMonth,     // 1日 / 16日
        MonthDivided2, // 見た目2分割
        MonthDivided4  // 見た目4分割
    }
}
