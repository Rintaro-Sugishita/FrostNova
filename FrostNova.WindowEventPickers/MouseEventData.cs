using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FrostNova.WindowEventPickers
{
    public class MouseEventData
    {
        public int X { get; }
        public int Y { get; }
        public int Delta { get; }

        public MouseButton Button { get; }

        public MouseEventData(MouseButton button, int x, int y, int delta = 0)
        {
            X = x;
            Y = y;
            Delta = delta;
            Button = button;
        }

    }
}
