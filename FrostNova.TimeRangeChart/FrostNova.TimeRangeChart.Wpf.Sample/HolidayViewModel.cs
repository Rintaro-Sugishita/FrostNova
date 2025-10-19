using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.Core.Common;
using FrostNova.Core.Wpf.ViewModelBases;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.Wpf.Sample
{
    public partial class HolidayViewModel : ViewModelBase
    {

        [ObservableProperty]
        private NotifyCollection<GridSampleData> _items = new NotifyCollection<GridSampleData>();

        [ObservableProperty]
        private DateTime _startTime;

        [ObservableProperty]
        private DateTime _endTime;

        [ObservableProperty]
        private NotifyCollection<ITimeRange> _nonWorkingTimes = new NotifyCollection<ITimeRange>();
        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();

            Items.Clear();

            //今の日付から2週間分作る
            //水曜を左端にする
            var d = DateTime.Now.Date;
            var dayOfWeek = d.DayOfWeek;
            var div = dayOfWeek - DayOfWeek.Wednesday;
            StartTime = d.AddDays(-div);
            EndTime = StartTime.AddDays(14);

            //土曜と日曜を非稼働にする
            NonWorkingTimes.Clear();
            NonWorkingTimes.Add(new FixedTimeRange(StartTime.AddDays(3), StartTime.AddDays(4)));
            NonWorkingTimes.Add(new FixedTimeRange(StartTime.AddDays(4), StartTime.AddDays(5)));
            NonWorkingTimes.Add(new FixedTimeRange(StartTime.AddDays(10), StartTime.AddDays(11)));
            NonWorkingTimes.Add(new FixedTimeRange(StartTime.AddDays(11), StartTime.AddDays(12)));


            var a = new GridSampleData();
            a.Name = "holiday";


            //日曜開始になるようにする
            var task1 = new SampleModel(StartTime.AddDays(4), TimeSpan.FromDays(1), NonWorkingTimes);

            a.Tasks.Add(task1);

            Items.Add(a);

        }



    }
}
