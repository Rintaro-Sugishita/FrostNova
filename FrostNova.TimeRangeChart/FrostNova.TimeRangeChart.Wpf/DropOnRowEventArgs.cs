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
    
        public DateTime DragStartPoint { get; }
        public DateTime DropPoint { get; }

        public object? TaskObject { get; }

        public IList? SourceList { get; internal set; }
        public IList? DestList { get; internal set; }

        internal DropOnRowEventArgs(RoutedEvent routedEvent, DateTime dropPoint, object taskObject) : base(routedEvent)
        {
            DropPoint = dropPoint;
            TaskObject = taskObject;
        }
    
    
    
    }


}
