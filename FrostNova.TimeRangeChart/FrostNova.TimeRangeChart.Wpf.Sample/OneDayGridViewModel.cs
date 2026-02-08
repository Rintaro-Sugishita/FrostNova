using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FrostNova.TimeRangeChart.Wpf.Sample.MainWindowViewModel;

namespace FrostNova.TimeRangeChart.Wpf.Sample
{
    public partial class OneDayGridViewModel : FrostNova.Core.Wpf.ViewModelBases.ViewModelBase
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


        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();

        }

        public void SetData()
        {
            SetData(10, 2);
        }

        private void SetData(int r, int c)
        {
            var nowDay = DateTime.Now.Date;
            ChartStart = nowDay.AddHours(8);
            ChartEnd = nowDay.AddHours(20);

            //お昼を非稼働時間にする
            NonWorkingTimes.Add(new FixedTimeRange(nowDay.AddHours(12), nowDay.AddHours(13)));



            Random random = new Random();

            for (int i = 0; i < r; i++)
            {
                var a = new GridSampleData();
                //1行のデータ
                a.Name = "data" + i;


                ModelList.Add(a);
            }

            var current = nowDay.AddHours(9);

            //
            for (int j = 0; current <= ChartEnd; j++)
            {
                //配置位置を決定
                var c2 = random.Next(ModelList.Count);

                var nextMinute = random.Next(200);
                var span = new TimeSpan(0, 0, nextMinute, 0);

                var task =  new SampleModel(current, span);
                current += span;
                ModelList[c2].Tasks.Add(task);
            }



        }

    }
}
