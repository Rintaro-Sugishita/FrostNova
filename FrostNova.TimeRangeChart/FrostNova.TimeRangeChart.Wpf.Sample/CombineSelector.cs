using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.TimeRangeChart.Wpf.Sample;

public class CombineSelector : DataTemplateSelector
{
    public DataTemplate NormalTemplate { get; set; } = default!;
    public DataTemplate AnotherTemplate { get; set; } = default!;


    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is SampleModel sample)
        {
if (sample.CanStack)
            {
                return NormalTemplate;
            }
            else
            {
                return AnotherTemplate;
            }
        }
        return NormalTemplate;
    }

}
