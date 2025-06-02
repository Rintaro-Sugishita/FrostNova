using CommunityToolkit.Mvvm.Input;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class FileDropBehavior : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.DragOver += this.DragOver;
            this.AssociatedObject.Drop += this.DragDrop;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.DragOver -= this.DragOver;
            this.AssociatedObject.Drop -= this.DragDrop;
        }

        public RelayCommand<string> OnDropCommand { get => (RelayCommand<string>) GetValue( OnDropCommandProperty);
            set => SetValue(OnDropCommandProperty, value); }

        public static DependencyProperty OnDropCommandProperty = DependencyProperty.Register(nameof(OnDropCommand), typeof(RelayCommand<string>), typeof(UIElement));


        protected virtual void DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;

            }
            else
            {
                e.Effects = DragDropEffects.None;
                //e.Handled= false;
            }
        }

        protected virtual void DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;

            }
            else
            {
                e.Effects = DragDropEffects.None;
                //e.Handled= false;
            }
        }

    }
}
