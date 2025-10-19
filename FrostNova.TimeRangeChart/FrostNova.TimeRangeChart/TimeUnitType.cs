using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart
{
    /// <summary>
    /// Unit type of time axis in chart.
    /// </summary>
    public enum TimeUnitType
    {
        /// <summary>
        /// normal type. The size of time axis depend on 'ScaleIncrement' property.
        /// </summary>
        Normal,

        /// <summary>
        /// month type. The size of time axis depend on 'MonthSpan' property.
        /// </summary>
        Month
    }
}
