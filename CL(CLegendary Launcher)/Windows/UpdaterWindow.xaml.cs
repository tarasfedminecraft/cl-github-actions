using CL_CLegendary_Launcher_.Class;
using CL_CLegendary_Launcher_.Windows;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CL_CLegendary_Launcher_
{
    public partial class UpdaterWindow : FluentWindow
    {

        private readonly string tempZipPath = Path.Combine(Path.GetTempPath(), "launcher_update.zip");
        private string localVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        private string targetDownloadUrl = "";
        private string installPath = "";
        private string _downloadedFolderName = "win-x64";

        public UpdaterWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);

            VersionText.Text = $"Ваша версія: {localVersion}";

            string newDirName = $"CL_Launcher_v{localVersion}";
            installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), newDirName);

            if (PathTextBox != null) PathTextBox.Text = installPath;
            Loaded += UpdaterWindow_Loaded;
        }
        private async void UpdaterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1000);
            await CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                StatusText.Text = "Отримання даних...";
                if (BtnUpdate != null) BtnUpdate.Visibility = Visibility.Collapsed;
                if (PathSelectionPanel != null) PathSelectionPanel.Visibility = Visibility.Collapsed;

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("CL-Launcher-Updater");

                string json = "";

                if (File.Exists(Secrets.updateInfoUrl))
                {
                    json = await File.ReadAllTextAsync(Secrets.updateInfoUrl);
                }
                else
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("CL-Launcher-Updater");
                    json = await client.GetStringAsync(Secrets.updateInfoUrl);
                }

                var info = JsonSerializer.Deserialize<UpdateInfo>(json);
                if (info == null || string.IsNullOrEmpty(info.Version))
                {
                    ShowError("Не вдалося прочитати дані про оновлення.");
                    return;
                }

                targetDownloadUrl = GetCorrectUrlAndSetFolder(info);

                if (string.IsNullOrEmpty(targetDownloadUrl))
                {
                    ShowError("Посилання на завантаження відсутнє.");
                    return;
                }

                if (IsUpdateAvailable(info.Version, localVersion))
                {
                    VersionText.Text = $"Нова версія: {info.Version} (Поточна: {localVersion})";
                    StatusText.Text = "Доступне оновлення!";

                    installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"CL_Launcher_{info.Version}");
                    if (PathTextBox != null) PathTextBox.Text = installPath;

                    if (PathSelectionPanel != null) PathSelectionPanel.Visibility = Visibility.Visible;
                    if (BtnUpdate != null) BtnUpdate.Visibility = Visibility.Visible;
                }
                else
                {
                    StatusText.Text = "У вас найновіша версія.";
                    ProgreesBarDowload.Value = 100;
                    await Task.Delay(1500);
                    OpenMainLauncher();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Помилка перевірки: {ex.Message}");
                await Task.Delay(2000);
                OpenMainLauncher();
            }
        }

        private string GetCorrectUrlAndSetFolder(UpdateInfo info)
        {
            bool is64BitOS = Environment.Is64BitOperatingSystem;
            if (!is64BitOS && !string.IsNullOrEmpty(info.UrlX86))
            {
                _downloadedFolderName = "win-x86";
                return info.UrlX86;
            }
            _downloadedFolderName = "win-x64";
            return info.UrlDefault;
        }

        private bool IsUpdateAvailable(string newVer, string currentVer)
        {
            string cleanNew = newVer?.Trim().Replace("v", "", StringComparison.OrdinalIgnoreCase) ?? "0.0.0";
            string cleanCurrent = currentVer?.Trim().Replace("v", "", StringComparison.OrdinalIgnoreCase) ?? "0.0.0";

            System.Windows.MessageBox.Show($"Remote: '{cleanNew}'\nLocal: '{cleanCurrent}'", "Debug Version Check");

            bool v1Success = Version.TryParse(cleanNew, out Version vRemote);
            bool v2Success = Version.TryParse(cleanCurrent, out Version vLocal);

            if (v1Success && v2Success)
            {
                return vRemote > vLocal;
            }

            return !string.Equals(cleanNew, cleanCurrent, StringComparison.OrdinalIgnoreCase);
        }
        private void SelectPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Оберіть папку для встановлення",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                installPath = dialog.FolderName;
                if (PathTextBox != null) PathTextBox.Text = installPath;
            }
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (BtnUpdate != null) BtnUpdate.IsEnabled = false;
            if (PathSelectionPanel != null) PathSelectionPanel.IsEnabled = false;

            try
            {
                StatusText.Text = "Завантаження архіву...";
                await DownloadFileAsync(targetDownloadUrl);

                StatusText.Text = "Розпакування...";
                await Task.Run(() => ExtractZipSafe(tempZipPath, installPath));

                StatusText.Text = "Міграція налаштувань...";
                await Task.Run(() => MigrateUserData());

                StatusText.Text = "Готово! Запуск...";
                await Task.Delay(1500);
                StartNewVersion();
            }
            catch (Exception ex)
            {
                ShowError($"Помилка оновлення: {ex.Message}");
                if (BtnUpdate != null) BtnUpdate.IsEnabled = true;
                if (PathSelectionPanel != null) PathSelectionPanel.IsEnabled = true;
            }
        }
        private async Task DownloadFileAsync(string url)
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1L;

            using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var contentStream = await response.Content.ReadAsStreamAsync();

            var buffer = new byte[8192];
            var totalRead = 0L;
            int read;

            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                totalRead += read;

                if (canReportProgress)
                {
                    var percentage = (double)totalRead / totalBytes * 100;
                    Dispatcher.Invoke(() =>
                    {
                        ProgreesBarDowload.Value = percentage;
                        SizeText.Text = $"{totalRead / 1024 / 1024:F1} MB / {totalBytes / 1024 / 1024:F1} MB";
                    });
                }
            }
        }
        private void ExtractZipSafe(string archivePath, string destination)
        {
            if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destinationPath = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                    if (!destinationPath.StartsWith(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\") || string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    entry.ExtractToFile(destinationPath, true);
                }
            }
            try { File.Delete(archivePath); } catch { }
        }
        private void MigrateUserData()
        {
            try
            {
                string sourceBasePath = AppDomain.CurrentDomain.BaseDirectory;
                string destBasePath = installPath;

                string targetDataRoot = Path.Combine(destBasePath, _downloadedFolderName);
                if (!Directory.Exists(targetDataRoot))
                {
                    targetDataRoot = destBasePath;
                }

                string sourceData = Path.Combine(sourceBasePath, "Data");
                string destData = Path.Combine(targetDataRoot, "Data");

                if (Directory.Exists(sourceData))
                {
                    ReportStatus("Перенесення налаштувань...");
                    CopyDirectorySmart(sourceData, destData);
                }

                string[] configFiles = Directory.GetFiles(sourceBasePath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (string file in configFiles)
                {
                    string fileName = Path.GetFileName(file);

                    if (fileName.EndsWith(".runtimeconfig.json") || fileName.EndsWith(".deps.json"))
                        continue;

                    string destFile = Path.Combine(targetDataRoot, fileName);
                    try
                    {
                        File.Copy(file, destFile, true);
                    }
                    catch {}
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Migration Warning: {ex.Message}");
            }
        }
        private void CopyDirectorySmart(string sourceDir, string destinationDir)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                try
                {
                    string targetFilePath = Path.Combine(destinationDir, file.Name);
                    file.CopyTo(targetFilePath, true);
                }
                catch (IOException)
                {
                }
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                try
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectorySmart(subDir.FullName, newDestinationDir);
                }
                catch
                {
                }
            }
        }
        private void ReportStatus(string text)
        {
            Dispatcher.Invoke(() => StatusText.Text = text);
        }
        private void StartNewVersion()
        {
            string expectedPath = Path.Combine(installPath, _downloadedFolderName, "CL(CLegendary Launcher).exe");

            if (!File.Exists(expectedPath))
            {
                var files = Directory.GetFiles(installPath, "CL(CLegendary Launcher).exe", SearchOption.AllDirectories);
                if (files.Length > 0) expectedPath = files[0];
                else expectedPath = Path.Combine(installPath, "CL(CLegendary Launcher).exe");
            }

            if (File.Exists(expectedPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = expectedPath,
                    WorkingDirectory = Path.GetDirectoryName(expectedPath),
                    UseShellExecute = true
                });
                Application.Current.Shutdown();
            }
            else
            {
                ShowError($"Не знайдено файл запуску!\nШукали тут: {expectedPath}");
                if (BtnUpdate != null) BtnUpdate.IsEnabled = true;
                if (PathSelectionPanel != null) PathSelectionPanel.IsEnabled = true;
            }
        }
        private void ShowError(string message)
        {
            MascotMessageBox.Show(message, "Помилка оновлення", MascotEmotion.Sad);
        }

        private void OpenMainLauncher()
        {
            var loadScreen = new Windows.LoadScreen();
            loadScreen.Show();
            this.Close();
        }
    }

    public class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("url")]
        public string UrlDefault { get; set; } = "";

        [JsonPropertyName("url_x86")]
        public string UrlX86 { get; set; } = "";
    }
}