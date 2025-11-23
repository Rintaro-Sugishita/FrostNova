using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using FrostNova.Core.Wpf.Common;
using FrostNova.Core.Wpf.AttachedProperties;

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
            if (ClearTarget == null) return;

            var clearValue = TextBoxEx.GetClearValue(ClearTarget);


            var textBinding = ClearTarget.GetBindingExpression(TextBox.TextProperty);
            if (clearValue == null)
            {
                if (textBinding == null)
                {
                    ClearTarget.Text = string.Empty;
                }
                else
                {
                    //object value = textBinding.ParentBinding .sourc;
                    var sourceType = BindingResolveHelper.GetBindingSourceType(textBinding);
                    if (sourceType == null)
                    {
                        ClearTarget.Text = string.Empty;
                        textBinding.UpdateSource();
                    }
                    else
                    {
                        var defaultValue = BindingResolveHelper.GetDefaultValueForType(sourceType);
                        ClearTarget.Text = defaultValue?.ToString() ?? "";
                        textBinding.UpdateSource();
                    }
                }
            }
            else
            {

                ClearTarget.Text = clearValue?.ToString() ?? "";
                textBinding?.UpdateSource();
            }

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
