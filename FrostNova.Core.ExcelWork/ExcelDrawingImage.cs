using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.ExcelWork
{
    public class ExcelDrawingImage : ExcelDrawing
    {
        internal ExcelDrawingImage(OfficeOpenXml.Drawing.ExcelDrawing shape) : base(shape)
        {
        }

        protected OfficeOpenXml.Drawing.ExcelPicture _picture
        {
            get
            {
                return (OfficeOpenXml.Drawing.ExcelPicture)_drawing;
            }
        }
    }
}
