using System.Windows;
using System.Windows.Navigation;
using FrostNova.Core.TableWork;
using FrostNova.Core.TypeToolkit;

namespace FrostNova.Core.TableWorkTest;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

        var list = new List<Class2>();
        list.AddRange(
            [
                new (){Class1=new(){Val1=1500,Val2=1200,TextVal1="string\",", dateVal=new DateTime(2026,02,23,10,50,56), SpanVal= new TimeSpan(10,50,78), BoolVal=false}},
                new (){Class1=new(){Val1=1,Val2=1200,TextVal1="stringlf\n,", dateVal=new DateTime(2026,02,23,10,50,56), SpanVal= new TimeSpan(10,50,78), BoolVal=false}},
                new (){Class1=new(){Val1=1,Val2=1200,TextVal1="stringtab\t,", dateVal=new DateTime(2026,02,23,10,50,56), SpanVal= new TimeSpan(10,50,78), BoolVal=false}},
                new (){Class1=new(){Val1=1,Val2=1200,TextVal1="string,", dateVal=new DateTime(2026,02,23,10,50,56), SpanVal= new TimeSpan(10,50,78), BoolVal=false}},
                new (){Class1=new(){Val1=1,Val2=1200,TextVal1="0001,", dateVal=new DateTime(2026,02,23,10,50,56), SpanVal= new TimeSpan(10,50,78), BoolVal=false}}

            ]
            );

        var table = new ClassTableObject<Class2>(list);
        table.TargetColumns.AddRange([
                new TableColumn<Class2, int>((x)=> x.Class1.Val1),
                new TableColumn<Class2, decimal>((x)=> x.Class1.Val2),
                new TableColumn<Class2, string>((x)=> x.Class1.TextVal1),
                new TableColumn<Class2, DateTime>((x)=> x.Class1.dateVal),
                new TableColumn<Class2, TimeSpan>((x)=> x.Class1.SpanVal),
                new TableColumn<Class2, bool>((x)=> x.Class1.BoolVal, "boolVal", null, converter:  (x)=>{return x ? "1" : "0"; } , backConverter:(x)=> { return ConvertHelper.ConvToBool(x); } )
            ]);


        FrostNova.Core.TableWork.TableDataProcessor tableCopier = new TableDataProcessor();
        //var html = tableCopier.ToHtml(table, columnBackColor: System.Drawing.Color.DarkCyan, columnForeColor: System.Drawing.Color.White, nullForeColor: System.Drawing.Color.Gray, "<<null>>", false, true);
        var html = TableDataProcessor.ToClipboardTextAsHtml(table, columnBackColor: System.Drawing.Color.DarkCyan, columnForeColor: System.Drawing.Color.White, nullForeColor: System.Drawing.Color.Gray, "<<null>>", false, true);
        // STAスレッドを作って実行
        Exception? ex = null;
        var thread = new Thread(() =>
        {
            try
            {
                Clipboard.SetText(html, TextDataFormat.Html);
            }
            catch (Exception e)
            {
                ex = e;
            }
        });

        thread.SetApartmentState(ApartmentState.STA); // ここが重要
        thread.Start();
        thread.Join();

        if (ex != null) throw ex;

        Console.WriteLine(html);
    }


    class Class1
    {
        public int Val1 { get; set; }
        public decimal Val2 { get; set; }

        public string TextVal1 { get; set; }
        public DateTime dateVal
        {
            get; set;

        }
        public TimeSpan SpanVal
        {
            get; set;
        }
        public bool BoolVal { get; set; }
    }

    class Class2
    {
        public Class1 Class1 { get; set; } = new Class1();
    }



}
