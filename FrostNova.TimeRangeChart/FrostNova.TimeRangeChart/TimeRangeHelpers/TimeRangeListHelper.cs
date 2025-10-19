using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{
    public class TimeRangeListHelper
    {
        public static void InsertAndPushForward(IList<ITimeRange> ranges, ITimeRange range, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            //rangeよりも未来のものを抽出
            //rangesは時間順に並んでるとは限らない
            var pushList = ranges.Where(x => x.StartTime >= range.StartTime).OrderBy(x=> x.StartTime).ToList();

            if (pushList == null || pushList.Count == 0)
            {
                //ないなら最後に追加して終わり
                ranges.Add(range);
                return;
            }

            // 4. rangeをリストに追加
            ranges.Add(range);

            // 5. 未来タスクを順に、前のタスクの終了時刻以降にずらす
            //    非稼働時間も考慮
            //    まず、rangeの終了時刻を基準にする
            DateTime prevEnd = range.EndTime;
            DateTime? prevTaskMovingEnd =null;

            foreach (var pushRange in pushList)
            {
                // 元のギャップ（前タスク移動前の終了～現ループタスクの移動前の開始）
                TimeSpan gap = prevTaskMovingEnd.HasValue ? pushRange.StartTime - prevTaskMovingEnd.Value : TimeSpan.Zero;

                // 次のループ用(移動前の値)
                prevTaskMovingEnd = pushRange.EndTime;

                // 新しい開始時刻 = 直前タスクの終了時刻 + 元のギャップ
                DateTime newStart = prevEnd + gap;
               

                // 非稼働時間を考慮して開始時刻を調整
                if (!pushRange.IgnoreNonWorkingTimes && nonWorkingTimes != null)
                {
                    newStart = SkipNonWorkingTimes(newStart, TimeSpan.Zero, nonWorkingTimes);
                }

                var span = newStart - pushRange.StartTime;

                // 新しい終了時刻を計算（元のWorkingSpanを維持）
                TimeSpan originalSpan = pushRange.WorkingSpan ?? (pushRange.EndTime - pushRange.StartTime);
                DateTime newEnd = pushRange.IgnoreNonWorkingTimes
                    ? newStart + originalSpan
                    : TimeHelper.CalcEndTime(newStart, originalSpan, nonWorkingTimes);

                TimeRangeHelper.Move(pushRange, span, nonWorkingTimes);


                // 次のループ用
                prevEnd = pushRange.EndTime;
            }

        }

        // 非稼働時間をスキップして開始時刻を返す（シンプルな例）
        private static DateTime SkipNonWorkingTimes(DateTime start, TimeSpan span, IEnumerable<ITimeRange> nonWorkingTimes)
        {
            DateTime candidate = start;
            while (true)
            {
                // 非稼働時間と重なるか？
                var overlap = nonWorkingTimes.FirstOrDefault(nw => nw.IsOverlap(new TimeRange(candidate, candidate + span)));
                if (overlap == null)
                    return candidate;

                // 非稼働時間の終了後にずらす
                candidate = overlap.EndTime;
            }
        }



    }
}
