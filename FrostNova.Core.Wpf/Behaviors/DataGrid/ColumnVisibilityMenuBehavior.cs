using FrostNova.Core.Wpf.AttachedProperties;
using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class ColumnVisibilityMenuBehavior : Behavior<DataGrid>
    {
        private Popup? _popup;
        private StackPanel? _menuPanel;
        private string _settingsPath = "";

        private Button? _cornerButton;

        protected override void OnAttached()
        {
            _settingsPath = GetSavePath();
            base.OnAttached();
            AssociatedObject.Unloaded += OnUnloaded;
            AssociatedObject.LayoutUpdated += AssociatedObject_LayoutUpdated;

            var window = System.Windows.Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                window.Closed += OnWindowClosed;
            }
            else
            {
                var dis = AssociatedObject.Dispatcher;
                dis.BeginInvoke((Action)(() =>
                {
                    while (true)
                    {
                        try
                        {
                            var w = Window.GetWindow(AssociatedObject);
                            if (w != null)
                            {
                                w.Closed += OnWindowClosed;
                                return;
                            }
                            else
                            {
                                System.Threading.Thread.Sleep(200);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.WriteLine(ex);
                        }
                    }
                }));
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.LayoutUpdated -= AssociatedObject_LayoutUpdated;
            AssociatedObject.Unloaded -= OnUnloaded;

            if (_cornerButton != null)
                _cornerButton.Click -= OnCornerClick;

            var window = Window.GetWindow(AssociatedObject);
            if (window != null)
                window.Closed -= OnWindowClosed;

            base.OnDetaching();
        }

        private void OnWindowClosed(object? sender, EventArgs e) => SaveColumnSettings();

        private void AssociatedObject_LayoutUpdated(object? sender, EventArgs e)
        {
            if (_cornerButton != null) return;

            _cornerButton = FindCornerButton(AssociatedObject);
            if (_cornerButton != null)
            {
                // 見つかったら即解除（LayoutUpdated は高頻度で発火するため）
                AssociatedObject.LayoutUpdated -= AssociatedObject_LayoutUpdated;
                _cornerButton.Click += OnCornerClick;
                CreatePopup();
                LoadColumnSettings();
            }
        }

        private Button? FindCornerButton(DependencyObject parent)
        {
            return GeneralHelper.FindVisualChildren<Button>(parent)
                .FirstOrDefault(b => b.Command == DataGrid.SelectAllCommand);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            SaveColumnSettings();
            AssociatedObject.Unloaded -= OnUnloaded;
        }

        private void CreatePopup()
        {
            var resetWidthsButton = new Button { Content = "幅リセット", Margin = new Thickness(2) };
            var resetOrderButton  = new Button { Content = "位置リセット", Margin = new Thickness(2) };
            resetWidthsButton.Click += (_, _) => GetSizeBehavior()?.ResetWidths();
            resetOrderButton.Click  += (_, _) => GetSizeBehavior()?.ResetOrder();

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5, 5, 5, 0)
            };
            buttonRow.Children.Add(resetWidthsButton);
            buttonRow.Children.Add(resetOrderButton);

            _menuPanel = new StackPanel { Margin = new Thickness(5, 0, 5, 5) };

            var outerPanel = new StackPanel();
            outerPanel.Children.Add(buttonRow);
            outerPanel.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 2) });
            outerPanel.Children.Add(_menuPanel);

            _popup = new Popup
            {
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                Child = new Border
                {
                    Background = SystemColors.WindowBrush,
                    BorderBrush = SystemColors.ActiveBorderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Child = outerPanel
                }
            };
        }

        private DataGridColumnSizeSavingBehavior? GetSizeBehavior()
            => Microsoft.Xaml.Behaviors.Interaction.GetBehaviors(AssociatedObject)
                .OfType<DataGridColumnSizeSavingBehavior>()
                .FirstOrDefault();

        private void OnCornerClick(object sender, RoutedEventArgs e)
        {
            if (_popup == null || _menuPanel == null) return;

            _menuPanel.Children.Clear();

            foreach (var column in AssociatedObject.Columns)
            {
                // ForcedVisibility が設定されている列はメニューに表示しない
                if (DataGridColumnEx.GetForcedVisibility(column).HasValue) continue;

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

        private static string GetColumnKey(DataGridColumn c)
            => DataGridSaveKeyHelper.GetColumnKey(c);

        private void SaveColumnSettings()
        {
            if (string.IsNullOrEmpty(_settingsPath)) return;
            try
            {
                // ForcedVisibility が設定されている列はプログラマー管轄なので保存しない
                var data = AssociatedObject.Columns
                    .Where(c => !DataGridColumnEx.GetForcedVisibility(c).HasValue)
                    .Select(c => new ColumnState
                    {
                        Key = GetColumnKey(c),
                        IsVisible = c.Visibility == Visibility.Visible
                    }).ToList();

                var dir = Path.GetDirectoryName(_settingsPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[ColumnVisibilityMenuBehavior] Save error: {ex}");
            }
        }

        private void LoadColumnSettings()
        {
            if (string.IsNullOrEmpty(_settingsPath)) return;
            try
            {
                if (!File.Exists(_settingsPath)) return;

                var json = File.ReadAllText(_settingsPath);
                var data = JsonSerializer.Deserialize<List<ColumnState>>(json);
                if (data == null) return;

                var dict = data.ToDictionary(x => x.Key);
                foreach (var column in AssociatedObject.Columns)
                {
                    // ForcedVisibility が設定されている列は OnForcedVisibilityChanged が管理するのでスキップ
                    if (DataGridColumnEx.GetForcedVisibility(column).HasValue) continue;

                    var key = GetColumnKey(column);
                    if (dict.TryGetValue(key, out var state))
                        column.Visibility = state.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[ColumnVisibilityMenuBehavior] Load error: {ex}");
            }
        }

        private string GetSavePath()
        {
            var key = DataGridSaveKeyHelper.GetGridKey(AssociatedObject);
            if (string.IsNullOrEmpty(key)) return string.Empty;

            // 単一ファイル発行では Assembly.Location が空になるため AppContext.BaseDirectory を使う
            string path = AppContext.BaseDirectory;
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            return System.IO.Path.Combine(path, "windowSizeData", "gridColumnVis", key + ".json");
        }

        private class ColumnState
        {
            public string Key { get; set; } = string.Empty;
            public bool IsVisible { get; set; }
        }
    }
}
