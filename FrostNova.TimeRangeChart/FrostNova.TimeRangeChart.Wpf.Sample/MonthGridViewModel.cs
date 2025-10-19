using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using Microsoft.Xaml.Behaviors.Core;
using static FrostNova.TimeRangeChart.Wpf.Sample.MainWindowViewModel;

namespace FrostNova.TimeRangeChart.Wpf.Sample
{
    public partial class MonthGridViewModel : FrostNova.Core.Wpf.ViewModelBases.ViewModelBase
    {

        [ObservableProperty]
        ObservableCollection<GridSampleData> _modelList = new ObservableCollection<GridSampleData>();


        [ObservableProperty]
        private DateTime _chartStart = DateTime.Now.Date.AddMonths(-3);

        [ObservableProperty]
        private DateTime _chartEnd = DateTime.Now.Date.AddMonths(2);

        [ObservableProperty]
        private TimeSpan _minSpan = TimeSpan.FromDays(1);

        [ObservableProperty]
        private int _aaa = 5;

        [ObservableProperty]
        private ObservableCollection<ITimeRange> _nonWorkingTimes = [];

        protected override void InitializeOnDesign()
        {
            base.InitializeOnDesign();
            SetData(5, 2, 3);
        }
        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();
            SetData(5, 10, 2);
        }

        private void SetData(int months, int r, int c)
        {
            var nowDay = DateTime.Now.Date;
            ChartStart = new DateTime(nowDay.Year, nowDay.Month, 1).AddMonths(-2);
            ChartEnd = ChartStart.AddMonths(months);

            //土日を非稼働時間にする
            for (DateTime i = ChartStart; i <= ChartEnd; i = i.AddDays(1))
            {
                if (i.DayOfWeek == DayOfWeek.Saturday || i.DayOfWeek == DayOfWeek.Sunday)
                {
                    NonWorkingTimes.Add(new FixedTimeRange(i, TimeSpan.FromDays(1)));
                }
            }


            Random random = new Random();

            for (int i = 0; i < r; i++)
            {
                var a = new GridSampleData();
                a.Name = "data" + i;

                //ganttを追加
                //1行あたり100こくらい
                for (int j = 0; j < c; j++)
                {
                    var d1 = DateTimeRandomGenerator.GetRandomDateTime(ChartStart, ChartEnd);
                    var span = new TimeSpan(random.Next(60) + 20, 0, 0, 0);

                    var d = new SampleModel(d1.Date, span);
                    d.Color = Color.FromArgb(130, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
                    d.WorkingSpan = span;

                    a.Tasks.Add(d);

                }

                ModelList.Add(a);


            }


        }
    }
}
