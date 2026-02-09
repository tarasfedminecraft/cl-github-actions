using CL_CLegendary_Launcher_.Class;
using CL_CLegendary_Launcher_.Models;
using CL_CLegendary_Launcher_.Windows;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CL_CLegendary_Launcher_
{
    public partial class CL_Main_
    {
        private string currentScreenshotsPath;
        internal int serverCount;
        internal int loadedCount;

        private async void CL_CLegendary_Launcher__Loaded_1(object sender, RoutedEventArgs e)
        {
            if (!Settings1.Default.TutorialComplete) { AnimationService.AnimatePageTransition(TutorialGrid); }
            else { TutorialGrid.Visibility = Visibility.Collapsed; }

            if (Settings1.Default.width != 0 && Settings1.Default.height != 0)
            {
                Width.Text = Settings1.Default.width.ToString();
                Height.Text = Settings1.Default.height.ToString();
                MincraftWindowSize.Content = $"{Settings1.Default.width}x{Settings1.Default.height}";
            }
            else
            {
                Settings1.Default.width = 800;
                Settings1.Default.height = 600;
                Settings1.Default.Save();
                Width.Text = "800";
                Height.Text = "600";
                MincraftWindowSize.Content = "800x600";
            }

            int savedType = Settings1.Default.LastSelectedType;
            string savedVer = Settings1.Default.LastSelectedVersion;
            string savedModVer = Settings1.Default.LastSelectedModVersion;

            if (savedType != 0 && !string.IsNullOrEmpty(savedVer))
            {
                VersionSelect = (byte)savedType;

                if (savedType == 5 && !string.IsNullOrEmpty(savedModVer))
                {
                    IconSelectVersion.Source = IconSelectVersion_Optifine.Source;
                    PlayTXT.Text = $"ГРАТИ В ({savedModVer})";
                }
                else if (savedType == 1)
                {
                    IconSelectVersion.Source = IconSelectVersion_Копировать.Source;
                    PlayTXT.Text = $"ГРАТИ В ({savedVer})";
                }
            }
            else
            {
                PlayTXT.Text = "ОБЕРІТЬ ВЕРСІЮ";
            }

            LoadCustomSettings();
            this.Dispatcher.Invoke(() =>
            {
                MemoryCleaner.FlushMemoryAsync(true);
            });
        }
        private void CL_CLegendary_Launcher__Closed(object sender, EventArgs e)
        {
            DiscordController.Deinitialize();
        }

        private void CL_CLegendary_Launcher__Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }
        public void Click()
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
        private void BackMainWindow_MouseDown(object sender, RoutedEventArgs e)
        {
            Click();
            if (PanelInfoServer.Visibility == Visibility.Visible) { AnimationService.AnimatePageTransitionExit(PanelInfoServer); AnimationService.AnimatePageTransition(ServerName); }

        }
        public BitmapImage ConvertBitmapToBitmapImage(System.Drawing.Bitmap bitmap)
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
        public async Task HideAllPages()
        {
            var allPages = new List<FrameworkElement>
            {
                SelectGirdAccount, ScrollSetting, SettingPanelMinecraft, PanelManegerAccount,
                ServerName, ListModsGird, ListModsBuild, GalleryContainer,
                GirdPanelFooter, GirdNews, ListNews, GirdTXTNews
            };

            bool animationStarted = false;

            foreach (var page in allPages)
            {
                if (page.Visibility == Visibility.Visible)
                {
                    AnimationService.AnimatePageTransitionExit(page);
                    animationStarted = true;
                }
            }

            ListNews.Items?.Clear();
            TextNews.Text = null;
            ScreenshotsList.Items?.Clear();
            ModsDowloadList1.Items?.Clear();

            ServerList.Items?.Clear();

            DescriptionServer.Text = null;

            if (animationStarted)
            {
                await Task.Delay(300);
            }
        }

        private void PlayTXTPanelSelect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _navigationService.NavigateToHome();
        }

        private void ModsTXTPanelSelect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _navigationService.NavigateToMods();
        }

        private void modbuilds_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _navigationService.NavigateToModPacks();
        }

        private void PhotoMinecraftTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _navigationService.NavigateToGallery();
        }

        private async void SettingPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();
            await HideAllPages();

            AnimationService.AnimatePageTransition(SettingPanelMinecraft, 0.3);
            AnimationService.AnimatePageTransition(ScrollSetting, 0.2);
        }
        private void FolderPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (sender is Grid grid && grid.ContextMenu != null)
                {
                    grid.ContextMenu.PlacementTarget = grid;
                    grid.ContextMenu.IsOpen = !grid.ContextMenu.IsOpen;
                }
            }
        }
        private void InfoPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (sender is Grid grid && grid.ContextMenu != null)
                {
                    grid.ContextMenu.PlacementTarget = grid;
                    grid.ContextMenu.IsOpen = !grid.ContextMenu.IsOpen;
                }
            }
        }
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Settings1.Default.PathLacunher;

                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                }
                else
                {
                    Directory.CreateDirectory(path);
                    Process.Start("explorer.exe", path);
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show($"Не вдалося відкрити папку:\n{ex.Message}", "Помилка", MascotEmotion.Sad);
            }
        }
        private void OpenGlobalBackups_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var managerWindow = new WorldBackupWindow();
                managerWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show($"Не вдалося відкрити список світів:\n{ex.Message}", "Помилка", MascotEmotion.Sad);
            }
        }
        private void BackMainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();
            if (ServerList.Visibility == Visibility.Visible || PanelInfoServer.Visibility == Visibility.Visible)
            {
                BackMainWindow.Visibility = Visibility.Hidden;
                SearchSystemTXT.Visibility = Visibility.Hidden;
                AnimationService.FadeOut(PanelInfoServer, 0.4);
                AnimationService.FadeOut(ServerList, 0.3);
            }
        }
        private void YesQuestionTutorialButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Settings1.Default.TutorialComplete = true;
            Settings1.Default.IsDocsTutorialShown = true;
            WebHelper.OpenUrl("https://github.com/WER-CORE/CL-OpenSource");
            Settings1.Default.Save();
            AnimationService.AnimatePageTransitionExit(TutorialBorder);
            AnimationService.AnimatePageTransitionExit(TutorialGrid);
        }

        private void NoQuestionTutorialButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Settings1.Default.TutorialComplete = true;
            Settings1.Default.Save();

            AnimationService.AnimatePageTransitionExit(TutorialBorder);
            AnimationService.AnimatePageTransitionExit(TutorialGrid);

            _tutorialService.ShowTutorial(InfoLauncherPanel, null, -120);
        }

        private void CloseTutorial_Click(object sender, RoutedEventArgs e)
        {
            _tutorialService.CloseTutorial(() =>
            {
                Settings1.Default.IsDocsTutorialShown = true;
                Settings1.Default.Save();
            });
        }
        private async void LoadScreenshots()
        {
            ScreenshotsList.Items.Clear();
            NoScreenshotsText.Visibility = Visibility.Visible;

            if (string.IsNullOrEmpty(currentScreenshotsPath)) return;

            var items = await _screenshotService.LoadScreenshotsAsync(currentScreenshotsPath);

            if (items.Count > 0)
            {
                NoScreenshotsText.Visibility = Visibility.Hidden;
                foreach (var item in items)
                {
                    ScreenshotsList.Items.Add(item);
                }
            }
        }

        public void InitializeGallery()
        {
            var sources = _screenshotService.GetScreenshotSources(Settings1.Default.PathLacunher);

            SourceSelector.ItemsSource = sources;
            SourceSelector.DisplayMemberPath = "Name";

            if (sources.Count > 0)
                SourceSelector.SelectedIndex = 0;
        }

        private void SourceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SourceSelector.SelectedItem is ScreenshotSourceItem selectedSource)
            {
                currentScreenshotsPath = selectedSource.FullScreenshotsPath;
                LoadScreenshots();
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentScreenshotsPath))
            {
                if (!Directory.Exists(currentScreenshotsPath)) Directory.CreateDirectory(currentScreenshotsPath);
                System.Diagnostics.Process.Start("explorer.exe", currentScreenshotsPath);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadScreenshots();
        }

        private void ScreenshotsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScreenshotsList.SelectedItem != null)
            {
                AnimationService.AnimatePageTransition(ActionPanel);
                var item = (ScreenshotItem)ScreenshotsList.SelectedItem;
                SelectedFileNameText.Text = item.FileName;
            }
            else
            {
                AnimationService.AnimatePageTransitionExit(ActionPanel);
            }
        }

        private void BtnOpenImage_Click(object sender, RoutedEventArgs e)
        {
            if (ScreenshotsList.SelectedItem is ScreenshotItem item)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.FilePath) { UseShellExecute = true });
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ScreenshotsList.SelectedItem is ScreenshotItem item)
            {
                if (_screenshotService.DeleteScreenshot(item.FilePath))
                {
                    ScreenshotsList.Items.Remove(item);

                    if (ScreenshotsList.Items.Count == 0)
                        NoScreenshotsText.Visibility = Visibility.Visible;

                    AnimationService.AnimatePageTransitionExit(ActionPanel);
                }
                else
                {
                    MascotMessageBox.Show("Не вдалося видалити файл.");
                }
            }
        }
    }
}