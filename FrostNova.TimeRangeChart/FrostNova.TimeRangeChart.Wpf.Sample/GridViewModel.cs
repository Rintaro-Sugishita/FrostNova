using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Common;
using FrostNova.Core.Operators;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        NotifyCollection<GridSampleData> _modelList = new NotifyCollection<GridSampleData>();


        [ObservableProperty]
        NotifyCollection<ITimeRangeRowIndex> _rangeList = new NotifyCollection<ITimeRangeRowIndex>();

        [ObservableProperty]
        NotifyCollection<IBackgroundTimeRange> _backgroundList = new NotifyCollection<IBackgroundTimeRange>();

        [ObservableProperty]
        private DateTime _chartStart = DateTime.Now.Date.AddMonths(-5);

        [ObservableProperty]
        private DateTime _chartEnd = DateTime.Now.Date.AddMonths(10);

        [ObservableProperty]
        private TimeSpan _minSpan = TimeSpan.FromDays(1);

        [ObservableProperty]
        private int _aaa = 5;

        [ObservableProperty]
        private NotifyCollection<ITimeRangeRowIndex> _nonWorkingTimes = [];

        [ObservableProperty]
        private Func<object, int> _rowFunc = (s) =>
        {
            if (s is SampleModel ss)
            {
                return ss.RowIndex;
            }
            return -1;
        };

        protected override void InitializeOnDesign()
        {
            base.InitializeOnDesign();
            SetData(10, 2, 3);
        }
        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();
        }

        [RelayCommand]
        public void SetData()
        {
            SetData(1000, 100, 600);

        }



        private void SetData(int days, int r, int c)
        {
            ModelList.Clear();
            using (_ = new OperationScope(NonWorkingTimes,RangeList, BackgroundList, ModelList))
            {

                RangeList.Clear();
                BackgroundList.Clear();

                var nowDay = DateTime.Now.Date;
                ChartStart = DateTime.Now.Date.AddDays(-3);
                ChartEnd = DateTime.Now.Date.AddDays(days);
                
                //土日を非稼働時間にする
                for (DateTime i = ChartStart; i <= ChartEnd; i = i.AddDays(1))
                {
                    for (int j = 0; j < r; j++)
                    {
                        if (i.DayOfWeek == DayOfWeek.Saturday || i.DayOfWeek == DayOfWeek.Sunday)
                        {
                            NonWorkingTimes.Add(new FixedTimeRange(i.Date, TimeSpan.FromDays(1)) { RowIndex = j });
                        }
                    }
                }


                
                Random random = new Random();

                for (int i = 0; i < r; i++)
                {
                    var a = new GridSampleData();
                    a.Name = "data" + i;

                    //taskを追加
                    //1行あたり100こくらい
                    for (int j = 0; j < c; j++)
                    {
                        var d1 = DateTimeRandomGenerator.GetRandomDateTime(ChartStart, ChartEnd);
                        var span = new TimeSpan(random.Next(4) + 1, 0, 0, 0);

                        var d = new SampleModel(d1.Date, span);
                        d.RowIndex = i;
                        d.Color = Color.FromArgb(130, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
                        d.WorkingSpan = span;
                        d.MinSpan = random.Next(3) == 0 ? TimeSpan.FromHours(1) : null;
                        d.StackBaseTop = 20;
                        a.Tasks.Add(d);
                        RangeList.Add(d);

                    }

                    //backgroundを追加
                    //for (int j = 0; j < c; j++)
                    //{
                    //    var d1 = DateTimeRandomGenerator.GetRandomDateTime(ChartStart, ChartEnd);
                    //    var span = new TimeSpan(random.Next(4) + 1, 0, 0, 0);

                    //    var d = new BackgroundTimeRange();
                    //    d.RowIndex = i;
                    //    d.StartTime = d1.Date;
                    //    d.EndTime = d1.Date + span;
                    //    d.WorkingSpan = span;
                    //    var color = new SolidColorBrush(Color.FromArgb(255, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
                    //    color.Freeze();
                    //    d.Background = color;
                    //    d.WorkingSpan = span;

                    //    BackgroundList.Add(d);

                    //    a.BackgroundTimeRanges.Add(d);
                    //}




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

        [RelayCommand]
        private void PreviewTaskDrop(DropOnRowEventArgs e)
        {
            Debug.WriteLine("Raised Preview TaskDrop");
        }

        [RelayCommand]
        private void TaskDrop(DropOnRowEventArgs e)
        {
            Debug.WriteLine("Raised TaskDrop");
        }

        [RelayCommand]
        private void TaskDragStart(DragDropRangeEventArgs e)
        {
            Debug.WriteLine("Raised DragStart");
        }

        [RelayCommand]
        private void TaskDragEnd(DragDropRangeEventArgs e)
        {
            Debug.WriteLine("Raised DragEnd");
        }

    }
}
