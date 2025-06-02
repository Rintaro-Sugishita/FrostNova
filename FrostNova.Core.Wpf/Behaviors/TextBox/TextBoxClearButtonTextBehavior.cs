using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class TextBoxClearButtonTextBehavior : Behavior<Button>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += AssociatedObject_Click;
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Click -= AssociatedObject_Click;
        }

        private void AssociatedObject_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ClearTarget.Text = string.Empty;
        }

        /// <summary>
        /// string
        /// </summary>
        public TextBox ClearTarget
        {
            get { return (TextBox)GetValue(ClearTargetProperty); }
            set { SetValue(ClearTargetProperty, value); }
        }
        public static DependencyProperty ClearTargetProperty = DependencyProperty.Register(nameof(ClearTarget), typeof(TextBox), typeof(TextBoxClearButtonTextBehavior));



    }
}
