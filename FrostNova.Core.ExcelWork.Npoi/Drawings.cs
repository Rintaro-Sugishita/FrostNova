using System;
using System.Drawing;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>Base class for worksheet drawings (images, shapes, charts). Positions use pixel/cell units.</summary>
    public abstract class ExcelDrawing
    {
        protected readonly XSSFClientAnchor Anchor;
        private int _width = 100;
        private int _height = 100;

        private protected ExcelDrawing(XSSFClientAnchor anchor)
        {
            Anchor = anchor;
        }

        public string Name { get; set; } = "";

        public int RowIndex
        {
            get => Anchor.Row1;
            set { int delta = value - Anchor.Row1; Anchor.Row1 = value; Anchor.Row2 += delta; }
        }

        public int ColumnIndex
        {
            get => Anchor.Col1;
            set { int delta = value - Anchor.Col1; Anchor.Col1 = value; Anchor.Col2 += delta; }
        }

        public int Left
        {
            get => Anchor.Dx1 / 9525;
            set => Anchor.Dx1 = value * 9525;
        }

        public int Top
        {
            get => Anchor.Dy1 / 9525;
            set => Anchor.Dy1 = value * 9525;
        }

        public void SetSize(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public (int width, int height) GetSize() => (_width, _height);
    }

    /// <summary>An embedded picture.</summary>
    public sealed class ExcelDrawingImage : ExcelDrawing
    {
        private readonly IPicture _picture;

        internal ExcelDrawingImage(IPicture picture, XSSFClientAnchor anchor) : base(anchor)
        {
            _picture = picture;
        }

        /// <summary>Resize the picture to its native pixel dimensions.</summary>
        public void ResizeToOriginal() => _picture.Resize();
    }

    /// <summary>An auto-shape (rectangle, ellipse, arrow, …) with text, fill and border.</summary>
    public class ExcelDrawingShape : ExcelDrawing
    {
        private readonly XSSFSimpleShape _shape;
        private string _text = "";

        internal ExcelDrawingShape(XSSFSimpleShape shape, XSSFClientAnchor anchor) : base(anchor)
        {
            _shape = shape;
        }

        public string Text
        {
            get => _text;
            set { _text = value; _shape.SetText(new XSSFRichTextString(value)); }
        }

        /// <summary>Rotation in degrees. Stored for API parity; NPOI simple shapes do not render rotation.</summary>
        public double Rotation { get; set; }

        public void SetBackground(Color color) => _shape.SetFillColor(color.R, color.G, color.B);

        public void SetBackgroundGradient(System.Collections.Generic.IEnumerable<(double position, Color color)> colors)
            => throw new NotSupportedException("Gradient fill is not supported by the NPOI backend.");

        public void SetBorder(Color color) => _shape.SetLineStyleColor(color.R, color.G, color.B);

        public void SetBorderWidth(double width) => _shape.LineWidth = width;

        public void SetBorderGradient(System.Collections.Generic.IEnumerable<(double position, Color color)> colors)
            => throw new NotSupportedException("Gradient border is not supported by the NPOI backend.");
    }

    internal static class ShapeStyleMapping
    {
        public static ShapeTypes ToShapeType(this ShapeStyle style)
        {
            return style switch
            {
                ShapeStyle.Rectangle => ShapeTypes.Rectangle,
                ShapeStyle.RoundRectangle => ShapeTypes.Rectangle, // NPOI ShapeTypes has no rounded variant; approximated

                ShapeStyle.Ellipse => ShapeTypes.Ellipse,
                ShapeStyle.Triangle => ShapeTypes.Triangle,
                ShapeStyle.RightTriangle => ShapeTypes.RightTriangle,
                ShapeStyle.Diamond => ShapeTypes.Diamond,
                ShapeStyle.Parallelogram => ShapeTypes.Parallelogram,
                ShapeStyle.Trapezoid => ShapeTypes.Trapezoid,
                ShapeStyle.Hexagon => ShapeTypes.Hexagon,
                ShapeStyle.Octagon => ShapeTypes.Octagon,
                ShapeStyle.Plus => ShapeTypes.Plus,
                ShapeStyle.Pentagon => ShapeTypes.Pentagon,
                ShapeStyle.Chevron => ShapeTypes.Chevron,
                ShapeStyle.Star5 => ShapeTypes.Star5,
                ShapeStyle.RightArrow => ShapeTypes.RightArrow,
                ShapeStyle.LeftArrow => ShapeTypes.LeftArrow,
                ShapeStyle.UpArrow => ShapeTypes.UpArrow,
                ShapeStyle.DownArrow => ShapeTypes.DownArrow,
                ShapeStyle.Heart => ShapeTypes.Heart,
                ShapeStyle.Cloud => ShapeTypes.Rectangle,
                ShapeStyle.Can => ShapeTypes.Can,
                ShapeStyle.Cube => ShapeTypes.Cube,
                _ => ShapeTypes.Rectangle,
            };
        }
    }
}
