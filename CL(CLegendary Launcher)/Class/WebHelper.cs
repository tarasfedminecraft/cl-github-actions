using CL_CLegendary_Launcher_.Class;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms; 

namespace CL_CLegendary_Launcher_.Class
{
    public static class WebHelper
    {
        private static readonly List<BrowserInfo> _supportedBrowsers = new List<BrowserInfo>
        {
            new BrowserInfo("chrome.exe", @"Google\Chrome\Application\chrome.exe"),
            new BrowserInfo("brave.exe", @"BraveSoftware\Brave-Browser\Application\brave.exe"),
            new BrowserInfo("msedge.exe", @"Microsoft\Edge\Application\msedge.exe"),
            new BrowserInfo("firefox.exe", @"Mozilla Firefox\firefox.exe"),
            new BrowserInfo("vivaldi.exe", @"Vivaldi\Application\vivaldi.exe"), 
            new BrowserInfo("launcher.exe", @"Opera GX\launcher.exe"), 
            new BrowserInfo("opera.exe", @"Opera\launcher.exe") 
        };

        public static void OpenUrl(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url) || url == "-")
                    return;

                bool launched = false;

                foreach (var browser in _supportedBrowsers)
                {
                    string path = GetBrowserPath(browser);
                    if (!string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            Process.Start(path, url);
                            launched = true;
                            break;
                        }
                        catch {  }
                    }
                }

                if (!launched)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        launched = true;
                    }
                    catch
                    {
                        ShowWebViewWindow(url);
                    }
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                    $"Не вдалося відкрити посилання.\nДеталі: {ex.Message}",
                    "Помилка браузера",Windows.MascotEmotion.Sad);
            }
        }
        private static string GetBrowserPath(BrowserInfo browser)
        {
            var searchFolders = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),     
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),   
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)  
            };

            foreach (var folder in searchFolders)
            {
                if (string.IsNullOrEmpty(folder)) continue;

                string fullPath = Path.Combine(folder, browser.RelativePath);

                if (File.Exists(fullPath))
                    return fullPath;

                string simplePath = Path.Combine(folder, browser.ExeName);
                if (File.Exists(simplePath))
                    return simplePath;
            }

            return null;
        }
        private static void ShowWebViewWindow(string url)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Form webForm = new Form
                    {
                        Text = "Вбудований перегляд (CL Launcher)",
                        Width = 1000,
                        Height = 700,
                        StartPosition = FormStartPosition.CenterScreen,
                        Icon = null 
                    };

                    var webView = new Microsoft.Web.WebView2.WinForms.WebView2
                    {
                        Dock = DockStyle.Fill
                    };

                    webForm.Controls.Add(webView);

                    webForm.Load += async (s, e) =>
                    {
                        try
                        {
                            await webView.EnsureCoreWebView2Async();
                            webView.CoreWebView2.Navigate(url);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Помилка ініціалізації WebView2: {ex.Message}");
                        }
                    };

                    webForm.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Критична помилка відкриття WebView: {ex.Message}");
                }
            });
        }

        private class BrowserInfo
        {
            public string ExeName { get; }
            public string RelativePath { get; }

            public BrowserInfo(string exeName, string relativePath)
            {
                ExeName = exeName;
                RelativePath = relativePath;
            }
        }
    }
}