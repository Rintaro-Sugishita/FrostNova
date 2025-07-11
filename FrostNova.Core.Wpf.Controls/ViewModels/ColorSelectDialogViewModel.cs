using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Common;
using FrostNova.Core.JsonData;
using FrostNova.Core.PathHelpers;
using FrostNova.Core.Wpf.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Controls.ViewModels
{
    public partial class ColorSelectDialogViewModel : FrostNova.Core.Wpf.ViewModelBases.DialogWindowViewModelBase
    {


        [ObservableProperty]
        Color? _selectedColor = null;

        [ObservableProperty]
        Color? _oldColor = null;


        [ObservableProperty]
        NotifyCollection<Color> _recentlyUsedList = new();

        [ObservableProperty]
        Func<ICollection<Color>>? _SetRecentlyUsedColorsFunc;


        public ColorSelectDialogViewModel()
        {




        }


        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (SetRecentlyUsedColorsFunc == null)
            {
                RecentlyUsedList = new NotifyCollection<Color>(ReadColor());
            }
            else
            {
                RecentlyUsedList = new NotifyCollection<Color>(SetRecentlyUsedColorsFunc.Invoke());
            }

        }

        

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);



        }

        protected override void Close()
        {

            base.Close();
        }

        protected override bool OnOkClosing()
        {
            if (SelectedColor != null)
            {
                if (RecentlyUsedList.Contains(SelectedColor.Value))
                {
                    RecentlyUsedList.Remove(SelectedColor.Value);
                }
                RecentlyUsedList.Insert(0, SelectedColor.Value);
                              

                if (SetRecentlyUsedColorsFunc == null)
                {
                    SaveColor(RecentlyUsedList.Distinct().ToList());
                }
               
            }


            return base.OnOkClosing();

        }


        internal static string GetSavePath()
        {
                var folder = FrostNova.Core.PathHelpers.PathHelper.GetUserDataPath(Process.GetCurrentProcess().ProcessName);
                var fullPath = System.IO.Path.Combine(folder, "colorPaletteData.json");
                return fullPath;
        }
        public static IList<Color> ReadColor()
        {
            var path = GetSavePath();
            if (System.IO.File.Exists(path))
            {
                return System.IO.File.ReadAllLines(path).Select(x => ColorTextConverter.ConvertBack(x)).ToList();
            }
            return Array.Empty<Color>();
        }

        public static void SaveColor(ICollection<Color> colors)
        {
            var path = GetSavePath();
            var folder = System.IO.Path.GetDirectoryName(path)!;
            if (System.IO.Directory.Exists(folder) == false)
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            if (colors.Count > 11)
            {
                System.IO.File.WriteAllLines(path, colors.Select(x => ColorTextConverter.Convert(x)).ToList().Slice(1, 11));
            }
            else
            {
                System.IO.File.WriteAllLines(path, colors.Select(x => ColorTextConverter.Convert(x)));
            }

        }



    }
}
