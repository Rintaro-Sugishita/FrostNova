using System;
using System.Collections.Generic;
using System.Linq;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{
    public class TimeRangeListHelper
    {
        public static void InsertAndPushForward(IList<ITimeRange> ranges, ITimeRange range, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            // range の開始以降のタスクを時間順に抽出
            var pushList = ranges.Where(x => x.StartTime >= range.StartTime).OrderBy(x => x.StartTime).ToList();

            if (pushList.Count == 0)
            {
                ranges.Add(range);
                return;
            }

            ranges.Add(range);

            DateTime prevEnd = range.EndTime;
            DateTime? prevTaskOriginalEnd = null;

            foreach (var pushRange in pushList)
            {
                bool useNonWorking = !pushRange.IgnoreNonWorkingTimes && nonWorkingTimes != null;

                // ギャップを稼働時間ベースで計算する（Bug #2 修正）
                // 絶対時間ではなく稼働時間のギャップを保持することで、
                // 非稼働時間をまたぐ場合でも間隔が正しく維持される
                TimeSpan gap = TimeSpan.Zero;
                if (prevTaskOriginalEnd.HasValue)
                {
                    gap = useNonWorking
                        ? TimeHelper.CalcWorkingSpan(prevTaskOriginalEnd.Value, pushRange.StartTime, nonWorkingTimes)
                        : pushRange.StartTime - prevTaskOriginalEnd.Value;
                }

                // 移動前の終了時刻を保存（次ループで gap 計算に使う）
                prevTaskOriginalEnd = pushRange.EndTime;

                DateTime newStart;
                if (useNonWorking)
                {
                    // prevEnd が非稼働期間の開始と一致する場合も含めてスキップする（Bug #1 修正）
                    // SkipNonWorkingTimes での零長区間判定では nw.StartTime == candidate を検出できないため
                    // 点包含判定（StartTime <= t < EndTime）に変更した AdvanceToWorkingTime を使う
                    DateTime workingPrevEnd = AdvanceToWorkingTime(prevEnd, nonWorkingTimes!);
                    newStart = gap > TimeSpan.Zero
                        ? TimeHelper.CalcShiftTime(workingPrevEnd, gap, nonWorkingTimes)
                        : workingPrevEnd;
                }
                else
                {
                    newStart = prevEnd + gap;
                }

                TimeRangeHelper.Move(pushRange, newStart - pushRange.StartTime, nonWorkingTimes);

                prevEnd = pushRange.EndTime;
            }
        }

        /// <summary>
        /// 指定日時が非稼働時間内（StartTime &lt;= t &lt; EndTime）に含まれる場合、
        /// その非稼働時間の終了直後まで進める。連続する非稼働時間も連鎖してスキップする。
        /// </summary>
        private static DateTime AdvanceToWorkingTime(DateTime start, IEnumerable<ITimeRange> nonWorkingTimes)
        {
            DateTime candidate = start;
            bool moved;
            do
            {
                moved = false;
                var overlap = nonWorkingTimes.FirstOrDefault(nw => nw.StartTime <= candidate && candidate < nw.EndTime);
                if (overlap != null)
                {
                    candidate = overlap.EndTime;
                    moved = true;
                }
            } while (moved);
            return candidate;
        }
    }
}
