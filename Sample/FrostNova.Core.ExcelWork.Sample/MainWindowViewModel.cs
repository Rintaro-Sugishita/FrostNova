using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Wpf.ViewModelBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrostNova.Core.ExcelWork;

namespace FrostNova.Core.ExcelWork.Sample
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [RelayCommand]
        private void Test1()
        {
            using (var xls = new ExcelOperator())
            {
                xls.Open();

                var sheet1 = xls.GetSheet(1);
                var range = sheet1.GetRange("A1:B2");
                var value = range.GetValue<object>();

            }

        }



    }
}
