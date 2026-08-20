using CommunityToolkit.Mvvm.ComponentModel;

namespace FrostNova.Scheduling.Sample;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private ScenarioAViewModel _scenarioA = new();
    [ObservableProperty] private ScenarioBViewModel _scenarioB = new();
    [ObservableProperty] private ScenarioCViewModel _scenarioC = new();
}
