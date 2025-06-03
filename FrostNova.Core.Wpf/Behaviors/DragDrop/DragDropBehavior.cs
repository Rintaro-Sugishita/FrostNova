using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Behaviors
{

    /*
     ReactiveProperty.WPF
     Microsoft.Xaml.Behaviors.Wpf
      

    xmlns:interactivity="clr-namespace:Reactive.Bindings.Interactivity;assembly=ReactiveProperty.WPF"

     
    <i:Interaction.Behaviors>
        <pb:DragOverBehavior />
    </i:Interaction.Behaviors>
    <i:Interaction.Triggers>
        <i:EventTrigger EventName="Drop">
            <interactivity:EventToReactiveCommand Command="{Binding DropCommand}" />
        </i:EventTrigger>
    </i:Interaction.Triggers>



    public ReactiveCommand<DragEventArgs> DropCommand { get; private set; }
    
    
    DropCommand = new ReactiveCommand<DragEventArgs>();
    DropCommand.Subscribe(OnDrop);

     */
    public class DragDropBehavior : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AllowDrop = true;


            DragDrop.AddDropHandler(AssociatedObject, UIElementDrop);

            //this.AssociatedObject.DragOver += this.DragDrop;
            //this.AssociatedObject.DragEnter += AssociatedObject_DragEnter;

        }

        //private void AssociatedObject_DragEnter(object sender, DragEventArgs e)
        //{

        //}

        //protected virtual void DragDrop(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        e.Effects = DragDropEffects.Copy;
        //        e.Handled = true;

        //    }
        //    else
        //    {
        //        e.Effects = DragDropEffects.None;
        //        //e.Handled= false;
        //    }
        //}

        private void UIElementDrop(object sender, DragEventArgs e)
        {
            UIElement? element = sender as UIElement;
            if (element != null)
            {
                var formats = e.Data.GetFormats();
                if (formats != null && formats.Length > 0)
                {
                    var data = e.Data.GetData(formats[0]);
                    if (data is DragItemData data2)
                    {
                        var param = new DragItemData();
                        param.DragData = data2.DragData;
                        param.SourceParameter = data2.SourceParameter;
                        param.DropParameter = CommandParameter;

                        if (Command?.CanExecute(param) == true)
                        {
                            Command?.Execute(param);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// string
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
        public static DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(DragDropBehavior));

        /// <summary>
        /// string
        /// </summary>
        public object CommandParameter
        {
            get { return (object)GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }
        public static DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(DragDropBehavior));


        protected override void OnDetaching()
        {
            base.OnDetaching();
            //this.AssociatedObject.DragOver -= this.DragDrop;
        }
    }
}
