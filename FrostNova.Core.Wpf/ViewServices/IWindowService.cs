using FrostNova.Core.Wpf.ViewModelBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.ViewServices
{
    public interface IWindowService
    {

        Window? ShowWindow<T>(T vm) where T : ViewModelBase;
        bool? ShowDialogWindow<T>(T vm) where T : DialogWindowViewModelBase;


        



    }
}
