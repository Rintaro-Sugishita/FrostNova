using CommunityToolkit.Mvvm.Input;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class ListBoxDragRowBehavior : Behavior<ListBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            //this.AssociatedObject.AllowDrop = true;
            this.AssociatedObject.DragOver += this.DataGrid_DragOver;
            this.AssociatedObject.Drop += this.DataGrid_Drop;
            this.AssociatedObject.PreviewMouseLeftButtonDown += this.DataGrid_PreviewMouseLeftButtonDown;

        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.DragOver -= this.DataGrid_DragOver;
            this.AssociatedObject.Drop -= this.DataGrid_Drop;
            this.AssociatedObject.PreviewMouseLeftButtonDown -= this.DataGrid_PreviewMouseLeftButtonDown;
            //this.AssociatedObject.AllowDrop = false;

        }

        public delegate Point GetPosition(IInputElement element);
        private int rowIndex = -1;


        public RelayCommand<DragMoveEventArgs> OnDragMove
        {
            get => (RelayCommand<DragMoveEventArgs>)GetValue(OnDragMoveProperty);
            set
            {
                SetValue(OnDragMoveProperty, value);

            }
        }
        public static readonly DependencyProperty OnDragMoveProperty =
            DependencyProperty.Register(nameof(OnDragMove), typeof(RelayCommand<DragMoveEventArgs>), typeof(DataGridDragRowBehavior));


        void DataGrid_Drop(object sender, DragEventArgs e)
        {
            if (rowIndex < 0)
                return;


            int index = this.GetCurrentRowIndex(e.GetPosition);
            if (index < 0)
                return;
            if (index == rowIndex)
                return;

            var collection = AssociatedObject.ItemsSource as System.Collections.IList;
            if (collection == null) { return; }

            var changedProduct = collection[rowIndex];
            if (changedProduct == null) { return; }
            collection.RemoveAt(rowIndex);
            if (index > collection.Count - 1)
            {
                collection.Add(changedProduct);
            }
            else
            {
                collection.Insert(index, changedProduct);
            }

            var dragArgs = new DragMoveEventArgs(index, rowIndex, changedProduct);
            if ((OnDragMove?.CanExecute(dragArgs) ?? true) == true)
            {
                OnDragMove?.Execute(dragArgs);
            }

        }

        void DataGrid_DragOver(object sender, DragEventArgs e)
        {
            if (rowIndex < 0)
                return;



        }

        void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            rowIndex = GetCurrentRowIndex(e.GetPosition);
            if (rowIndex < 0)
                return;

            //クリックしたのがボタンだった場合、この処理はしない
            var clickedElement = e.OriginalSource as DependencyObject;
            if (clickedElement != null)
            {
                var button = FindParent<Button>(clickedElement);
                if (button != null) { return; }
            }


            AssociatedObject.SelectedIndex = rowIndex;
            var selectedItem = AssociatedObject.Items[rowIndex];

            if (selectedItem == null)
                return;
            DragDropEffects dragDropEffects = DragDropEffects.Move;
            if (DragDrop.DoDragDrop(AssociatedObject, selectedItem, dragDropEffects)
                                != DragDropEffects.None)
            {
                AssociatedObject.SelectedItem = selectedItem;
            }
        }

        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }

        private bool GetMouseTargetRow(Visual theTarget, GetPosition position)
        {
            Rect rect = VisualTreeHelper.GetDescendantBounds(theTarget);
            Point point = position((IInputElement)theTarget);
            return rect.Contains(point);
        }

        private ListBoxItem? GetRowItem(int index)
        {
            if (AssociatedObject.ItemContainerGenerator.Status
                    != GeneratorStatus.ContainersGenerated)
                return null;
            return AssociatedObject.ItemContainerGenerator.ContainerFromIndex(index)
                                                            as ListBoxItem;




        }

        private int GetCurrentRowIndex(GetPosition pos)
        {
            int curIndex = -1;


            var cell = GetListBoxObject<ListBoxItem>(AssociatedObject, pos((IInputElement)AssociatedObject));

            for (int i = 0; i < AssociatedObject.Items.Count; i++)
            {
                ListBoxItem? itm = GetRowItem(i);
                if (itm != null)
                {
                    if (GetMouseTargetRow(itm, pos))
                    {
                        curIndex = i;




                        break;
                    }
                }
            }
            return curIndex;
        }

        //private int[] ArrayExtensionToArray()
        //{
        //    var list = new List<int>();
        //    foreach (var item in array)
        //    {
        //        if (item is int a && a >= 0)
        //        {
        //            list.Add(a);
        //        }
        //        else if (item is string b)
        //        {
        //            var c = ConvFunc.StrToAny<int>(b);
        //            if (c >= 0)
        //            {
        //                list.Add(c);
        //            }
        //        }

        //    }

        //    return list.ToArray();

        //}

        private T? GetListBoxObject<T>(ListBox dataGrid, Point point)
        {
            T? result = default(T);
            var hitResultTest = VisualTreeHelper.HitTest(dataGrid, point);
            if (hitResultTest != null)
            {
                var visualHit = hitResultTest.VisualHit;
                while (visualHit != null)
                {
                    if (visualHit is T)
                    {
                        result = (T)(object)visualHit;
                        break;
                    }
                    visualHit = VisualTreeHelper.GetParent(visualHit);
                }
            }
            return result;
        }
    }
}
