using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridCellClickEventHandleBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.MouseDoubleClick += AssociatedObject_MouseDoubleClick;
            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseRightButtonDown += AssociatedObject_MouseRightButtonDown;
            AssociatedObject.MouseRightButtonUp += AssociatedObject_MouseRightButtonUp;
        }

    
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseDoubleClick -= AssociatedObject_MouseDoubleClick;
            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseRightButtonDown -= AssociatedObject_MouseRightButtonDown;
            AssociatedObject.MouseRightButtonUp -= AssociatedObject_MouseRightButtonUp;
        }


        private void AssociatedObject_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            DataGridCell cell = GetCell(e);
            if (cell != null)
            {
                if (DoubleClickCommand?.CanExecute(cell.DataContext) == true)
                {
                    DoubleClickCommand?.Execute(cell.DataContext);
                }
            }
        }
 

        private void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = GetCell(e);
            if (cell != null)
            {
                if (LeftButtonDownCommand?.CanExecute(cell.DataContext) == true)
                {
                    LeftButtonDownCommand?.Execute(cell.DataContext);
                }
            }
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = GetCell(e);
            if (cell != null)
            {
                if (LeftButtonUpCommand?.CanExecute(cell.DataContext) == true)
                {
                    LeftButtonUpCommand?.Execute(cell.DataContext);
                }
            }
        }

        private void AssociatedObject_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = GetCell(e);
            if (cell != null)
            {
                if (RightButtonDownCommand?.CanExecute(cell.DataContext) == true)
                {
                    RightButtonDownCommand?.Execute(cell.DataContext);
                }
            }
        }

        private void AssociatedObject_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = GetCell(e);
            if (cell != null)
            {
                if (RightButtonUpCommand?.CanExecute(cell.DataContext) == true)
                {
                    RightButtonUpCommand?.Execute(cell.DataContext);
                }
            }
        }

        private DataGridCell GetCell(MouseEventArgs e)
        {

            var elem = e.MouseDevice.DirectlyOver as FrameworkElement;
            if (elem != null)
            {
                DataGridCell cell = elem.Parent as DataGridCell;
                if (cell == null)
                {
                    cell = elem.TemplatedParent as DataGridCell;
                }
                if (cell != null)
                {
                    return cell;
                }
            }
            return null;
        }

        /// <summary>
        /// double click command
        /// </summary>
        public ICommand DoubleClickCommand
        {
            get { return (ICommand)GetValue(DoubleClickCommandProperty); }
            set { SetValue(DoubleClickCommandProperty, value); }
        }
        public static DependencyProperty DoubleClickCommandProperty = DependencyProperty.Register(nameof(DoubleClickCommand), typeof(ICommand), typeof(DataGridCellClickEventHandleBehavior));

        /// <summary>
        /// string
        /// </summary>
        public ICommand LeftButtonDownCommand
        {
            get { return (ICommand)GetValue(LeftButtonDownCommandProperty); }
            set { SetValue(LeftButtonDownCommandProperty, value); }
        }
        public static DependencyProperty LeftButtonDownCommandProperty = DependencyProperty.Register(nameof(LeftButtonDownCommand), typeof(ICommand), typeof(DataGridCellClickEventHandleBehavior));

        /// <summary>
        /// string
        /// </summary>
        public ICommand LeftButtonUpCommand
        {
            get { return (ICommand)GetValue(LeftButtonUpCommandProperty); }
            set { SetValue(LeftButtonUpCommandProperty, value); }
        }
        public static DependencyProperty LeftButtonUpCommandProperty = DependencyProperty.Register(nameof(LeftButtonUpCommand), typeof(ICommand), typeof(DataGridCellClickEventHandleBehavior));

        /// <summary>
        /// string
        /// </summary>
        public ICommand RightButtonDownCommand
        {
            get { return (ICommand)GetValue(RightButtonDownCommandProperty); }
            set { SetValue(RightButtonDownCommandProperty, value); }
        }
        public static DependencyProperty RightButtonDownCommandProperty = DependencyProperty.Register(nameof(RightButtonDownCommand), typeof(ICommand), typeof(DataGridCellClickEventHandleBehavior));

        /// <summary>
        /// string
        /// </summary>
        public ICommand RightButtonUpCommand
        {
            get { return (ICommand)GetValue(RightButtonUpCommandProperty); }
            set { SetValue(RightButtonUpCommandProperty, value); }
        }
        public static DependencyProperty RightButtonUpCommandProperty = DependencyProperty.Register(nameof(RightButtonUpCommand), typeof(ICommand), typeof(DataGridCellClickEventHandleBehavior));



    }
}
