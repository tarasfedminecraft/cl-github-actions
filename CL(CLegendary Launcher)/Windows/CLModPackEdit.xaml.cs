using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NAudio.Wave;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Windows.Controls.Primitives;
using CL_CLegendary_Launcher_.Class;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class CLModPackEdit : Window
    {
        public ModpackInfo CurrentModpack { get; set; }
        public string PathJsonModPack { get; set; }
        public string PathMods { get; set; }
        public string TypeModPack { get; set; }
        public string VersionType { get; set; }
        public string typesite { get; set; }
        public string version { get; set; }
        public int index { get; set; }
        public string projectId;
        byte selectmodPack = 0;
        public event Action ModpackUpdated;
        public CLModPackEdit()
        {
            InitializeComponent();
        }
        void Click()
        {
            Task.Run(() =>
            {
                var Click = new NAudio.Vorbis.VorbisWaveReader(Resource2.click);
                using (var waveOut = new NAudio.Wave.WaveOutEvent())
                {
                    waveOut.Volume = 0.1f;
                    waveOut.Init(Click);
                    waveOut.Play();
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
            });
        }
        private async Task UpdateModsTypeMinecraftAsync()
        {
            var CurrentMod = selectmodPack switch
            {
                0 => "mods",
                1 => "resourcepacks",
                2 => "shaderpacks",
                3 => "Settings_Packs",
            };

            await DiscordController.UpdatePresence("Налаштовує моди");
            ModsManegerList.Visibility = Visibility.Visible;
            ModsManegerList.Items.Clear();

            string modsDirectory = $@"{PathMods}{CurrentMod}";
            if (!Directory.Exists(modsDirectory))
            {
                MascotMessageBox.Show(
                                    "Ой! Я не можу знайти папку з модами.\nМожливо, вона ще не створена?",
                                    "Папка відсутня",
                                    MascotEmotion.Confused);
                return;
            }
            string[] searchPatterns = selectmodPack switch
            {
                0 => new[] { "*.jar", "*.jar.disabled", "*.litemod", "*.litemod.disabled" },
                1 => new[] { "*.zip", "*.zip.disabled" },
                2 => new[] { "*.zip", "*.rar", "*.zip.disabled", "*.rar.disabled" },
                _ => new[] { "*.*" }
            };

            var modFiles = searchPatterns
              .SelectMany(pattern => Directory.GetFiles(modsDirectory, pattern))
              .ToArray();

            string searchQuery = SearchSystem.Text.ToLower();

            foreach (var file in modFiles)
            {
                var fileName = System.IO.Path.GetFileName(file);

                if (!string.IsNullOrEmpty(searchQuery) && !fileName.ToLower().Contains(searchQuery))
                {
                    continue; 
                }

                bool isEnabled = !fileName.EndsWith(".disabled"); 

                var item = new ItemManegerPack
                {
                    Title = { Text = fileName.Replace(".disabled", "") }, 
                    Description = { Text = "" }, 
                    pathmods = file, 
                    Off_OnMod = isEnabled, 
                    IsModPack = true, 
                    CurrentModpack = CurrentModpack,
                };
                ModsManegerList.SelectionChanged += (s, e) =>
                {
                    if (ModsManegerList.SelectedItem != null)
                        item.Index = ModsManegerList.SelectedIndex;
                };
                item.Off_OnMod = isEnabled;
                ModsManegerList.Items.Add(item);
            }
        }
        private void ExitLauncher_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationService.FadeIn(BordrExitTXT, 0.5);
        }
        private void ExitLauncher_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationService.FadeOut(BordrExitTXT, 0.5);
        }
        private void ExitLauncher_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        private void BorderTool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        bool IsChecekd;
        bool IsChecekd2;
        private bool isSliderDragging;

        private void DebugOff_On_Click(object sender, MouseButtonEventArgs e)
        {
            Click();
            IsChecekd = !IsChecekd;
            DebugOff_On.Source = IsChecekd
                ? ConvertBitmapToBitmapImage(Resource2.toggle__1_)
                : ConvertBitmapToBitmapImage(Resource2.toggle__2_);
            EditInstalledModpack(CurrentModpack.Name, "IsConsoleLogOpened", IsChecekd);
        }
        private void OnJoinServerOff_On_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();
            IsChecekd2 = !IsChecekd2;
            OnJoinServerOff_On.Source = IsChecekd2
                ? ConvertBitmapToBitmapImage(Resource2.toggle__1_)
                : ConvertBitmapToBitmapImage(Resource2.toggle__2_);
            IPAdressServer.IsEnabled = IsChecekd2;

            EditInstalledModpack(CurrentModpack.Name, "EnterInServer", IsChecekd2);
        }
        BitmapImage ConvertBitmapToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        private async void ModsPack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsVanillaVersion())
            {
                ShowError("Ех, це ж ванільна версія! Сюди моди не поставиш.\nСпробуй створити збірку на Forge або Fabric.");
                return;
            }

            await SwitchTab(0, 0); 
        }

        private async void Resource_packPack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await SwitchTab(1, 30); 
        }

        private async void ShaderPack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsVanillaVersion())
            {
                ShowError("Без OptiFine або Iris шейдери не запрацюють.\nНа ванільній версії це, на жаль, неможливо.");
                return;
            }

            await SwitchTab(2, 60);
        }

        private void SettingPack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _ = SwitchTab(3, 90);
        }

        private async Task SwitchTab(byte index, double positionY)
        {
            selectmodPack = index;
            AnimationService.AnimateBorderObject(0, positionY, PanelSelectNowSiteMods, true);

            if (index == 3) 
            {
                ManegerPack.Visibility = Visibility.Hidden;
                SettingPack_Mod.Visibility = Visibility.Visible;
            }
            else 
            {
                ManegerPack.Visibility = Visibility.Visible;
                SettingPack_Mod.Visibility = Visibility.Hidden;

                await UpdateModsTypeMinecraftAsync();
            }
        }
        private bool IsVanillaVersion()
        {
            return CurrentModpack.LoaderType == "Vanila" || CurrentModpack.LoaderType == "Vanilla";
        }
        private void ShowError(string message)
        {
            MascotMessageBox.Show(message, "Обмеження", MascotEmotion.Sad);
        }
        private double previousSliderValue = 2048;

        private void OPSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isSliderDragging) return; 

            SliderOPTXT.Content = OPSlider.Value.ToString("0") + "MB";

            double direction = OPSlider.Value - previousSliderValue;

            var track = OPSlider.Template.FindName("PART_Track", OPSlider) as Track;
            var thumb = track?.Thumb;
            if (thumb != null && thumb.RenderTransform is ScaleTransform scale)
            {
                scale.ScaleX = scale.ScaleY = Math.Max(0.5, Math.Min(2, scale.ScaleX + (direction > 0 ? 0.009 : -0.009)));
            }

            previousSliderValue = OPSlider.Value;
        }
        private void OPSlider_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Click();
            isSliderDragging = true; 
        }
        private void OPSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isSliderDragging = false;

            EditInstalledModpack(CurrentModpack.Name, "OPack", (int)OPSlider.Value);
        }
        private double GetTotalMemoryInMB()
        {
            double totalMemoryInBytes = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            return totalMemoryInBytes / (1024 * 1024);
        }
        private bool EditInstalledModpack(string modpackName, string propertyName, object newValue)
        {
            try
            {
                string exeDir = AppContext.BaseDirectory;
                string jsonPath = Path.Combine(exeDir, "Data", "installed_modpacks.json");

                if (!File.Exists(jsonPath))
                {
                    return false;
                }

                string json = File.ReadAllText(jsonPath);
                var modpacks = System.Text.Json.JsonSerializer.Deserialize<List<InstalledModpack>>(json);

                if (modpacks == null || modpacks.Count == 0)
                {
                    return false;
                }

                var targetPack = modpacks.FirstOrDefault(p =>
                    p.Name.Equals(modpackName, StringComparison.OrdinalIgnoreCase));

                if (targetPack == null)
                {
                    return false;
                }

                var property = typeof(InstalledModpack).GetProperty(propertyName);
                if (property == null || !property.CanWrite)
                {
                    return false;
                }

                object convertedValue = Convert.ChangeType(newValue, property.PropertyType);
                property.SetValue(targetPack, convertedValue);

                string updatedJson = System.Text.Json.JsonSerializer.Serialize(modpacks, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, updatedJson);

                ModpackUpdated?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ой, я не змогла зберегти налаштування.\n{ex.Message}",
                                    "Помилка збереження",
                                    MascotEmotion.Sad);
                return false;
            }
        }

        private async void SearchSystem_TextChanged(object sender, TextChangedEventArgs e)
        {
            await UpdateModsTypeMinecraftAsync();
        }

        private void HeghitTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HeghitTXT == null || CurrentModpack == null) return;
            if (int.TryParse(HeghitTXT.Text, out int _))
                EditInstalledModpack(CurrentModpack.Name, "Height", HeghitTXT.Text);
        }

        private void WdithTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WdithTXT == null || CurrentModpack == null) return;
            if (int.TryParse(WdithTXT.Text, out int _))
                EditInstalledModpack(CurrentModpack.Name, "Wdith", WdithTXT.Text);
        }

        private void IPAdressServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IPAdressServer == null || CurrentModpack == null) return;
            if (!string.IsNullOrWhiteSpace(IPAdressServer.Text))
                EditInstalledModpack(CurrentModpack.Name, "ServerIP", IPAdressServer.Text);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OPSlider.Maximum = GetTotalMemoryInMB();
            OPSlider.Value = CurrentModpack.OPack;
            SliderOPTXT.Content = OPSlider.Value.ToString("0") + "MB";
            WdithTXT.Text = CurrentModpack.Wdith.ToString();
            HeghitTXT.Text = CurrentModpack.Height.ToString();
            IPAdressServer.Text = CurrentModpack.ServerIP.ToString();
            IsChecekd = CurrentModpack.IsConsoleLogOpened;
            IsChecekd2 = CurrentModpack.EnterInServer;
            IPAdressServer.IsEnabled = CurrentModpack.EnterInServer;

            OnJoinServerOff_On.Source = IsChecekd2
                ? ConvertBitmapToBitmapImage(Resource2.toggle__1_)
                : ConvertBitmapToBitmapImage(Resource2.toggle__2_);
            DebugOff_On.Source = IsChecekd
                ? ConvertBitmapToBitmapImage(Resource2.toggle__1_)
                : ConvertBitmapToBitmapImage(Resource2.toggle__2_);

            if (CurrentModpack.LoaderType == "Vanila") { selectmodPack = 1; }
        }

        private void AddFileInPack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = selectmodPack == 0
                    ? "Jar Files (*.jar)|*.jar"
                    : "Zip Files (*.zip)|*.zip|Rar Files (*.rar)|*.rar";

                openFileDialog.Multiselect = true;

                string currentModFolder = selectmodPack switch
                {
                    0 => "mods",
                    1 => "resourcepacks",
                    2 => "shaderpacks",
                    _ => "mods"
                };

                string modsDirectory = Path.Combine(PathMods, currentModFolder);
                Directory.CreateDirectory(modsDirectory);

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string[] sourceFiles = openFileDialog.FileNames;
                    string[] fileNames = openFileDialog.SafeFileNames;

                    for (int i = 0; i < sourceFiles.Length; i++)
                    {
                        string targetPath = Path.Combine(modsDirectory, fileNames[i]);

                        if (File.Exists(targetPath))
                            File.Delete(targetPath);

                        File.Copy(sourceFiles[i], targetPath);
                    }
                }
            }

        }

        private void DownloadAddMod_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DownloadEditPack downloadEditPack = new DownloadEditPack(this.CurrentModpack,selectmodPack); 
            downloadEditPack.Show();
        }
    }
}
