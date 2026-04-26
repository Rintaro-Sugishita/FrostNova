using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.TimeRangeChart.Wpf
{
    public class DropOnRowEventArgs : RoutedEventArgs
    {
        public bool Cancel { get; set; } = false;
    
        public DateTime DragStartPoint { get; internal set; }
        public DateTime DropPoint { get; internal set; }

        public object? TaskObject { get; }

        //public IList? SourceList { get; internal set; }
        //public IList? DestList { get; internal set; }
        public int OldRowIndex { get; internal set; }

        public int NewRowIndex { get; internal set; }

        internal DropOnRowEventArgs(RoutedEvent routedEvent, DateTime dropPoint, object taskObject) : base(routedEvent)
        {
            DropPoint = dropPoint;
            TaskObject = taskObject;
        }
    
        internal DropOnRowEventArgs(RoutedEvent routedEvent, DropOnRowEventArgs baseArgs) : base(routedEvent)
        {
            Cancel = baseArgs.Cancel;

            DragStartPoint = baseArgs.DragStartPoint;
            DropPoint = baseArgs.DropPoint;

            TaskObject = baseArgs.TaskObject;

            OldRowIndex = baseArgs.OldRowIndex;
            NewRowIndex = baseArgs.NewRowIndex;

        }
    
    
    
    }


}
