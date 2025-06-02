using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart
{
    public class MoveEventParameter(object? dataContext, TimeSpan delta)
    {
        public object? DataContext => dataContext;

        public TimeSpan Delta => delta;
    }
}
