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
using CommunityToolkit.Mvvm.Input;

namespace FrostNova.TimeRangeChart.Wpf.Sample
{
    public partial class MainWindowViewModel : FrostNova.Core.Wpf.ViewModelBases.ViewModelBase
    {


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

        [ObservableProperty]
        GridViewModel _gridVm = new GridViewModel();

        [ObservableProperty]
        MonthGridViewModel _monthGridVm = new MonthGridViewModel();
        [ObservableProperty]
        OneDayGridViewModel _oneDayGridVm = new OneDayGridViewModel();

        [ObservableProperty]
        ChartViewModel _chartVm = new ChartViewModel();

        [ObservableProperty]
        HolidayViewModel _holidayVM = new HolidayViewModel();



        protected override void InitializeOnDesign()
        {
            base.InitializeOnDesign();
        }
        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();
        }

        [RelayCommand]
        private void SetData()
        {
            OneDayGridVm.SetData();
            //GridVm.SetData();
        }


    }
}
