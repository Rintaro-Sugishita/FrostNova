using FrostNova.Core.Wpf.ViewServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Controls.Dialogs
{
    public static class ColorSelectDialog
    {

        public static Color? Show(Color? referenceColor, Window? ownerWindow = null, WindowStartupLocation? location = null)
        {

            FrostNova.Core.Wpf.Controls.ViewModels.ColorSelectDialogViewModel vm = new ViewModels.ColorSelectDialogViewModel();
            vm.OldColor = referenceColor;
            vm.SelectedColor = referenceColor ?? Colors.Red;

            if (AbstractWindowService.ShowDialogWindow(vm, typeof(Views.ColorSelectDialog), null, ownerWindow, location) == true)
            {
                return vm.SelectedColor;
            }
            else
            {
                return null;
            }


        }


    }
}
