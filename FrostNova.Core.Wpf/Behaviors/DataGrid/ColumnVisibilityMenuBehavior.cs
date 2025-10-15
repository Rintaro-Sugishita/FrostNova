using FrostNova.Core.Wpf.AttachedProperties;
using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class ColumnVisibilityMenuBehavior : Behavior<DataGrid>
    {
        private Popup? _popup;
        private StackPanel? _menuPanel;
        private string _settingsPath = "";

        private FrameworkElement? _cornerElement;

        protected override void OnAttached()
        {
            _settingsPath = GetSavePath();
            base.OnAttached();
            //AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.Unloaded += OnUnloaded;

            AssociatedObject.LayoutUpdated += AssociatedObject_LayoutUpdated;
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

        private void DataGridColumnSizeSavingBehavior_Closed(object? sender, EventArgs e)
        {
            SaveColumnSettings();
        }

        protected override void OnDetaching()
        {
            //AssociatedObject.Loaded -= OnLoaded;
            //AssociatedObject.Unloaded -= OnUnloaded;
            AssociatedObject.LayoutUpdated -= AssociatedObject_LayoutUpdated;
            base.OnDetaching();
        }



        private void AssociatedObject_LayoutUpdated(object? sender, EventArgs e)
        {
            if (_cornerElement == null)
            {
                _cornerElement = FindCornerButton(AssociatedObject);
                if (_cornerElement != null && _cornerElement is Button button)
                {
                    button.Click += OnCornerClick;
                    _cornerElement.MouseLeftButtonDown += OnCornerClick;
                    CreatePopup();
                    LoadColumnSettings();
                }
            }
        }

        private Button? FindCornerButton(DependencyObject parent)
        {
            // DataGrid のテンプレートにある SelectAllButton を検索する
            return GeneralHelper.FindVisualChildren<Button>(parent)
                .FirstOrDefault(b =>
                    b.Command == DataGrid.SelectAllCommand); // SelectAllCommand が付いている唯一のボタン
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            SaveColumnSettings();
            AssociatedObject.Unloaded -= OnUnloaded;
        }

        private void CreatePopup()
        {
            _menuPanel = new StackPanel { Margin = new Thickness(5) };
            _popup = new Popup
            {
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                //AllowsTransparency = true,
                Child = new Border
                {
                    Background = SystemColors.WindowBrush,
                    BorderBrush = SystemColors.ActiveBorderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Child = _menuPanel
                }
            };
        }

        private void OnCornerClick(object sender, RoutedEventArgs e)
        {
            if (_popup == null || _menuPanel == null) return;

            _menuPanel.Children.Clear();

            foreach (var column in AssociatedObject.Columns)
            {
                var checkBox = new CheckBox
                {
                    Content = column.Header?.ToString(),
                    IsChecked = column.Visibility == Visibility.Visible,
                    Margin = new Thickness(4)
                };

                checkBox.Checked += (_, _) => column.Visibility = Visibility.Visible;
                checkBox.Unchecked += (_, _) => column.Visibility = Visibility.Collapsed;

                _menuPanel.Children.Add(checkBox);
            }

            _popup.PlacementTarget = sender as UIElement;
            _popup.IsOpen = true;
        }

        private void SaveColumnSettings()
        {
            try
            {
                var data = AssociatedObject.Columns.Select(c => new ColumnState
                {
                    Header = c.Header?.ToString() ?? "",
                    IsVisible = c.Visibility == Visibility.Visible
                }).ToList();

                var dir = Path.GetDirectoryName(_settingsPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ColumnVisibilityMenuBehavior] Save error: {ex}");
            }
        }

        private void LoadColumnSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return;

                var json = File.ReadAllText(_settingsPath);
                var data = JsonSerializer.Deserialize<List<ColumnState>>(json);

                if (data == null) return;

                foreach (var state in data)
                {
                    var column = AssociatedObject.Columns
                        .FirstOrDefault(c => c.Header?.ToString() == state.Header);

                    if (column != null)
                        column.Visibility = state.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ColumnVisibilityMenuBehavior] Load error: {ex}");
            }
        }
        private string GetSavePath()
        {

            var key = DataGridEx.GetSaveKey(AssociatedObject);


            if (string.IsNullOrEmpty(key)) { return string.Empty; }
            string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            string fullPath = System.IO.Path.Combine(path, "windowSizeData", "gridColumnVis", key + ".json");

            return fullPath;

        }

        //private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        //{
        //    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        //    {
        //        var child = VisualTreeHelper.GetChild(parent, i);
        //        if (child is T t)
        //            return t;
        //        var result = FindVisualChild<T>(child);
        //        if (result != null)
        //            return result;
        //    }
        //    return null;
        //}

        private class ColumnState
        {
            public string Header { get; set; } = string.Empty;
            public bool IsVisible { get; set; }
        }
    }
}
