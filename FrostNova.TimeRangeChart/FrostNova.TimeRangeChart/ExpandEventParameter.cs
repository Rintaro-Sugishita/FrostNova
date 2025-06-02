using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart
{
    public class ExpandEventParameter(object? dataContext, DateTime newDateTime)
    {
        public object? DataContext => dataContext;

        public DateTime NewDateTime => newDateTime;

    }
}
