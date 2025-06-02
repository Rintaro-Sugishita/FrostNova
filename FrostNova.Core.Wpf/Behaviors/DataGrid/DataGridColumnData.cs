using Microsoft.Xaml.Behaviors.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.Behaviors
{
    internal class DataGridColumnData
    {
        public string Key { get; set; } = string.Empty;
        public int DisplayIndex { get; set; }

        public double Width { get; set; }
    }
}
