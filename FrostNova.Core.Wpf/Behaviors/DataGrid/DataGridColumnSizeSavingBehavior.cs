using Microsoft.VisualBasic;
using Microsoft.Xaml.Behaviors;
using FrostNova.Core.Wpf.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FrostNova.Core.Wpf.AttachedProperties;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridColumnSizeSavingBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            //AssociatedObject.Columns.CollectionChanged += Columns_CollectionChanged;
            AssociatedObject.Initialized += AssociatedObject_Initialized;
            AssociatedObject.Unloaded += DataGridColumnSizeSavingBehavior_Closed;

            var window = System.Windows.Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                //windowが見つかったらそのままセット
                window.Closed += DataGridColumnSizeSavingBehavior_Closed;
            }
            else
            {
                //見つからなかったら (UserControl内からの呼び出しなど)
                //windowに追加されるまでループして待つ
                var dis = AssociatedObject.Dispatcher;
                dis.BeginInvoke((Action)(() =>
                                {
                                    while (true)
                                    {
                                        try
                                        {
                                            var window = Window.GetWindow(AssociatedObject);
                                            if (window != null)
                                            {
                                                window.Closed += DataGridColumnSizeSavingBehavior_Closed;
                                                return;
                                            }
                                            else
                                            {
                                                System.Threading.Thread.Sleep(200);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex);
                                        }
                                    }
                                }));

            }


        }

        protected override void OnDetaching()
        {
            System.Windows.Window.GetWindow(AssociatedObject).Closed -= DataGridColumnSizeSavingBehavior_Closed;
            AssociatedObject.Initialized -= AssociatedObject_Initialized;
            base.OnDetaching();

        }

        private void AssociatedObject_Initialized(object? sender, EventArgs e)
        {
            if (sender is DataGrid grid)
            {
                LoadData(grid);
            }
        }

        private void DataGridColumnSizeSavingBehavior_Closed(object? sender, EventArgs e)
        {
            SaveData(AssociatedObject);
        }

        /// <summary>
        /// save key
        /// </summary>
        public string SaveKey
        {
            get { return (string)GetValue(SaveKeyProperty); }
            set { SetValue(SaveKeyProperty, value); }
        }
        public static readonly DependencyProperty SaveKeyProperty = DependencyProperty.Register(nameof(SaveKey), typeof(string), typeof(DataGridColumnSizeSavingBehavior));

        /// <summary>
        /// string
        /// </summary>
        public string SavePath
        {
            get { return (string)GetValue(SavePathProperty); }
            set { SetValue(SavePathProperty, value); }
        }
        public static readonly DependencyProperty SavePathProperty = DependencyProperty.Register(nameof(SavePath), typeof(string), typeof(DataGridColumnSizeSavingBehavior));

        private string GetSavePath()
        {
            if (string.IsNullOrEmpty(SaveKey)) { return string.Empty; }
            string path = SavePath;
            if (string.IsNullOrEmpty(path))
            {
                path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            }
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            string fullPath = System.IO.Path.Combine(path, "windowSizeData", "gridColumnData", SaveKey + ".json");

            return fullPath;

        }


        private void LoadData(DataGrid grid)
        {
            if (grid == null) { return; }
            string fullPath = GetSavePath();

            var data = FrostNova.Core.JsonData.JsonDataHelper.LoadFromFile<List<DataGridColumnData>>(fullPath);
            if (data == null || data.Count == 0) { return; }
            foreach (var item in grid.Columns)
            {
                string key = DataGridColumnEx.GetKey(item);
                if (string.IsNullOrEmpty(key)) { continue; }
                var dataItem = data.FirstOrDefault(x => x.Key == key);
                if (dataItem != null)
                {

                    if (dataItem.DisplayIndex != -1) item.DisplayIndex = dataItem.DisplayIndex;
                    item.Width = dataItem.Width;
                }
            }

        }

        private void SaveData(DataGrid grid)
        {

            string fullPath = GetSavePath();
            var saveData = new List<DataGridColumnData>();
            foreach (var item in grid.Columns)
            {
                string key = DataGridColumnEx.GetKey(item);
                bool canSave = DataGridColumnEx.GetCanSaveWidth(item);
                if (!canSave) continue;
                if (string.IsNullOrEmpty(key)) continue;
                DataGridColumnData columnData = new DataGridColumnData();
                columnData.Key = key;
                columnData.DisplayIndex = item.DisplayIndex;
                columnData.Width = item.ActualWidth;

                saveData.Add(columnData);
            }

            FrostNova.Core.JsonData.JsonDataHelper.SaveToFile(fullPath, saveData, true);
        }



    }
}
