using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Wpf.ViewModelBases;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.ViewServices
{
    public partial class FloatingWindowViewModelBase : ViewModelBase
    {
        public Action? RequestClose { get; set; }


        [RelayCommand]
        protected virtual void Close()
        {
            RequestClose?.Invoke();
        }

        protected internal virtual void OnLoaded()
        {
        }



        protected internal virtual void OnClosing(CancelEventArgs e)
        {
        }

        protected internal virtual void OnClosed()
        {

        }


    }
}
