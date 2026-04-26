using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Common;
using FrostNova.Core.Operators;
using FrostNova.Core.Wpf.ViewModelBases;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers;
using static FrostNova.TimeRangeChart.Wpf.Sample.MainWindowViewModel;

namespace FrostNova.TimeRangeChart.Wpf.Sample;

public partial class MilestoneViewModel : ViewModelBase
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
    private ObservableCollection<ITimeRangeRowIndex> _nonWorkingTimes = [];

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
        SetData(5, 2, 3);
    }
    protected override void InitializeOnExecute()
    {
        base.InitializeOnExecute();
    }

    [RelayCommand]
    public void SetData()
    {
        SetData(7, 10, 4);

    }



    private void SetData(int days, int r, int c)
    {
        ModelList.Clear();
        using (_ = new OperationScope(RangeList, BackgroundList, ModelList))
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

                //milestoneを追加
                //1行あたり100こくらい
                for (int j = 0; j < c; j++)
                {
                    var d1 = DateTimeRandomGenerator.GetRandomDateTime(ChartStart, ChartEnd);
                    var span = TimeSpan.Zero;

                    var d = new SampleModel(d1.Date, span);
                    d.RowIndex = i;
                    d.Color = System.Windows.Media.Color.FromArgb(130, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
                    d.WorkingSpan = span;
                    d.MinSpan = random.Next(3) == 0 ? TimeSpan.FromHours(1) : null;


                    d.MilestoneStyle = MilestoneStyle.Circle;

                    a.Tasks.Add(d);
                    RangeList.Add(d);

                }

                ModelList.Add(a);


            }

        }

    }

}
