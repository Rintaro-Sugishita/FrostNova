using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    /// <summary>
    /// Specifies the supported input data formats.
    /// </summary>
    /// <remarks>Use this enumeration to indicate the expected format for user input or data parsing
    /// operations. The values represent common data types such as integer, decimal, and date/time.</remarks>
    public enum InputFormatType
    {
        None,
        Integer,
        Decimal,
        DateTime
    }
}
