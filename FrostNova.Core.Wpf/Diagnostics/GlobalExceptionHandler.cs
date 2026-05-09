using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Diagnostics;

public static class GlobalExceptionHandler
{
    private const string LogFolderName = "log";

    /// <summary>
    /// Registers exception handling events and prepares the log directory.
    /// </summary>
    public static void Initialize()
    {
        Application.Current.DispatcherUnhandledException += (s, e) =>
        {
            HandleException(e.Exception, "UI Dispatcher");
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            HandleException(e.Exception, "Task Scheduler");
            e.SetObserved();
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ee)
                HandleException(ee, "AppDomain");
        };
    }

    /// <summary>
    /// Handles the exception by logging to a unique file and notifying the user.
    /// </summary>
    private static void HandleException(Exception ex, string source)
    {
        if (ex == null) return;

        try
        {
            // ログ内容の構築
            var logContent = BuildLogMessage(ex, source);

            // ファイルへの書き出し（1例外 1ファイル）
            string logFilePath = SaveLogToFile(logContent);

            // ユーザーへの通知
            ShowErrorMessage(ex, logFilePath);
        }
        catch (Exception fatalEx)
        {
            // ログ出力自体が失敗した場合の最終手段
            MessageBox.Show($"Failed to write the log file: {fatalEx.Message}", "Fatal Error");
        }
    }

    /// <summary>
    /// Builds a formatted log message string.
    /// </summary>
    private static string BuildLogMessage(Exception ex, string source)
    {
        string appPath = Environment.ProcessPath ?? string.Empty;

        string appVersion = "Unknown";
        if (!string.IsNullOrEmpty(appPath))
        {
            try
            {
                // ファイルのプロパティからバージョン情報を取得
                var versionInfo = FileVersionInfo.GetVersionInfo(appPath);
                appVersion = versionInfo.FileVersion ?? "Unknown";
            }
            catch
            {
                // フォールバック: アセンブリの情報から取得
                var assembly = Assembly.GetEntryAssembly();
                appVersion = assembly?.GetName().Version?.ToString() ?? "Unknown";
            }
        }        // バージョン情報の取得（AssemblyVersionではなくFileVersionが実用的です）

        var sb = new StringBuilder();
        sb.AppendLine("==================================================");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"App Path:  {appPath}");
        sb.AppendLine($"Version:   {appVersion}");
        sb.AppendLine($"Source:    {source}");
        sb.AppendLine($"Exception: {ex.GetType().FullName}");
        sb.AppendLine($"Message:   {ex.Message}");
        sb.AppendLine("==================================================");
        sb.AppendLine("StackTrace:");
        sb.AppendLine(ex.StackTrace);

        var inner = ex.InnerException;
        while (inner != null)
        {
            sb.AppendLine("\n[Inner Exception]");
            sb.AppendLine($"Type:    {inner.GetType().FullName}");
            sb.AppendLine($"Message: {inner.Message}");
            sb.AppendLine(inner.StackTrace);
            inner = inner.InnerException;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Saves the log content to a new file in the log directory.
    /// </summary>
    private static string SaveLogToFile(string content)
    {
        // 実行ファイルのディレクトリを取得
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string logDir = Path.Combine(baseDir, LogFolderName);

        // logフォルダがなければ作成
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        // ファイル名の生成 (yyyyMMdd_HHmmss_Guid.log)
        string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.log";
        string filePath = Path.Combine(logDir, fileName);

        File.WriteAllText(filePath, content, Encoding.UTF8);
        return filePath;
    }

    /// <summary>
    /// Displays an error dialog to the user.
    /// </summary>
    private static void ShowErrorMessage(Exception ex, string logPath)
    {
        string message = "An unexpected error has occurred.\n\n" +
                             $"Error: {ex.Message}\n\n" +
                             $"For more details, please check the log file at:\n{logPath}";
        MessageBox.Show(message, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
