using FrostNova.Core.Common;
using FrostNova.Core.Wpf.Messages;
using FrostNova.Core.Wpf.ViewServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.ViewModelBases
{
    public abstract class ViewModelBase : BindableBase
    {

        public IWindowService? WindowService { get; set; }

        public IWpfMessageBox? MessageBox { get; set; }

        public ViewModelBase()
        {
            OnCreated();
        }

        private void OnCreated()
        {
            Initialize();

            bool isDesignMode = DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
            if (isDesignMode)
            {
                InitializeOnDesign();
            }
            else
            {
                InitializeOnExecute();
            }
        }

        protected virtual void InitializeOnDesign()
        {

        }

        protected virtual void Initialize()
        {

        }

        protected virtual void InitializeOnExecute()
        {
        }


    }
}
