using FrostNova.Core.TypeToolkit;
using System.Net;

namespace FrostNova.CoreTest
{
    public class UnitTest1
    {
        [Fact]
        public void ParseTest()
        {

            Assert.Equal<int>(111, ConvertHelper.ConvToAny<int>("111"));
            Assert.Equal<int>(0, ConvertHelper.ConvToAny<int>("111.5"));
            
            
            Assert.Equal("111", ConvertHelper.ConvToAny<string>(111));

            Assert.Equal<decimal>(111.5M, ConvertHelper.ConvToAny<decimal>("111.5"));
            
            Assert.Equal<DateTime>(new DateTime(2025, 06,01), ConvertHelper.ConvToAny<DateTime>("2025-06-01"));
            Assert.Equal<TimeSpan>(TimeSpan.FromMinutes(10), ConvertHelper.ConvToAny<TimeSpan>("00:10:00"));
            Assert.Equal<TimeSpan>(TimeSpan.FromHours(10), ConvertHelper.ConvToAny<TimeSpan>("10:00:00"));
            Assert.Equal<TimeSpan>(TimeSpan.FromHours(25), ConvertHelper.ConvToAny<TimeSpan>("1.1:00:00"));
            Assert.True(ConvertHelper.ConvToAny<bool>("true"));
            Assert.True(ConvertHelper.ConvToAny<bool>(1));
            Assert.True(ConvertHelper.ConvToAny<bool>(-1));
            Assert.True(ConvertHelper.ConvToAny<bool>(2));
            Assert.True(ConvertHelper.ConvToAny<bool>(1M));
            Assert.True(ConvertHelper.ConvToAny<bool>(1d));
            Assert.True(ConvertHelper.ConvToAny<bool>(true));
            Assert.False(ConvertHelper.ConvToAny<bool>(false));
            Assert.False(ConvertHelper.ConvToAny<bool>("false"));
            Assert.Equal(new IPAddress([192, 168,0,1]),ConvertHelper.ConvToAny<IPAddress>("192.168.0.1"));






        }
    }
}