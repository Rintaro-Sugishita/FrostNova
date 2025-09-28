using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.Wpf
{
    public class DragDropRangeEventArgs : EventArgs
    {

        public DateTime? DragStartTime { get; internal set; }
        public DateTime? DropStartTime { get; internal set; }

        public TimeSpan DragActualSpan { get; internal set; }
        public TimeSpan? DropActualSpan { get; internal set; }
    
        public TimeSpan DragWorkingSpan { get; internal set; }
        public TimeSpan? DropWorkingSpan { get; internal set; }
    
        public object? DragData { get; internal set; }
    
    }
}
