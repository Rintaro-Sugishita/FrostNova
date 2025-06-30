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
    public partial class ChartViewModel : FrostNova.Core.Wpf.ViewModelBases.ViewModelBase
    {

        [ObservableProperty]
        GridSampleData _model = new GridSampleData();


        [ObservableProperty]
        private DateTime _chartStart = DateTime.Now.Date.AddMonths(-5);

        [ObservableProperty]
        private DateTime _chartEnd = DateTime.Now.Date.AddMonths(10);

        [ObservableProperty]
        private TimeSpan _minSpan = TimeSpan.FromDays(1);

        [ObservableProperty]
        private int _aaa = 5;

        [ObservableProperty]
        private ObservableCollection<ITimeRange> _nonWorkingTimes = [];

        protected override void InitializeOnDesign()
        {
            base.InitializeOnDesign();
            SetData(5, 3);
        }
        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();
            SetData(20, 2);
        }

        private void SetData(int days, int c)
        {

            var nowDay = DateTime.Now.Date;
            ChartStart = new DateTime(nowDay.Year, nowDay.Month, 1).AddDays(-2);
            ChartEnd = new DateTime(nowDay.Year, nowDay.Month, 1).AddDays(days);

            //土日を非稼働時間にする
            for (DateTime i = ChartStart; i <= ChartEnd; i = i.AddDays(1))
            {
                if (i.DayOfWeek == DayOfWeek.Saturday || i.DayOfWeek == DayOfWeek.Sunday)
                {
                    NonWorkingTimes.Add(new FixedTimeRange(i, TimeSpan.FromDays(1)));
                }
            }


            Random random = new Random();

            var a = new GridSampleData();
            a.Name = "data";

            //rangeを追加
            //1行あたり100こくらい
            for (int j = 0; j < c; j++)
            {
                var d1 = DateTimeRandomGenerator.GetRandomDateTime(ChartStart, ChartEnd);
                var span = new TimeSpan(random.Next(60) + 20, 0, 0, 0);

                var d = new SampleModel(d1.Date, TimeSpan.Zero);
                d.Color = Color.FromArgb(130, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
                d.WorkingSpan = TimeSpan.Zero;
                d.IgnoreNonWorkingTimes = true;

                a.Tasks.Add(d);

            }

            Model = a;

        }


    }
}

