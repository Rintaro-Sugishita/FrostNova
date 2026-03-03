using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Wpf.Models.DataGrdExtCols;

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


        public static readonly DependencyProperty ExtColumnsProperty =
            DependencyProperty.RegisterAttached(
                "ExtColumns",
                typeof(ObservableCollection<ExtensionColumnInfo>),
                typeof(DataGridEx),
                new PropertyMetadata(null, OnExtColumnsChanged));

        public static ObservableCollection<ExtensionColumnInfo> GetExtColumns(DataGrid element) => (ObservableCollection<ExtensionColumnInfo>)element.GetValue(ExtColumnsProperty);

        public static void SetExtColumns(DataGrid element, ObservableCollection<ExtensionColumnInfo> value) => element.SetValue(ExtColumnsProperty, value);

        private static void OnExtColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if (e.OldValue is ObservableCollection<ExtensionColumnInfo> oldList)
                    oldList.CollectionChanged -= (s, args) => RebuildColumns(dataGrid, oldList);

                if (e.NewValue is ObservableCollection<ExtensionColumnInfo> newList)
                {
                    newList.CollectionChanged += (s, args) => RebuildColumns(dataGrid, newList);
                    RebuildColumns(dataGrid, newList);
                }
            }
        }

        private static void RebuildColumns(DataGrid dataGrid, IEnumerable<ExtensionColumnInfo> config)
        {
            // 自作した拡張カラムだけを特定して削除（固定列は残す）
            var dynamicCols = dataGrid.Columns.Where(c => (bool)c.GetValue(DataGridColumnEx .IsGeneratedProperty)).ToList();
            foreach (var col in dynamicCols) dataGrid.Columns.Remove(col);

            if (config == null) return;

            foreach (var info in config)
            {
                DataGridColumn newCol = info.Type switch
                {
                    ColumnType.checkBox => new DataGridCheckBoxColumn
                    {
                        Binding = CreateBinding(info.UniqueKey)
                    },
                    ColumnType.comboBox => new DataGridComboBoxColumn
                    {
                        SelectedValueBinding = CreateBinding(info.UniqueKey),
                        SelectedValuePath = "Value",
                        DisplayMemberPath = "Label",
                        ItemsSource = info.Options
                    },

                    ColumnType.numeric => new DataGridTextColumn
                    {
                        Binding = CreateBinding(info.UniqueKey),
                        ElementStyle = CreateRightAlignedStyle() // 数値なので右寄せ
                    },
                    ColumnType.date => new DataGridTemplateColumn
                    {
                        CellTemplate = CreateDatePickerTemplate(info.UniqueKey),
                        CellEditingTemplate = CreateDatePickerTemplate(info.UniqueKey)
                    },
                    ColumnType.button => CreateButtonColumn(info),

                    _ => new DataGridTextColumn
                    {
                        Binding = CreateBinding(info.UniqueKey)
                    }
                };

                newCol.IsReadOnly = info.IsReadOnly;
                newCol.Visibility = info.Visibility;

                // 幅の設定 (文字列からDataGridLengthへの変換)
                var converter = new DataGridLengthConverter();
                if (converter.ConvertFromString(info.Width) is DataGridLength length)
                {
                    newCol.Width = length;
                }
                newCol.Header = info.HeaderText;
                newCol.SetValue(DataGridColumnEx.IsGeneratedProperty, true); // 削除用目印をセット
                dataGrid.Columns.Add(newCol);
            }
        }

        private static Binding CreateBinding(string key)
        {
            // Dictionaryのキーにドットが含まれることを想定し [] で囲む
            return new Binding($"CustomItems[{key}]")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay
            };
        }
        // 数値用の右寄せスタイル
        private static Style CreateRightAlignedStyle()
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            return style;
        }

        // DatePickerのテンプレート生成
        private static DataTemplate CreateDatePickerTemplate(string key)
        {
            var factory = new FrameworkElementFactory(typeof(DatePicker));
            factory.SetBinding(DatePicker.SelectedDateProperty, new Binding($"CustomItems[{key}]") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            factory.SetValue(DatePicker.BorderThicknessProperty, new Thickness(0));
            return new DataTemplate { VisualTree = factory };
        }

        // ボタンのテンプレート生成
        private static DataGridColumn CreateButtonColumn(ExtensionColumnInfo info)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));
            factory.SetValue(Button.ContentProperty, info.HeaderText);

            // info.Command が設定されていれば、それをボタンに結びつける
            if (info.Command != null)
            {
                factory.SetValue(Button.CommandProperty, info.Command);

                // 【重要】CommandParameter に「行のデータ(Class1)」をバインド
                // テンプレート内での "." は、DataGridの各行のデータ(DataContext)を指します
                factory.SetBinding(Button.CommandParameterProperty, new Binding("."));
            }

            template.VisualTree = factory;
            return new DataGridTemplateColumn
            {
                Header = info.HeaderText,
                CellTemplate = template
            };
        }



    }
}
