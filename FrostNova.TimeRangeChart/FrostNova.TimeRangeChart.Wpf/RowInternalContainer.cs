using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.Wpf;

public class RowInternalContainer : INotifyPropertyChanged
{
    // 開発者が渡してきた本物のデータモデル
    public object RawData { get; }

    private double _height = 40.0;
    public double Height
    {
        get => _height;
        set { _height = value; OnPropertyChanged(); }
    }

    public RowInternalContainer(object rawData) => RawData = rawData;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

}
