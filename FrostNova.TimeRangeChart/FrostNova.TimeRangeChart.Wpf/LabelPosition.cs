using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.Wpf;

/// <summary>
/// Specifies the possible positions for displaying a label relative to its Symbol element.
/// </summary>
/// <remarks>Use this enumeration to control the placement of labels in user interface layouts. The default
/// position is Right. The choice of position may affect the readability and alignment of form elements.</remarks>
public enum LabelPosition
{
    Right,  // default
    Left,
    Top,
    Bottom
}
