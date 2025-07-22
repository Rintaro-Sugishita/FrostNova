using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.ExcelWork.TemplateFramework
{
    internal class KeyCell
    {
        public string Key { get; set; } = "";

        public string? Format { get; set; }

        public string? Formula { get; set; }

        //public HorizontalAlignment HorizontalAlignment { get; set; }

        //public VerticalAlignment VerticalAlignment { get; set; }

        public int RelativeRow { get; set; }

        public int RelativeColumn { get; set; }



    }
}
