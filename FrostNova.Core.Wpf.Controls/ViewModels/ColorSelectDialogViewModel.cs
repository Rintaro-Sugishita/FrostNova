using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Common;
using FrostNova.Core.JsonData;
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


        public string? Path { get; set; } = null;

        public ColorSelectDialogViewModel()
        {




        }


        protected override void OnLoaded()
        {
            base.OnLoaded();

            //load
            if (RecentlyUsedList.Count  == 0)
            {
                var path = GetSavePath();
                RecentlyUsedList = JsonDataHelper.LoadFromFile<NotifyCollection<Color>>(path) ?? new NotifyCollection<Color>();
            }

        }


        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            //save
            var path = GetSavePath();
            JsonDataHelper.SaveToFile(GetSavePath(), RecentlyUsedList, true);


        }

        protected override void Close()
        {

            base.Close();
        }

        protected override bool OnOkClosing()
        {
            return base.OnOkClosing();

        }


        private string GetSavePath()
        {

            if (Path == null)
            {

                var folder = FrostNova.Core.PathHelpers.PathHelper.GetUserDataPath(Process.GetCurrentProcess().ProcessName);
                var fullPath = System.IO.Path.Combine(folder, "colorPaletteData.json");
                return fullPath;
            }
            else
            {
                return Path;
            }
        }


    }
}
