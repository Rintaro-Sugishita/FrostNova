using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace FrostNova.Core.Wpf.VisualHelpers
{
    public static class GeneralHelper
    {

        #region find visual child
        /// <summary>
        /// ビジュアルツリーを再帰的に探索して最初の子要素を返します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static T? FindVisualChild<T>(this DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    yield return typedChild;

                foreach (var grandChild in FindVisualChildren<T>(child))
                    yield return grandChild;
            }
        }

        public static T? FindAncestor<T>(this DependencyObject? child) where T : DependencyObject
        {
            if (child == null) return null;

            DependencyObject current = child;

            while (true)
            {
                DependencyObject? parent = null;

                // まず Visual/Visual3D ツリーを優先して探す
                if (current is Visual || current is Visual3D)
                {
                    parent = VisualTreeHelper.GetParent(current);
                }
                else
                {
                    // Visual で取れない（ContentElement 等）の場合は Logical ツリーを試す
                    parent = LogicalTreeHelper.GetParent(current);
                }

                if (parent == null)
                {
                    // どちらでも取れなかったら終了
                    return null;
                }

                if (parent is T tParent)
                {
                    return tParent;
                }

                current = parent;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="callback"></param>
        public static void ResolveChildWhenLoaded<T>(this FrameworkElement? parent, Action<T> callback) where T : FrameworkElement
        {
            if (parent == null) return;


            if (parent is T)
            {
                callback((T)parent);
                return;
            }

            // 1. すでにビジュアルツリーにあるか試行
            var found = FindVisualChild<T>(parent);
            if (found != null)
            {
                callback(found);
                return;
            }

            // 2. なければ Loaded を待機（一度だけ実行）
            RoutedEventHandler? handler = null;
            handler = (s, e) =>
            {
                parent.Loaded -= handler;
                var reFound = FindVisualChild<T>(parent);
                if (reFound != null)
                {
                    callback(reFound);
                }
            };
            parent.Loaded += handler;
        }

        #endregion

        #region point

        /// <summary>
        /// Determines whether the mouse pointer is currently positioned within the bounds of the specified <see
        /// cref="FrameworkElement"/>.
        /// </summary>
        /// <remarks>This method uses screen coordinates to determine the mouse position relative to the
        /// specified element. If the element is not currently associated with a window, the method returns <see
        /// langword="false"/>.</remarks>
        /// <param name="element">The <see cref="FrameworkElement"/> to test for mouse pointer containment. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the mouse pointer is within the bounds of <paramref name="element"/>; otherwise,
        /// <see langword="false"/>.</returns>
        public static bool IsInMousePoint(FrameworkElement element)
        {
            var ownerWindow = Window.GetWindow(element);
            if (ownerWindow == null) return false;
            // スクリーン座標を取得（WPF方式）
            Point screenPoint = GetMouseScreenPosition(ownerWindow);

            // control内のローカル座標に変換
            Point localPoint = element.PointFromScreen(screenPoint);

            return IsPointInsideAssociatedObject(element, localPoint);

        }

        /// <summary>
        /// Gets the current position of the mouse pointer in screen coordinates relative to the specified window.
        /// </summary>
        /// <param name="owner">The window relative to which the mouse position is calculated. If <see langword="null"/>, the application's
        /// main window is used.</param>
        /// <returns>A <see cref="Point"/> representing the mouse pointer's position in screen coordinates.</returns>
        public static Point GetMouseScreenPosition(Window? owner)
        {
            var window = owner ?? Application.Current.MainWindow;

            // Get mouse position relative to the entire screen (using WPF only)            
            var position = Mouse.GetPosition(window);
            return window.PointToScreen(position);
        }

        /// <summary>
        /// Determines whether the specified point is within the bounds of the given <see cref="FrameworkElement"/>.
        /// </summary>
        /// <remarks>The containment check uses the current <see cref="FrameworkElement.ActualWidth"/> and
        /// <see cref="FrameworkElement.ActualHeight"/> properties. The point is considered inside if its X and Y
        /// coordinates are greater than or equal to zero and less than the element's actual width and height,
        /// respectively.</remarks>
        /// <param name="element">The <see cref="FrameworkElement"/> whose bounds are used for the containment check. Must not be <c>null</c>.</param>
        /// <param name="point">The point, in the coordinate space of <paramref name="element"/>, to test for containment.</param>
        /// <returns><see langword="true"/> if the point is within the bounds of <paramref name="element"/>; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool IsPointInsideAssociatedObject(FrameworkElement element, Point point)
        {
            return point.X >= 0 && point.Y >= 0 &&
                   point.X < element.ActualWidth &&
                   point.Y < element.ActualHeight;
        }

        #endregion

        #region behaviors

        public static void SetBehavior<T>(DependencyObject d, DependencyPropertyChangedEventArgs e) where T : Behavior, new()
        {
            var isEnable = e.NewValue as bool?;
            if (isEnable != null)
            {
                SetBehavior<T>(d, isEnable.Value);
            }
        }

        public static void SetBehavior<T>(DependencyObject d, bool isEnable) where T : Behavior, new()
        {
            var behavior = new T();
            SetBehavior(d, behavior);
        }

        public static void SetBehavior(DependencyObject d, Behavior behavior, bool allowDuplicateBehaviorType = false)
        {
            SetBehaviors(d, [behavior], allowDuplicateBehaviorType);
        }

        public static void SetBehaviors(DependencyObject d, IEnumerable<Behavior> behaviors, bool allowDuplicateBehaviorType = false)
        {
            if (d is UIElement element)
            {
                var attachingBehaviors = Microsoft.Xaml.Behaviors.Interaction.GetBehaviors(element);

                behaviors.ToList().ForEach(behavior =>
                    {
                        // 既にアタッチ済みか確認
                        if (!allowDuplicateBehaviorType)
                        {
                            var existing = attachingBehaviors.OfType<Behavior>().FirstOrDefault(b => b.GetType() == behavior.GetType());
                            if (existing != null)
                            {
                                //存在してたら削除
                                attachingBehaviors.Remove(existing);
                            }
                        }
                        attachingBehaviors.Add(behavior); // 任意の Behavior
                    });
            }
        }

        public static void ClearBehavior<T>(DependencyObject d) where T : Behavior, new()
        {

            if (d is UIElement element)
            {
                var attachingBehaviors = Microsoft.Xaml.Behaviors.Interaction.GetBehaviors(element);

                var existing = attachingBehaviors.OfType<Behavior>().FirstOrDefault(b => b.GetType() == typeof(T));
                if (existing != null)
                {
                    //存在してたら削除
                    attachingBehaviors.Remove(existing);
                }
            }
        }

        #endregion

    }
}
