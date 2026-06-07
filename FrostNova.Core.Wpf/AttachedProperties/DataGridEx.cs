using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using FrostNova.Core.Wpf.VisualHelpers;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class DataGridEx
    {
        public static readonly DependencyProperty IsEditOnEnterProperty =
            DependencyProperty.RegisterAttached(
                "IsEditOnEnter",
                typeof(bool),
                typeof(DataGridEx),
                new PropertyMetadata(false, OnIsEditOnEnterChanged));

        public static bool? GetIsEditOnEnter(DataGrid element) => (bool)element.GetValue(IsEditOnEnterProperty);

        public static void SetIsEditOnEnter(DataGrid element, bool value) => element.SetValue(IsEditOnEnterProperty, value);

        private static void OnIsEditOnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid && e.NewValue is bool newValue && newValue)
            {
                GeneralHelper.SetBehavior<FrostNova.Core.Wpf.Behaviors.DataGridEditOnEnterBehavior>(d,true);
            }
            else
            {
                GeneralHelper.ClearBehavior<FrostNova.Core.Wpf.Behaviors.DataGridEditOnEnterBehavior>(d);
            }
        }



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
        /// command with argument type List
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

        public static readonly DependencyProperty ExtColumnBindingPathProperty =
            DependencyProperty.RegisterAttached(
                "ExtColumnBindingPath",
                typeof(string),
                typeof(DataGridEx),
                new PropertyMetadata("CustomItems", OnExtColumnBindingPathChanged));

        public static string GetExtColumnBindingPath(DataGrid element) => (string)element.GetValue(ExtColumnBindingPathProperty);

        public static void SetExtColumnBindingPath(DataGrid element, string value) => element.SetValue(ExtColumnBindingPathProperty, value);

        private static void OnExtColumnBindingPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {

            }
        }


        public static readonly DependencyProperty ExtColumnInsertIndexProperty =
            DependencyProperty.RegisterAttached(
                "ExtColumnInsertIndex",
                typeof(int?),
                typeof(DataGridEx),
                new PropertyMetadata(null, OnExtColumnInsertIndexChanged));

        public static int? GetExtColumnInsertIndex(DataGrid element) => (int?)element.GetValue(ExtColumnInsertIndexProperty);

        public static void SetExtColumnInsertIndex(DataGrid element, int? value) => element.SetValue(ExtColumnInsertIndexProperty, value);

        private static void OnExtColumnInsertIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
                    if (dataGrid.GetValue(CollectionHandlerProperty)
                        is NotifyCollectionChangedEventHandler oldHandler)
                    {
                        oldList.CollectionChanged -= oldHandler;
                    }

                if (e.NewValue is ObservableCollection<ExtensionColumnInfo> newList)
                {
                    var handler =
                        new NotifyCollectionChangedEventHandler(
                            (s, args) => RebuildColumns(dataGrid, newList));

                    newList.CollectionChanged += handler;

                    dataGrid.SetValue(CollectionHandlerProperty, handler);
                    RebuildColumns(dataGrid, newList);
                }
            }
        }
        private static readonly DependencyProperty CollectionHandlerProperty =
    DependencyProperty.RegisterAttached(
        "CollectionHandler",
        typeof(NotifyCollectionChangedEventHandler),
        typeof(DataGridEx));
        private static void RebuildColumns(DataGrid dataGrid, IEnumerable<ExtensionColumnInfo> config)
        {
            // 自作した拡張カラムだけを特定して削除（固定列は残す）
            var dynamicCols = dataGrid.Columns.Where(c => (bool)c.GetValue(DataGridColumnEx.IsGeneratedProperty)).ToList();
            foreach (var col in dynamicCols) dataGrid.Columns.Remove(col);

            if (config == null) return;

            var insertIndex = GetExtColumnInsertIndex(dataGrid)?? (Math.Max( 0,  dataGrid.Columns.Count-1));
            foreach (var info in config)
            {
                DataGridColumn newCol = info.Type switch
                {
                    ColumnType.checkBox => new DataGridCheckBoxColumn
                    {
                        Binding = CreateBinding(dataGrid, info.UniqueKey),
                        ElementStyle = CreateCenterCheckBoxStyle(),
                        EditingElementStyle = CreateCenterCheckBoxStyle()
                    },
                    ColumnType.comboBox => new DataGridComboBoxColumn
                    {
                        SelectedValueBinding = CreateBinding(dataGrid, info.UniqueKey),
                        SelectedValuePath = "Value",
                        DisplayMemberPath = "Label",
                        ItemsSource = info.Options
                    },

                    ColumnType.numeric => new DataGridTextColumn
                    {
                        Binding = CreateBinding(dataGrid, info.UniqueKey),
                        ElementStyle = CreateRightAlignedStyle(), // 数値なので右寄せ
                        EditingElementStyle = CreateRightAlignedTextBoxStyle()

                    },
                    ColumnType.date => new DataGridTemplateColumn
                    {
                        CellTemplate = CreateDatePickerTemplate(dataGrid, info.UniqueKey),
                        CellEditingTemplate = CreateDatePickerTemplate(dataGrid, info.UniqueKey)
                    },
                    ColumnType.button => CreateButtonColumn(info),

                    _ => new DataGridTextColumn
                    {
                        Binding = CreateBinding(dataGrid, info.UniqueKey)
                    }
                };

                newCol.IsReadOnly = info.IsReadOnly;
                newCol.Visibility = info.Visibility;
                newCol.CanUserSort = info.CanUserSort;

                // 幅の設定 (文字列からDataGridLengthへの変換)
                var converter = new DataGridLengthConverter();
                if (converter.ConvertFromString(info.Width) is DataGridLength length)
                {
                    newCol.Width = length;
                }
                newCol.Header = info.HeaderText;
                newCol.SetValue(DataGridColumnEx.IsGeneratedProperty, true); // 削除用目印をセット
                newCol.SetValue(DataGridColumnEx.KeyProperty, info.UniqueKey); // 幅保存ビヘイビアが識別できるようにKeyをセット
                dataGrid.Columns.Insert(insertIndex, newCol);
                insertIndex++;
            }
        }

        private static Binding CreateBinding(DependencyObject datagrid, string key)
        {
            var bindingPath = datagrid.GetValue(ExtColumnBindingPathProperty);

            // Dictionaryのキーにドットが含まれることを想定し [] で囲む
            return new Binding($"{bindingPath}[{key}]")
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
        private static Style CreateRightAlignedTextBoxStyle()
        {
            var style = new Style(typeof(TextBox));

            style.Setters.Add(
                new Setter(
                    TextBox.TextAlignmentProperty,
                    TextAlignment.Right));

            return style;
        }
        /// <summary>
        /// CheckBoxの中央寄せスタイル
        /// </summary>
        /// <returns></returns>
        private static Style CreateCenterCheckBoxStyle()
        {
            var style = new Style(typeof(CheckBox));

            style.Setters.Add(
                new Setter(
                    FrameworkElement.HorizontalAlignmentProperty,
                    HorizontalAlignment.Center));

            style.Setters.Add(
                new Setter(
                    FrameworkElement.VerticalAlignmentProperty,
                    VerticalAlignment.Center));

            return style;
        }
        // DatePickerのテンプレート生成
        private static DataTemplate CreateDatePickerTemplate(DependencyObject datagrid, string key)
        {
            var bindingPath = datagrid.GetValue(ExtColumnBindingPathProperty);

            var factory = new FrameworkElementFactory(typeof(DatePicker));
            factory.SetBinding(DatePicker.SelectedDateProperty, new Binding($"{bindingPath}[{key}]") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
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
            factory.SetValue(
                FrameworkElement.MarginProperty,
                new Thickness(0));
            template.VisualTree = factory;
            return new DataGridTemplateColumn
            {
                Header = info.HeaderText,
                CellTemplate = template
            };
        }
        private static Style CreateGridButtonStyle()
        {
            var style = new Style(typeof(Button));

            style.Setters.Add(
                new Setter(
                    FrameworkElement.MarginProperty,
                    new Thickness(0)));

            style.Setters.Add(
                new Setter(
                    Control.PaddingProperty,
                    new Thickness(4, 0, 4, 0)));

            return style;
        }

    }
}
