using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class DataGridEx
    {

        public static readonly DependencyProperty SelectedItemsProperty =
                    DependencyProperty.RegisterAttached(
                        "SelectedItems",
                        typeof(List<object>),
                        typeof(DataGridEx),
                        new PropertyMetadata(null, OnSelectedItemsChanged));

        public static List<object> GetSelectedItems(DataGrid element) => (List<object>)element.GetValue(SelectedItemsProperty);

        public static void SetSelectedItems(DataGrid element, List<object> value) => element.SetValue(SelectedItemsProperty, value);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                var cmd = GetSelectedItemsChangeCommand(ctrl);
                if (cmd != null)
                {
                    if (cmd.CanExecute(e.NewValue))
                    {
                        cmd.Execute(e.NewValue);
                    }
                }
            }
        }

        /// <summary>
        /// command with argument type List<object>
        /// </summary>
        public static readonly DependencyProperty SelectedItemsChangeCommandProperty =
                    DependencyProperty.RegisterAttached(
                        "SelectedItemsChangeCommand",
                        typeof(ICommand),
                        typeof(DataGridEx),
                        new PropertyMetadata(null, OnSelectedItemsChangeCommandChanged));

        /// <summary>
        /// <see cref="SelectedItemsChangeCommandProperty"/>
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static ICommand GetSelectedItemsChangeCommand(DataGrid element) => (ICommand)element.GetValue(SelectedItemsChangeCommandProperty);

        /// <summary>
        /// <see cref="SelectedItemsChangeCommandProperty"/>
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetSelectedItemsChangeCommand(DataGrid element, ICommand value) => element.SetValue(SelectedItemsChangeCommandProperty, value);

        private static void OnSelectedItemsChangeCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {

            }
        }


        public static readonly DependencyProperty SaveKeyProperty =
            DependencyProperty.RegisterAttached(
                "SaveKey",
                typeof(string),
                typeof(DataGridEx),
                new PropertyMetadata(null, OnSaveKeyChanged));

        public static string GetSaveKey(DataGrid element) => (string)element.GetValue(SaveKeyProperty);

        public static void SetSaveKey(DataGrid element, string value) => element.SetValue(SaveKeyProperty, value);

        private static void OnSaveKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {

            }
        }




    }
}
