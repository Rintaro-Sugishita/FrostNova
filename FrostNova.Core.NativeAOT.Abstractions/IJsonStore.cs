using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.NativeAOT;
#if NET8_0_OR_GREATER

public  interface IJsonStore<T> 
{

    static abstract T? LoadSettings();

    void SaveSettings();

    static abstract Task<T> LoadSettingsAsync();


    Task SaveSettingsAsync();

    static abstract string GetSavePath();
}

#endif
