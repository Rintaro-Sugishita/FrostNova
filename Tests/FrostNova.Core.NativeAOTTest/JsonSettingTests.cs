using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FrostNova.Core.NativeAOTTest;

public  class JsonSettingTests
{


    [Fact]
    public void GenerateSettingTest()
    {
        JsonSettingModel model1 = new JsonSettingModel();

       var json= model1.ToJson();
        var model3 = JsonSettingModel.FromJson(json) ?? new JsonSettingModel();
        Assert.Equal("aaaaa", model3.Value);

        model1.ToJson();

       var model2 = JsonSettingModel.FromJson("{ \"Name\" : \"name\"}") ?? new JsonSettingModel();
        Assert.Equal("bababa", model2.Value );
        
    }

}
