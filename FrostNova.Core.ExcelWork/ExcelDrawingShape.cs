using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.ExcelWork
{
    public class ExcelDrawingShape : ExcelDrawing
    {
        internal ExcelDrawingShape(OfficeOpenXml.Drawing.ExcelDrawing shape) : base(shape)
        {
            base._drawing = (OfficeOpenXml.Drawing.ExcelShape)shape;
        }

        protected OfficeOpenXml.Drawing.ExcelShape _shape => (OfficeOpenXml.Drawing.ExcelShape)base._drawing;
        public string Text
        {
            get { return _shape.Text; }
            set { _shape.Text = value; }
        }

        public double Rotation
        {
            get
            {
                return _shape.Rotation;
            }
            set
            {
                _shape.Rotation = value;
            }
        }
        public void SetBackground(System.Drawing.Color color)
        {
            _shape.Fill.Style = OfficeOpenXml.Drawing.eFillStyle.SolidFill;
            _shape.Fill.Color = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        public void SetBackgroundGradient(IEnumerable<(double position, System.Drawing.Color color)> colors)
        {
            _shape.Fill.Style = OfficeOpenXml.Drawing.eFillStyle.GradientFill;
            foreach (var color in colors)
            {
                _shape.Fill.GradientFill.Colors.AddRgb(color.position, System.Drawing.Color.FromArgb(color.color.A, color.color.R, color.color.G, color.color.B));
            }
        }

        public void SetBorder(System.Drawing.Color color)
        {
            _shape.Border.Fill.Style = OfficeOpenXml.Drawing.eFillStyle.SolidFill;
            _shape.Border.Fill.Color = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public void SetBorderWidth(double width)
        {
            _shape.Border.Width = width;
        }

        public void SetBorderGradient(IEnumerable<(double position, System.Drawing.Color color)> colors)
        {
            _shape.Border.Fill.Style = OfficeOpenXml.Drawing.eFillStyle.GradientFill;
            foreach (var color in colors)
            {
                _shape.Border.Fill.GradientFill.Colors.AddRgb(color.position, System.Drawing.Color.FromArgb(color.color.A, color.color.R, color.color.G, color.color.B));
            }
        }

    }
}
