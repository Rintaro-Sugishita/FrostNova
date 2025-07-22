using OfficeOpenXml.Utils.TypeConversion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.ExcelWork
{
    public class ExcelDrawing
    {


        protected OfficeOpenXml.Drawing.ExcelDrawing _drawing;

        internal ExcelDrawing(OfficeOpenXml.Drawing.ExcelDrawing shape)
        {
            _drawing = shape;
        }

        public string Name
        {
            get { return _drawing.Name; }
            set { _drawing.Name = value; }
        }


        public int RowIndex
        {
            get
            {
                return _drawing.From.Row;
            }
            set
            {
                _drawing.From.Row = value;
            }
        }
        public int ColumnIndex
        {
            get
            {
                return _drawing.From.Column;
            }
            set
            {
                _drawing.From.Column = value;
            }
        }




        public void SetSize(int width, int height)
        {
            _drawing.SetSize(width, height);
        }

        public (int width, int height) GetSize()
        {
            return ((int)(_drawing.Size.Width / 9525), (int)(_drawing.Size.Height / 9525));
        }

        public int Left
        {
            get
            {
                return _drawing.Position.X / 9525;
            }
            set
            {
                _drawing.Position.X = value * 9525;
            }
        }

        public int Top
        {
            get
            {
                return _drawing.Position.Y / 9525;
            }
            set
            {
                _drawing.Position.Y = value * 9525;
            }
        }







    }
}
