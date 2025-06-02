using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.TypeToolkit;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridDragRowBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            //this.AssociatedObject.AllowDrop = true;
            this.AssociatedObject.DragOver += this.DataGrid_DragOver;
            this.AssociatedObject.Drop += this.DataGrid_Drop;
            this.AssociatedObject.PreviewMouseLeftButtonDown += this.DataGrid_PreviewMouseLeftButtonDown;

            foreach(var item in this.AssociatedObject.Columns)
            {
               
            }
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

        public IList AllowDragColumnIndexes
        {
            get => (IList)_AllowDragColumnIndexes;
            set
            {
                _AllowDragColumnIndexes = value;
            }
        }
        private IList _AllowDragColumnIndexes = new List<int>();
        //public static readonly DependencyProperty AllowDragColumnIndexesProperty =
        //    DependencyProperty.Register(nameof(AllowDragColumnIndexes), typeof(IList), typeof(DataGridDragRowBehavior), new PropertyMetadata(new List<int>()));


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


            int index = this.GetCurrentRowIndex(e.GetPosition, ArrayExtensionToArray(AllowDragColumnIndexes));
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
            rowIndex = GetCurrentRowIndex(e.GetPosition, ArrayExtensionToArray(AllowDragColumnIndexes));
            if (rowIndex < 0)
                return;
            if (AssociatedObject.SelectionUnit != DataGridSelectionUnit.Cell) AssociatedObject.SelectedIndex = rowIndex;
            var selectedItem = AssociatedObject.Items[rowIndex];


            if (selectedItem == null)
                return;
            DragDropEffects dragdropeffects = DragDropEffects.Move;
            if (DragDrop.DoDragDrop(AssociatedObject, selectedItem, dragdropeffects)
                                != DragDropEffects.None)
            {
               if (AssociatedObject.SelectionUnit != DataGridSelectionUnit.Cell) AssociatedObject.SelectedItem = selectedItem;
            }
        }

        private bool GetMouseTargetRow(Visual theTarget, GetPosition position)
        {
            Rect rect = VisualTreeHelper.GetDescendantBounds(theTarget);
            Point point = position((IInputElement)theTarget);
            return rect.Contains(point);
        }

        private DataGridRow GetRowItem(int index)
        {
            if (AssociatedObject.ItemContainerGenerator.Status
                    != GeneratorStatus.ContainersGenerated)
                return null;
            return AssociatedObject.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;




        }

        private int GetCurrentRowIndex(GetPosition pos, int[] allowedIndexes)
        {
            int curIndex = -1;


            var cell = GetDataGridObject<DataGridCell>(AssociatedObject, pos((IInputElement)AssociatedObject));
            if (cell == null) { return -1; }
            if (cell != null && allowedIndexes.Length > 0)
            {
                if (!allowedIndexes.Contains(cell.Column.DisplayIndex))
                {
                    return -1;
                }
            }

            for (int i = 0; i < AssociatedObject.Items.Count; i++)
            {
                DataGridRow itm = GetRowItem(i);
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

        private int[] ArrayExtensionToArray(IList array)
        {
            if (array == null || array.Count == 0)
            {
                return Array.Empty<int>();
            }

            var list = new List<int>();
            foreach (var item in array)
            {
                if (item is int a && a >= 0)
                {
                    list.Add(a);
                }
                else if (item is string b)
                {
                    var c = ConvertHelper.StrToAny<int>(b);
                    if (c >= 0)
                    {
                        list.Add(c);
                    }
                }

            }

            return list.ToArray();

        }

        private T GetDataGridObject<T>(DataGrid dataGrid, Point point)
        {
            T result = default(T);
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
