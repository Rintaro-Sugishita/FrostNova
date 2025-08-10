using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Threading;

namespace FrostNova.WindowEventPickers
{
    public class ActiveWindowChangedEventPicker
    {
        

        //public delegate void ActiveWindowChanged(object? sender, WindowEventArgs e);

        //private static ActiveWindowChanged? activeWindowChangedHandler;

        private static Action<WindowEventArgs>? ActiveWindowChangedAction=null;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        // プロセス名を取得するための関数
        private static Process GetProcess(IntPtr hWnd)
        {
            uint processId;
            GetWindowThreadProcessId(hWnd, out processId);
            Process process = Process.GetProcessById((int)processId);
            return process;
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        static IntPtr currentForegroundWindow = IntPtr.Zero;

        static string lastWindowTitle = "";
        static Process? lastProcessName = null;



        // ウインドウ情報を表示するメソッド
        public static void ShowWindowInfo()
        {
            try
            {
                IntPtr currentWindow = GetForegroundWindow();
                StringBuilder titleBuilder = new StringBuilder(256);
                GetWindowText(currentWindow, titleBuilder, titleBuilder.Capacity);
                string currentWindowTitle = titleBuilder.ToString().Replace("\0","");

                // タイトルが前回と変わった場合にのみ表示
                if (currentWindow != currentForegroundWindow || currentWindowTitle != lastWindowTitle)
                {
                    Process currentProcess = GetProcess(currentWindow);
                    if (currentProcess.ProcessName != lastProcessName?.ProcessName || currentWindowTitle != lastWindowTitle)
                    {

                        //string url = "";
                        //switch (lastProcessName)
                        //{
                        //    case "chrome":
                        //    case "msedge":


                        //        url = GetChromeCurrentURL(currentProcess);
                        //        break;
                        //    case "firefox":
                        //        url = GetFirefoxCurrentURL();
                        //        break;
                        //    default:
                        //        break;
                        //}

                        //activeWindowChangedHandler?.Invoke(null, new WindowEventArgs(currentProcess.ProcessName, currentWindowTitle, url));
                       
                        ActiveWindowChangedAction?.Invoke(new WindowEventArgs(currentProcess, currentWindowTitle, lastProcessName, lastWindowTitle));
                        lastWindowTitle = currentWindowTitle;
                        lastProcessName = currentProcess;
                    }
                }

                currentForegroundWindow = currentWindow;

            }
            catch(Exception ex) 
            {
                var ex2 = new Exception("Exception throwed when specifing a active window..",ex);                
            }

        }

        private static DispatcherTimer _timer = new DispatcherTimer();




        public static void SetHook(Action<WindowEventArgs> handler, int eventSpanMilisecond)
        {
            //activeWindowChangedHandler += handler;
            ActiveWindowChangedAction = handler;
            _timer.Tick += _timer_Tick;

            _timer.Interval = TimeSpan.FromMilliseconds(eventSpanMilisecond);

            _timer.Start();

        }

        public static void UnHook()
        {
            _timer.Stop();
        }

        private static bool doingEvent = false;


        private static void _timer_Tick(object? sender, EventArgs e)
        {
            if (!doingEvent)
            {
                try
                {
                    doingEvent = true;
                    ShowWindowInfo();
                }
                finally { doingEvent = false; }

            }
            //ShowWindowInfo();
        }

        //public static string GetChromeCurrentURL(Process process)
        //{
        //    // ウィンドウのハンドルを取得
        //    IntPtr handle = process.MainWindowHandle;
        //    AutomationElement rootElement = AutomationElement.FromHandle(handle);

        //    // タブのコンテナを特定 (ChromeのUI Automation ツリーは比較的複雑です)
        //    AutomationElement tabContainer;

        //    if (process.ProcessName == "chrome")
        //    {
        //        tabContainer = rootElement.FindFirst(
        //                        TreeScope.Descendants,
        //                        new PropertyCondition(AutomationElement.NameProperty, "タブ"));

        //    }
        //    else if (process.ProcessName == "msedge")
        //    {
        //        tabContainer = rootElement.FindFirst(
        //            TreeScope.Descendants,
        //            new PropertyCondition(AutomationElement.AutomationIdProperty, "TabsToolbar"));

        //    }
        //    else
        //    {
        //        return null;
        //    }

        //    if (tabContainer != null)
        //    {
        //        // タブ内の要素を特定 (タブ内にはタイトルとURLが含まれています)
        //        AutomationElementCollection tabs = tabContainer.FindAll(
        //            TreeScope.Descendants,
        //            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));

        //        if (tabs != null && tabs.Count > 0)
        //        {
        //            // 現在アクティブなタブを特定
        //            AutomationElement activeTab = tabs.Cast<AutomationElement>()
        //                .FirstOrDefault(tab => tab.Current.IsEnabled && tab.Current.IsKeyboardFocusable);

        //            if (activeTab != null)
        //            {
        //                // タブのタイトルとURLを取得
        //                AutomationElement tabTitleElement = activeTab.FindFirst(
        //                    TreeScope.Descendants,
        //                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));
        //                AutomationElement tabURLBarElement = activeTab.FindFirst(
        //                    TreeScope.Descendants,
        //                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));

        //                string title = tabTitleElement?.Current.Name ?? "";
        //                string url = tabURLBarElement?.Current.Name ?? "";

        //                // タイトルとURLを出力 (デバッグ目的)
        //                Console.WriteLine("タイトル: " + title);
        //                Console.WriteLine("URL: " + url);

        //                return url;
        //            }
        //        }
        //    }

        //    // Chromeが見つからない場合やタブが見つからない場合にnullを返す
        //    return null;
        //}





        //public static string GetFirefoxCurrentURL()
        //{
        //    string firefoxProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mozilla\\Firefox\\Profiles\\";

        //    // プロファイルフォルダのパスを特定 (フォルダ内にはランダムな名前のフォルダがあります)
        //    string[] profileFolders = Directory.GetDirectories(firefoxProfilePath, "*.*", SearchOption.TopDirectoryOnly);

        //    if (profileFolders.Length > 0)
        //    {
        //        // 最初のプロファイルフォルダを使用 (Firefoxプロファイルは複数ある場合があるため)
        //        string sessionStorePath = Path.Combine(profileFolders[0], "sessionstore.js");

        //        // sessionstore.jsファイルが存在するか確認
        //        if (File.Exists(sessionStorePath))
        //        {
        //            try
        //            {
        //                // ファイルを読み込んで内容を解析
        //                string sessionData = File.ReadAllText(sessionStorePath);

        //                // 正規表現を使用して現在のURLを抽出
        //                Match match = Regex.Match(sessionData, @"""url"":""([^""]+)""");
        //                if (match.Success)
        //                {
        //                    string url = match.Groups[1].Value;
        //                    return url;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                // エラー処理
        //                Console.WriteLine("エラー: " + ex.Message);
        //            }
        //        }
        //    }

        //    // Firefoxのプロファイルフォルダが見つからない場合やsessionstore.jsが見つからない場合にnullを返す
        //    return null;
        //}

    }

}
