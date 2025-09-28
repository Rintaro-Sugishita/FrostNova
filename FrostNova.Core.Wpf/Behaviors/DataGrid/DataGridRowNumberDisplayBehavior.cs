using CommunityToolkit.Mvvm.Input;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridRowNumberDisplayBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.LoadingRow += AssociatedObject_LoadingRow;
            //RowNumberRefreshCommand = new RelayCommand<DragMoveEventArgs>(RefreshRowNumber);
        }



        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.LoadingRow -= AssociatedObject_LoadingRow;
        }

        private void AssociatedObject_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

    }
}
