using CL_CLegendary_Launcher_.Class;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CL_CLegendary_Launcher_.Windows
{
    public class BackupSource
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class WorldListItem
    {
        public string Name { get; set; }
        public string FolderName { get; set; }
        public string FullPath { get; set; }
        public string IconPath { get; set; }
        public BitmapImage IconBitmap { get; set; }
        public string Version { get; set; }
        public string WorldId { get; set; }
    }

    public partial class WorldBackupWindow : FluentWindow
    {
        private WorldListItem _currentWorld;

        public WorldBackupWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
            LoadSources();
        }
        private void LoadSources()
        {
            var sources = new List<BackupSource>();
            string rootPath = Settings1.Default.PathLacunher;

            string globalSaves = Path.Combine(rootPath, "saves");
            sources.Add(new BackupSource
            {
                Name = "📂 Глобальні світи (Global)",
                Path = globalSaves
            });

            string versionsPath = Path.Combine(rootPath, "CLModpack");

            if (Directory.Exists(versionsPath))
            {
                var dirs = Directory.GetDirectories(versionsPath);

                foreach (var dir in dirs)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    string verName = dirInfo.Name;

                    var possiblePaths = new List<string>
                    {
                        Path.Combine(dir, "saves"),
                        Path.Combine(dir, "override", "saves"),
                        Path.Combine(dir, "overrides", "saves")
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (Directory.Exists(path))
                        {
                            sources.Add(new BackupSource
                            {
                                Name = $"📦 Збірка: {verName}",
                                Path = path
                            });

                            break;
                        }
                    }
                }
            }
            SourceCombo.ItemsSource = sources;
            SourceCombo.SelectedIndex = 0;
        }

        private void SourceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SourceCombo.SelectedItem is BackupSource source)
            {
                LoadWorlds(source.Path);
            }
        }

        private void LoadWorlds(string savesPath)
        {
            _currentWorld = null;
            PlaceholderPanel.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Hidden;

            var list = new List<WorldListItem>();

            if (!Directory.Exists(savesPath)) Directory.CreateDirectory(savesPath);

            var dirs = Directory.GetDirectories(savesPath);
            foreach (var dir in dirs)
            {
                string levelDatPath = Path.Combine(dir, "level.dat");
                if (File.Exists(levelDatPath))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    string iconPath = Path.Combine(dir, "icon.png");

                    if (!File.Exists(iconPath))
                        iconPath = "pack://application:,,,/Icon/IconCL(Common).png";

                    BitmapImage bmp = null;
                    try
                    {
                        if (File.Exists(iconPath) || iconPath.StartsWith("pack:"))
                        {
                            bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = new Uri(iconPath);
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                        }
                    }
                    catch { }

                    string version = GetVersionFromLevelDat(levelDatPath);
                    string wId = WorldBackupService.GetWorldID(dir);

                    list.Add(new WorldListItem
                    {
                        Name = dirInfo.Name,
                        FolderName = dirInfo.Name,
                        FullPath = dir,
                        IconPath = iconPath,
                        IconBitmap = bmp,
                        Version = version,
                        WorldId = wId 
                    });
                }
            }

            WorldsListBox.ItemsSource = list;
            NoWorldsTxt.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private string GetVersionFromLevelDat(string path)
        {
            try
            {
                using (FileStream fs = File.OpenRead(path))
                using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
                using (MemoryStream ms = new MemoryStream())
                {
                    gzip.CopyTo(ms);
                    byte[] data = ms.ToArray();
                    for (int i = 0; i < data.Length - 10; i++)
                    {
                        if (data[i] == 0x08 && data[i + 1] == 0x00 && data[i + 2] == 0x04 &&
                            data[i + 3] == (byte)'N' && data[i + 4] == (byte)'a' &&
                            data[i + 5] == (byte)'m' && data[i + 6] == (byte)'e')
                        {
                            int lenIndex = i + 7;
                            if (lenIndex + 1 >= data.Length) break;

                            short strLen = (short)((data[lenIndex] << 8) | data[lenIndex + 1]);

                            if (strLen > 0 && lenIndex + 2 + strLen <= data.Length)
                            {
                                string ver = Encoding.UTF8.GetString(data, lenIndex + 2, strLen);
                                if (char.IsDigit(ver[0]) || ver.Length < 30)
                                {
                                    return ver;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return "Unknown";
            }
            return "?";
        }

        private void WorldsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WorldsListBox.SelectedItem is WorldListItem world)
            {
                _currentWorld = world;
                PlaceholderPanel.Visibility = Visibility.Collapsed;
                ContentPanel.Visibility = Visibility.Visible;

                SelectedName.Text = world.Name;
                SelectedVersionText.Text = "Версія:" + world.Version.ToString();
                SelectedFolder.Text = $"Папка: {world.FolderName}";
                SelectedIcon.Source = world.IconBitmap;

                RefreshBackups();
            }
        }

        private void RefreshBackups()
        {
            if (_currentWorld == null) return;

            var backups = WorldBackupService.GetBackupsForWorld(_currentWorld.FullPath);

            BackupsList.ItemsSource = backups;
            NoBackupsTxt.Visibility = backups.Count == 0 ? Visibility.Visible : Visibility.Hidden;
        }

        private async void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            if (_currentWorld == null) return;
            BtnCreate.IsEnabled = false;

            try
            {
                await WorldBackupService.CreateWorldBackupAsync(_currentWorld.FullPath);
                RefreshBackups();
                MascotMessageBox.Show("Бекап створено успішно!", "Успіх", MascotEmotion.Happy);
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show($"Помилка створення: {ex.Message}", "Ой", MascotEmotion.Sad);
            }
            finally
            {
                BtnCreate.IsEnabled = true;
            }
        }

        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var backup = btn.Tag as WorldBackupInfo;

            if (MascotMessageBox.Ask(
                $"Відновити світ '{_currentWorld.Name}' до стану від {backup.CreationTime:dd.MM HH:mm}?\n\n" +
                "⚠️ Поточний прогрес буде втрачено!",
                "Відновлення", MascotEmotion.Alert))
            {
                this.IsEnabled = false;
                try
                {
                    string savesRoot = Directory.GetParent(_currentWorld.FullPath).FullName;

                    await WorldBackupService.RestoreWorldBackupAsync(backup.FullPath, savesRoot);

                    MascotMessageBox.Show("Світ відновлено!", "Готово", MascotEmotion.Happy);

                    if (SourceCombo.SelectedItem is BackupSource source)
                    {
                        LoadWorlds(source.Path);
                    }
                }
                catch (Exception ex)
                {
                    MascotMessageBox.Show($"Помилка: {ex.Message}", "Біда", MascotEmotion.Sad);
                }
                finally
                {
                    this.IsEnabled = true;
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var backup = btn.Tag as WorldBackupInfo;

            if (MascotMessageBox.Ask("Видалити цей архів?", "Видалення", MascotEmotion.Normal))
            {
                try
                {
                    File.Delete(backup.FullPath);
                    RefreshBackups();
                }
                catch (Exception ex)
                {
                    MascotMessageBox.Show(ex.Message, "Помилка", MascotEmotion.Sad);
                }
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}