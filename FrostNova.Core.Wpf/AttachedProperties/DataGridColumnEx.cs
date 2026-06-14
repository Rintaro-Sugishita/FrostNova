using FrostNova.Core.Wpf.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class DataGridColumnEx
    {
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.RegisterAttached("Key", typeof(string), typeof(DataGridColumnEx), new FrameworkPropertyMetadata(string.Empty));

        public static string GetKey(DependencyObject target)
        {
            return (string)target.GetValue(KeyProperty);
        }

        public static void SetKey(DependencyObject target, string value)
        {
            target.SetValue(KeyProperty, value);
        }

        public static readonly DependencyProperty CanSaveWidthProperty =
            DependencyProperty.RegisterAttached("CanSaveWidth", typeof(bool), typeof(DataGridColumnEx), new FrameworkPropertyMetadata(true));


        public static bool GetCanSaveWidth(DependencyObject target)
        {
            return (bool)target.GetValue(CanSaveWidthProperty);
        }

        public static void SetCanSaveWidth(DependencyObject target, bool value)
        {
            target.SetValue(CanSaveWidthProperty, value);
        }


        public static readonly DependencyProperty CanDragRowProperty =
            DependencyProperty.RegisterAttached("CanDragRow", typeof(bool), typeof(DataGridColumnEx), new FrameworkPropertyMetadata(false));

        public static bool GetCanDragRow(DependencyObject target)
        {
            return (bool)target.GetValue(CanDragRowProperty);
        }

        public static void SetCanDragRow(DependencyObject target, bool value)
        {
            target.SetValue(CanDragRowProperty, value);
        }

        public static readonly DependencyProperty UseSyncProperty =
            DependencyProperty.RegisterAttached("UseSync", typeof(bool), typeof(DataGridColumnEx), new FrameworkPropertyMetadata(true));

        public static bool GetUseSync(DependencyObject target)
        {
            return (bool)target.GetValue(UseSyncProperty);
        }

        public static void SetUseSync(DependencyObject target, bool value)
        {
            target.SetValue(UseSyncProperty, value);
        }


        public static readonly DependencyProperty IsGeneratedProperty =
             DependencyProperty.RegisterAttached(
                 "IsGenerated",
                 typeof(bool),
                 typeof(DataGridColumnEx),
                 new PropertyMetadata(false, OnIsGeneratedChanged));

        public static bool GetIsGenerated(DependencyObject element) => (bool)element.GetValue(IsGeneratedProperty);

        public static void SetIsGenerated(DependencyObject element, bool value) => element.SetValue(IsGeneratedProperty, value);

        private static void OnIsGeneratedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DependencyObject ctrl)
            {

            }
        }

        /// <summary>
        /// プログラマーがユーザー操作より優先して表示状態を強制したい列に設定する。
        /// セットされた列はコーナーメニューに表示されず、ユーザーの保存設定も無視される。
        /// null の場合はユーザー設定が適用される通常動作。
        /// </summary>
        public static readonly DependencyProperty ForcedVisibilityProperty =
            DependencyProperty.RegisterAttached(
                "ForcedVisibility",
                typeof(Visibility?),
                typeof(DataGridColumnEx),
                new PropertyMetadata(null, OnForcedVisibilityChanged));

        public static Visibility? GetForcedVisibility(DependencyObject target)
            => (Visibility?)target.GetValue(ForcedVisibilityProperty);

        public static void SetForcedVisibility(DependencyObject target, Visibility? value)
            => target.SetValue(ForcedVisibilityProperty, value);

        // 強制前の Visibility を退避するための内部プロパティ
        private static readonly DependencyProperty PreForcedVisibilityProperty =
            DependencyProperty.RegisterAttached(
                "PreForcedVisibility",
                typeof(Visibility?),
                typeof(DataGridColumnEx),
                new PropertyMetadata(null));

        private static void OnForcedVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not System.Windows.Controls.DataGridColumn col) return;

            if (e.NewValue is Visibility forced)
            {
                // 初めて強制するとき（null→値）だけ元の状態を退避する。
                // null→値→別値 の連続変化で退避値を上書きしないようにする。
                if (e.OldValue == null)
                    col.SetValue(PreForcedVisibilityProperty, col.Visibility);

                col.Visibility = forced;
            }
            else
            {
                // 強制解除：退避した状態に戻す。退避値がなければ Visible をデフォルトとする。
                var before = (Visibility?)(col.GetValue(PreForcedVisibilityProperty));
                col.Visibility = before ?? Visibility.Visible;
                col.SetValue(PreForcedVisibilityProperty, null);
            }
        }



    }
}
