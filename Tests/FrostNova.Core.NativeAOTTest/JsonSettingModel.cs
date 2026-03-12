using System;
using System.Collections.Generic;
using System.Text;
using FrostNova.Core.NativeAOT;

namespace FrostNova.Core.NativeAOTTest;

[GenerateJsonSettings]
internal partial class JsonSettingModel
{


    public string Value { get; set; } = "";

    partial void BeforeSerialize()
    {
        if (string.IsNullOrEmpty(Value)) Value = "aaaaa";
    }

    partial void AfterDeserialize()
    {
        if (string.IsNullOrEmpty(Value)) Value = "bababa";
    }


    [JsonSettingPath]
    public static string GetSavePath111()
    {
        return "ss.json";
    }

    //void Aaa()
    //{


    //    AfterDeserialize();
    //    BeforeSerialize();

    //}


}
