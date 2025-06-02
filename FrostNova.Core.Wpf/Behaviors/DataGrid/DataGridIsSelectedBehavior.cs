using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Behaviors
{
    [Obsolete("Use DataGridUsualBehavior")]
    public class DataGridIsSelectedBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            SelectedItems = new List<object>();
            AssociatedObject.SelectionChanged += DataGrid_SelectionChanged;

        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectionChanged -= DataGrid_SelectionChanged;
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = (DataGrid)sender;


            //foreach (PrismCollectionItem addedItem in e.AddedItems)
            //{
            //    addedItem.IsSelected = true;
            //    SelectedItems.Add(addedItem);
            //}
            //foreach (PrismCollectionItem removedItem in e.RemovedItems)
            //{
            //    removedItem.IsSelected = false;
            //    SelectedItems.Remove(removedItem);
            //}

            //SelectedItemsCount += e.AddedItems.Count - e.RemovedItems.Count;

            //SelectedItems = grid.SelectedItems;
            //SelectedItemsCount = SelectedItems.Count;
            //foreach (var item in grid.Items)
            //{
            //    if (item is PrismCollectionItem colItem)
            //    {

            //        colItem.IsSelected = SelectedItems.Cast<PrismCollectionItem>().Where(x => x == colItem).Any();
            //    }
            //}

            SelectedItems = grid.SelectedItems;
            SelectedItemsCount = SelectedItems.Count;
            //foreach (var item in grid.Items)
            //{
            //    if (item is IPrismCollectionItem colItem)
            //    {

            //        colItem.IsSelected = SelectedItems.Cast<IPrismCollectionItem>().Where(x => x == colItem).Any();
            //    }
            //}

            if (Command?.CanExecute(null) == true)
            {
                Command?.Execute(SelectedItems);
            }

        }



        public int SelectedItemsCount
        {
            get { return (int)GetValue(SelectedItemsCountProperty); }
            set { SetValue(SelectedItemsCountProperty, value); }
        }
        public static readonly DependencyProperty SelectedItemsCountProperty =
            DependencyProperty.Register(nameof(SelectedItemsCount), typeof(int), typeof(DataGridIsSelectedBehavior), new PropertyMetadata(null));

        /// <summary>
        /// 選択されたアイテムリスト
        /// </summary>
        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(DataGridIsSelectedBehavior), new PropertyMetadata(null));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(DataGridIsSelectedBehavior), new PropertyMetadata(null));




    }
}
