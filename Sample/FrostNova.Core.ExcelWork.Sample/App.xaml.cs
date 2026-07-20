using System.Configuration;
using System.Data;
using System.Windows;
using OfficeOpenXml;

namespace FrostNova.Core.ExcelWork.Sample;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // EPPlus 8 requires the consuming application to declare a license once, before any
        // Excel operation runs. FrostNova.Core.ExcelWork intentionally does not do this for you.
        ExcelPackage.License.SetNonCommercialPersonal("FrostNova Sample");

        base.OnStartup(e);
    }
}

