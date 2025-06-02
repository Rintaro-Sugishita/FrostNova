using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Wpf.ViewServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.ViewModelBases
{
    public partial class DialogWindowViewModelBase : FloatingWindowViewModelBase
    {


        public Action? RequestCancelClose;

        [RelayCommand]
        protected void CloseCancel()
        {
            RequestCancelClose?.Invoke();
        }

        protected internal virtual bool OnOkClosing()
        {
            return true;
        }





    }
}
