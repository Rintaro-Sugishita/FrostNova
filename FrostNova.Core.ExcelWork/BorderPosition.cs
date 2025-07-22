using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.ExcelWork
{
    [Flags]
    public enum BorderPosition
    {
        None = 0,
        Left = 1,
        Top = 2,
        Right = 4,
        Bottom = 8,
        Horizontal = Top | Right,
        Vertical = Left | Top,
        All = Left | Top | Right | Bottom,
    }
}
