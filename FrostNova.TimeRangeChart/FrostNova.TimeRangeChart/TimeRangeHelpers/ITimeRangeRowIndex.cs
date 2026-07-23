using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers;

public interface ITimeRangeRowIndex : ITimeRange 
{
    int RowIndex { get; set; }
    double StackBaseTop { get; }

    bool IsSelected {  get; set; }

    /// <summary>
    /// このタスクをドラッグで移動できるか。false の場合、単体ドラッグ・複数選択の一括移動のいずれでも移動しない。
    /// </summary>
    bool CanDrag { get; }

    /// <summary>
    /// このタスクの左右端ドラッグで期間（開始／終了時刻）を伸縮できるか。
    /// false の場合、単体リサイズ・複数選択の一括リサイズのいずれでも伸縮しない。
    /// </summary>
    bool CanExpandSpan { get; }

    bool CanDragTasksBetweenRows { get;}

    bool CanStack { get; }
}
