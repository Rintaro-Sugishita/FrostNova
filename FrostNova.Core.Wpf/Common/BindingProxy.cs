using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Common
{
    public class BindingProxy : Freezable
    {

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        /// <summary>
        /// string
        /// </summary>
        public object Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }
        public static DependencyProperty DataProperty = DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));


    }
}
