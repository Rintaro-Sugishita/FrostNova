using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace FrostNova.TimeRangeChart.Wpf
{
    public class DragDropRangeEventArgs : RoutedEventArgs
    {

        public DateTime? DragStartTime { get; internal set; }
        public DateTime? DropStartTime { get; internal set; }

        public TimeSpan DragActualSpan { get; internal set; }
        public TimeSpan? DropActualSpan { get; internal set; }
    
        public TimeSpan DragWorkingSpan { get; internal set; }
        public TimeSpan? DropWorkingSpan { get; internal set; }

        public DateTime? DragEndTime { get; internal set; }


    
        public object? DragData { get; internal set; }

        public bool Cancel { get; set; } = false;

        internal DragDropRangeEventArgs(RoutedEvent routedEvent) : base(routedEvent)
        {
        }

        internal DragDropRangeEventArgs(RoutedEvent routedEvent, DragDropRangeEventArgs baseArgs) : base(routedEvent)
        {
            DragStartTime = baseArgs.DragStartTime;
            DropStartTime = baseArgs.DropStartTime;

            DragActualSpan = baseArgs.DragActualSpan;
            DropActualSpan = baseArgs.DropActualSpan;

            DragWorkingSpan = baseArgs.DragWorkingSpan;
            DropWorkingSpan = baseArgs.DropWorkingSpan;

            DragEndTime = baseArgs.DragEndTime;

            DragData = baseArgs.DragData;

            Cancel = baseArgs.Cancel;
        }



    }
}
