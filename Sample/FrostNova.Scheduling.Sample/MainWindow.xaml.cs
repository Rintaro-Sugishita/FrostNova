using FrostNova.TimeRangeChart.Wpf;
using System.Windows;

namespace FrostNova.Scheduling.Sample;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel Vm => (MainWindowViewModel)DataContext;

    private void ChartA_TaskDragEnd(object sender, DragDropRangeEventArgs e)
        => Vm.ScenarioA.RecalcFooter();

    private void ChartB_TaskDragEnd(object sender, DragDropRangeEventArgs e)
        => Vm.ScenarioB.RecalcFooter();

    private void ChartC_TaskDragEnd(object sender, DragDropRangeEventArgs e)
        => Vm.ScenarioC.RecalcFooter();
}
