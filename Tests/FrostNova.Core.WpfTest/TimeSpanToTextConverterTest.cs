using FrostNova.Core.Wpf.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.WpfTest
{
    public class TimeSpanToTextConverterTest
    {
        [Theory]
        [MemberData(nameof(Test1Data))]
        public void Test1(TimeSpan span, bool isShowSecond, bool isShowDay, string ans)
        {
            Assert.Equal(ans, TimeSpanToTextConverter.Convert(span, isShowSecond, isShowDay));
        }

        public static TheoryData<TimeSpan, bool, bool, string> Test1Data => new()
        {
            { new TimeSpan(10,0,0), false, false , "10:00"},
            { -new TimeSpan(10,0,0), false, false , "-10:00"},
            { new TimeSpan(10,0,0), true, false , "10:00:00"},
            { -new TimeSpan(10,0,0), true, false , "-10:00:00"},
            { new TimeSpan(25,0,0), true, false , "25:00:00"},
            { -new TimeSpan(25,0,0), true, false , "-25:00:00"},
            { new TimeSpan(25,0,0), true, true , "1.01:00:00"},
            { -new TimeSpan(25,0,0), true, true , "-1.01:00:00"},
            { new TimeSpan(25,0,0), false, true , "1.01:00"},
            { -new TimeSpan(25,0,0), false, true , "-1.01:00"},
        };



    }
}
