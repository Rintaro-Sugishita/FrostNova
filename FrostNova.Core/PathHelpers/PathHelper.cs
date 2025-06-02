using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.PathHelpers
{
    public static class PathHelper
    {
        public static string GetAbsolutePath(string relativePath, string? basePath = null)
        {
            if (IsAbsolutePath(relativePath))
            {
                return relativePath;
            }

            Uri baseUrl;
            if (basePath != null)
            {
                if (basePath.Last() != '\\')
                {
                    basePath += '\\';
                }
                baseUrl = new Uri(basePath.Replace("%", "%25"));
            }
            else
            {
                baseUrl = new Uri(AppDomain.CurrentDomain.BaseDirectory.Replace("%", "%25"));
            }

            string relativePathLocal = relativePath.Replace("%", "%25");

            Uri uri = new Uri(baseUrl, relativePathLocal);

            return uri.LocalPath;
        }


        public static bool IsAbsolutePath(string relativePath)
        {

            if (string.IsNullOrEmpty(relativePath)) { return false; }

            return (System.IO.Path.GetFullPath(relativePath) == relativePath);
        }

        public static void CopyFile(string filePath, string directoryName, bool isOverwrite)
        {
            var a = System.IO.Path.GetFileName(filePath);
            var b = System.IO.Path.Combine(directoryName, a);

            System.IO.File.Copy(filePath, b, isOverwrite);
        }

        /// <summary>
        /// Get default application path of the argument file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetDefaultApplicationPath(string path)
        {
            System.Text.StringBuilder exePath = new System.Text.StringBuilder(255);
            //fileNameに関連付けられた実行ファイルのパスを取得する
            if (FindExecutable(path, null, exePath) <= 32)
            {
                //失敗した時
                return string.Empty;
            }
            var b = exePath.ToString();
            return b;

        }

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern int FindExecutable(string lpFile, string? lpDirectory, System.Text.StringBuilder lpResult);




        public static string GetExecutingPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetUserDataPath(string? appName = null)
        {
            string localAppName;
            if (string.IsNullOrEmpty(appName))
            {
                localAppName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            }
            else
            {
                localAppName = appName;
            }

            string appPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return System.IO.Path.Combine(appPath, localAppName);
        }


    }
}
