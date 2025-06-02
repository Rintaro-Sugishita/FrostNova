using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class ItemsControlHeightSyncBehavior : Behavior<ItemsControl>
    {

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;

        }

        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //if (AssociatedObject.Items.Count > 0)
           // {
                //SyncHeight(AssociatedObject, TargetItemsControl);
                AssociatedObject.ItemContainerGenerator.StatusChanged += (s, e) =>
                {
                    if (AssociatedObject.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {

                    }
                };

        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
        }


        private static void SetSync(ItemsControl src, ItemsControl dest)
        {
            var c = src.Items.Count;

            for (int i = 0; i < c; i++)
            {
                var item = dest.Items[i];

                var container = (ContentPresenter)src.ItemContainerGenerator.ContainerFromItem(item);
                var b = dest.Items[i];
                var row2 = dest.ItemContainerGenerator.ContainerFromItem(b) as ContentPresenter;
                if (container != null)
                {
                    container.LayoutUpdated += (s2, e2) =>
                    {
                        var height = container.ActualHeight;
                        // → この高さを別のコントロールに反映

                        if (row2 != null)
                            row2.Height = height;

                    };
                }


            }

        }

        public ItemsControl TargetItemsControl
        {
            get { return (ItemsControl)GetValue(TargetItemsControlProperty); }
            set { SetValue(TargetItemsControlProperty, value); }
        }
        public static DependencyProperty TargetItemsControlProperty = DependencyProperty.Register(nameof(TargetItemsControl), typeof(ItemsControl), typeof(ItemsControlHeightSyncBehavior), new PropertyMetadata(null, OnTargetChanged));

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var newItemsControl = e.NewValue as ItemsControl;
            var b = d as ItemsControlHeightSyncBehavior;
            if (newItemsControl != null && b != null)
            {
                SetSync(b.AssociatedObject, newItemsControl);

            }
        }
    }
}
