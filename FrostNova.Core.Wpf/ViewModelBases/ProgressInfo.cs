using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.Core.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.ViewModelBases
{
    public partial class ProgressInfo : BindableBase
    {


        [ObservableProperty]
        private IProgress<ProgressInfo>? _progress;

        public ProgressInfo(IProgress<ProgressInfo> p)
        {
            _progress = p;
        }

        public ProgressInfo() { }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            Progress?.Report(this);
        }


        [ObservableProperty]
        private double _minimum = 0.0;

        [ObservableProperty]
        private double _maximum = 100.0;

        [ObservableProperty]
        private double _value = 0.0;

        [ObservableProperty]
        private string _title = "";

        [ObservableProperty]
        private string _progressText = "";

        [ObservableProperty]
        private bool _isIndeterminate = true;

        [ObservableProperty]
        private bool _canCancel = true;




    }
}
