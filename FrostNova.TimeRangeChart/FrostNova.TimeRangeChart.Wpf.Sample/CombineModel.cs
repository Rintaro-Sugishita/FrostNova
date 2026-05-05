using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using FrostNova.Core.Operators;
using FrostNova.Core.Wpf.ViewModelBases;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using static FrostNova.TimeRangeChart.Wpf.Sample.MainWindowViewModel;

namespace FrostNova.TimeRangeChart.Wpf.Sample;

public partial class CombineModel : GridViewModel
{
    public override void SetData()
    {
        SetData(10, 1, 2);

    }

    protected override void SetData(int days, int r, int c)
    {
        ModelList.Clear();
        using (_ = new OperationScope(NonWorkingTimes, RangeList, BackgroundList, ModelList))
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
                    d.CanStack = true;
                    if (j%2 == 0)
                    {
                        
                        d.StackBaseTop = 10;
                        d.CanStack = false;

                    }

                    a.Tasks.Add(d);
                    RangeList.Add(d);

                }

              

                ModelList.Add(a);


            }

        }



    }



}
