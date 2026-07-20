using System;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>Border line style. Mirrors the EPPlus-based FrostNova.Core.ExcelWork values.</summary>
    public enum BorderStyle
    {
        /// <summary>No border style</summary>
        None,
        /// <summary>Hairline</summary>
        Hair,
        /// <summary>Dotted</summary>
        Dotted,
        /// <summary>Dash Dot</summary>
        DashDot,
        /// <summary>Thin single line</summary>
        Thin,
        /// <summary>Dash Dot Dot</summary>
        DashDotDot,
        /// <summary>Dashed</summary>
        Dashed,
        /// <summary>Dash Dot Dot, medium thickness</summary>
        MediumDashDotDot,
        /// <summary>Dashed, medium thickness</summary>
        MediumDashed,
        /// <summary>Dash Dot, medium thickness</summary>
        MediumDashDot,
        /// <summary>Single line, Thick</summary>
        Thick,
        /// <summary>Single line, medium thickness</summary>
        Medium,
        /// <summary>Double line</summary>
        Double
    }

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

    public enum HorizontalAlignment
    {
        General,
        Left,
        Center,
        CenterContinuous,
        Right,
        Fill,
        Distributed,
        Justify
    }

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom,
        Distributed,
        Justify
    }

    public enum InsertShiftType
    {
        Right,
        Down,
        EntireRow,
        EntireColumn
    }

    public enum DeleteShiftType
    {
        Left,
        Top,
        EntireRow,
        EntireColumn
    }
}
