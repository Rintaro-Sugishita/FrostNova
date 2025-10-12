using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.Core.JsonData;
using FrostNova.Core.TypeToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Common
{

    public abstract class BindableDataModel<T> : BindableBase where T: class, new()
    {
        public abstract string GetSavePath();


        public static T LoadSettings()
        {
            var ss = new T();
            if (ss is AbstractDataModel<T>)
            {
                return ((AbstractDataModel<T>)(object)ss).Load();
            }
            else
            {
                return ss;
            }
        }

        public T Load()
        {
            return LoadFromFile(GetSavePath());
        }

        public virtual T LoadFromFile(string filePath)
        {
            string? jsonText = null;

            if (System.IO.File.Exists(filePath))
            {
                jsonText = System.IO.File.ReadAllText(filePath);
            }

            return LoadFromJsonText(jsonText);

        }

        public virtual T LoadFromJsonText(string? text)
        {
           var val = JsonDataHelper.LoadFromJsonText<T>(text) ?? new T();

            DeepCloneHelper.CopyProperty<BindableDataModel<T>>((BindableDataModel<T>)(object)val, this);

            return val;
        }
        public string ToJsonText()
        {
            return System.Text.Json.JsonSerializer.Serialize(this, typeof(T));
        }

        public virtual void Save()
        {
            SaveAs(GetSavePath());
        }
        public virtual void SaveAs(string filePath)
        {
            var isFormat = this.GetType().GetCustomAttribute<JsonFormatAttribute>() != null;

            JsonDataHelper.SaveToFile(filePath, this, typeof(T), isFormat);
        }
    }
}
