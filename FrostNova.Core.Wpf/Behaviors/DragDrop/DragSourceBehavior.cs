using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace FrostNova.Core.Wpf.Behaviors
{

    public class DragSourceBehavior : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            //AssociatedObject.DragOver += DragOver;
            AssociatedObject.MouseMove += AssociatedObject_MouseMove;


        }
        protected override void OnDetaching()
        {
            base.OnDetaching();
            //AssociatedObject.DragOver -= DragOver;
            AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
        }


        /// <summary>
        /// string
        /// </summary>
        public object DragData
        {
            get { return GetValue(DragDataProperty); }
            set { SetValue(DragDataProperty, value); }
        }
        public static DependencyProperty DragDataProperty = DependencyProperty.Register(nameof(DragData), typeof(object), typeof(DragSourceBehavior));

        /// <summary>
        /// string
        /// </summary>
        public object SourceParameter
        {
            get { return (object)GetValue(SourceParameterProperty); }
            set { SetValue(SourceParameterProperty, value); }
        }
        public static DependencyProperty SourceParameterProperty = DependencyProperty.Register(nameof(SourceParameter), typeof(object), typeof(DragSourceBehavior));


        internal void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is UIElement source)
            {

                if (source != null && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    var item = new DragItemData();
                    item.DragData = DragData;
                    item.SourceParameter = SourceParameter;


                    DragDrop.DoDragDrop(source, item, DragDropEffects.Move);
                }
            }
        }

        protected virtual void DragOver(object sender, DragEventArgs e)
        {
            //if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //{
            //    e.Effects = DragDropEffects.Copy;
            //    e.Handled= true;

            //}
            //else
            //{
            //    e.Effects = DragDropEffects.None;
            //    //e.Handled= false;
            //}
        }



    }
}
