using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using static FrostNova.TimeRangeChart.Wpf.Sample.MainWindowViewModel;

namespace FrostNova.TimeRangeChart.Wpf.Sample
{
    public partial class GridViewModel : FrostNova.Core.Wpf.ViewModelBases.ViewModelBase
    {

        [ObservableProperty]
        ObservableCollection<GridSampleData> _modelList = new ObservableCollection<GridSampleData>();


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
            SetData(10, 2, 3);
        }
        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();
        }


        public void SetData()
        {
            SetData(300, 10 + Random.Shared.Next(2), 10);

        }

        private void SetData(int days, int r, int c)
        {
            ModelList.Clear();


            var nowDay = DateTime.Now.Date;
            ChartStart = DateTime.Now.Date.AddDays(-3);
            ChartEnd = DateTime.Now.Date.AddDays(days);

            //土日を非稼働時間にする
            for (DateTime i = ChartStart; i <= ChartEnd; i = i.AddDays(1))
            {
                if (i.DayOfWeek == DayOfWeek.Saturday || i.DayOfWeek == DayOfWeek.Sunday)
                {
                    NonWorkingTimes.Add(new FixedTimeRange(i.Date, TimeSpan.FromDays(1)));
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

                    var d = new SampleModel(d1.Date, span);
                    d.Color = Color.FromArgb(130, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
                    d.WorkingSpan = span;
                    d.MinSpan = random.Next(3) == 0 ? TimeSpan.FromHours(1) : null;

                    a.Tasks.Add(d);

                }
                for (int j = 0; j < c; j++)
                {
                    var d1 = DateTimeRandomGenerator.GetRandomDateTime(ChartStart, ChartEnd);
                    var span = new TimeSpan(random.Next(4) + 1, 0, 0, 0);

                    var d = new BackgroundTimeRange();
                    d.StartTime = d1;
                    d.EndTime = d1 + span;
                    d.WorkingSpan = span;
                    var color = new SolidColorBrush(Color.FromArgb(255, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
                    color.Freeze();
                    d.Background = color;
                    d.WorkingSpan = span;

                    a.BackgroundTimeRanges.Add(d);
                }
                //var d3 = new BackgroundTimeRange();
                //d3.StartTime = ChartStart;
                //d3.EndTime = ChartEnd;
                //d3.WorkingSpan = ChartEnd - ChartStart;
                //var color = new SolidColorBrush(Color.FromArgb(255, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
                //color.Freeze();
                //d3.Background = color;

                //a.BackgroundTimeRanges.Add(d3);


                ModelList.Add(a);


            }


        }
    }
}
