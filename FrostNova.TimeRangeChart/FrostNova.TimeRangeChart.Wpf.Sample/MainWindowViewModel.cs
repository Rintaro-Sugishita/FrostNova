using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using FrostNova. TimeRangeChart.TimeRangeHelpers;
using Microsoft.Xaml.Behaviors.Core;

namespace FrostNova.TimeRangeChart.Wpf.Sample
{
    public partial class MainWindowViewModel : FrostNova.Core.Wpf.ViewModelBases.ViewModelBase
    {

        [ObservableProperty]
        ObservableCollection<GridSampleData> _modelList = new ObservableCollection<GridSampleData>();


        [ObservableProperty]
        private DateTime _chartStart = DateTime.Now.Date.AddDays(-5);

        [ObservableProperty]
        private DateTime _chartEnd = DateTime.Now.Date.AddDays(10);

        [ObservableProperty]
        private TimeSpan _minSpan = TimeSpan.FromDays(1);

        [ObservableProperty]
        private int _aaa = 5;

        [ObservableProperty]
        private ObservableCollection<ITimeRange> _nonWorkingTimes = [];


        public static class DateTimeRandomGenerator
        {
            private static readonly Random random = new Random();

            public static DateTime GetRandomDateTime(DateTime chartStart, DateTime chartEnd)
            {
                if (chartStart > chartEnd)
                    throw new ArgumentException("chartStart must be earlier than chartEnd.");

                // 総ミリ秒を取得（TimeSpanで範囲を表現）
                var range = chartEnd - chartStart;

                // 範囲をミリ秒単位で取得し、それに基づいてランダムなオフセットを生成
                var randomTicks = (long)(random.NextDouble() * range.Ticks);

                return chartStart.AddTicks(randomTicks);
            }
        }

        protected override void InitializeOnDesign()
        {
            base.InitializeOnDesign();
            SetData(10, 2, 3);
        }
        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();
            SetData(600, 10, 100);
        }

        private void SetData(int days,int r, int c)
        {
            ChartStart = DateTime.Now.Date.AddDays(-3);
            ChartEnd = DateTime.Now.Date.AddDays(days);

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
                    var span = new TimeSpan(random.Next(4) + 1, 0, 0, 0);

                    var d = new SampleModel(d1, span);
                    d.Color = Color.FromArgb(130, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
                    d.WorkingSpan = span;

                    a.Gantts.Add(d);

                }

                ModelList.Add(a);


            }


        }


    }
}
