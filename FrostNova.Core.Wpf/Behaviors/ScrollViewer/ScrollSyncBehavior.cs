using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class ScrollSyncBehavior : Behavior<ScrollViewer>
    {

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.ScrollChanged += AssociatedObject_ScrollChanged;
        }

        private void AssociatedObject_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            if (ReflectViewer != null)
            {

                if (IsVerticalSync && ReflectViewer.VerticalOffset != AssociatedObject.VerticalOffset)
                {
                    ReflectViewer.ScrollToVerticalOffset(AssociatedObject.VerticalOffset);
                }

                if (IsHorizontalSync && ReflectViewer.HorizontalOffset != AssociatedObject.HorizontalOffset)
                {
                    ReflectViewer.ScrollToHorizontalOffset(AssociatedObject.HorizontalOffset);
                }
            }
        }

        /// <summary>
        /// string
        /// </summary>
        public bool IsVerticalSync
        {
            get { return (bool)GetValue(IsVerticalSyncProperty); }
            set { SetValue(IsVerticalSyncProperty, value); }
        }
        public static DependencyProperty IsVerticalSyncProperty = DependencyProperty.Register(nameof(IsVerticalSync), typeof(bool), typeof(ScrollSyncBehavior), new PropertyMetadata(false));

        /// <summary>
        /// string
        /// </summary>
        public bool IsHorizontalSync
        {
            get { return (bool)GetValue(IsHorizontalSyncProperty); }
            set { SetValue(IsHorizontalSyncProperty, value); }
        }
        public static DependencyProperty IsHorizontalSyncProperty = DependencyProperty.Register(nameof(IsHorizontalSync), typeof(bool), typeof(ScrollSyncBehavior), new PropertyMetadata(false));



        /// <summary>
        /// string
        /// </summary>
        public ScrollViewer ReflectViewer
        {
            get { return (ScrollViewer)GetValue(ReflectViewerProperty); }
            set { SetValue(ReflectViewerProperty, value); }
        }
        public static DependencyProperty ReflectViewerProperty = DependencyProperty.Register(nameof(ReflectViewer), typeof(ScrollViewer), typeof(ScrollSyncBehavior));

    }
}
