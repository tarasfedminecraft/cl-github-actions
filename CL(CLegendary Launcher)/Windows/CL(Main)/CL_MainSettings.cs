using CL_CLegendary_Launcher_.Class;
using CL_CLegendary_Launcher_.Windows;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace CL_CLegendary_Launcher_
{
    public partial class CL_Main_
    {
        private void LoadCustomSettings()
        {
            string savedColor = Settings1.Default.LoadScreenBarColor;

            if (string.IsNullOrEmpty(savedColor)) savedColor = "#00BEFF";

            try
            {
                LoadScreenColorButton.Content = _themeService.CreateColorButtonContent(savedColor);
            }
            catch
            {
                LoadScreenColorButton.Content = "#Помилка";
            }
        }

        private void InitToggles()
        {
            DebugToggle.IsChecked = Settings1.Default.EnableLog;
            CloseLauncherToggle.IsChecked = Settings1.Default.CloseLaucnher;
            ModDepsToggle.IsChecked = Settings1.Default.ModDep;
            GlassEffectToggle.IsChecked = !Settings1.Default.DisableGlassEffect;
            FullScreenToggle.IsChecked = Settings1.Default.FullScreen;
            AutoBackupToggle.IsChecked = Settings1.Default.EnableAutoBackup;
            BackupCountText.Text = Settings1.Default.MaxAutoBackups.ToString();
        }
        private void LauncherFloderButton_Click(object sender, RoutedEventArgs e)
        {
            _launcherSettingsService.HandleChangePathClick();
        }
        private void ResetPathMinecraftLauncher_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _launcherSettingsService.HandleResetPathClick();
        }
        private void ResetOP_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _launcherSettingsService.HandleResetOpClick();
        }

        private void ResetResolution_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _launcherSettingsService.HandleResetResolutionClick();
        }
        private void MincraftWindowSize_Click(object sender, RoutedEventArgs e)
        {
            Click();
            if (EditWditXHeghit.Visibility == Visibility.Visible)
            {
                EditWditXHeghit.Visibility = Visibility.Collapsed;
            }
            else
            {
                EditWditXHeghit.Visibility = Visibility.Visible;
            }
        }

        private void ScreenSizeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ScreenSizeListBox.SelectedItem as ListBoxItem;
            if (selectedItem == null)
            {
                MascotMessageBox.Show(
                    "Дивина! Я бачила натискання, але не зрозуміла, який саме варіант ти обрав.\n" +
                    "Спробуй натиснути на рядок ще раз!",
                    "Пустий вибір",
                    MascotEmotion.Confused
                );
                return;
            }

            var resolution = selectedItem.Content.ToString().Split('x');
            if (resolution.Length != 2 || !int.TryParse(resolution[0], out int width) || !int.TryParse(resolution[1], out int height))
            {
                MascotMessageBox.Show(
                    "Ой! Я намагалася розібрати цей розмір екрана, але цифри записані якось дивно.\n" +
                    "Формат має бути 'ШиринаxВисота' (наприклад, 1920x1080), а тут щось інше.",
                    "Невірний формат",
                    MascotEmotion.Sad
                );
                return;
            }

            Width.Text = resolution[0];
            Height.Text = resolution[1];
            MincraftWindowSize.Content = $"{width}x{height}";

            Settings1.Default.width = width;
            Settings1.Default.height = height;
            Settings1.Default.Save();
        }

        private void HandleTextBoxInput(System.Windows.Controls.TextBox textBox, string dimensionKey)
        {
            int caretIndex = textBox.CaretIndex;
            string validText = Regex.Replace(textBox.Text, @"[^\d]", "");

            if (string.IsNullOrEmpty(validText))
            {
                textBox.Text = dimensionKey == "width" ? "800" : "600";
            }
            else
            {
                textBox.Text = validText;

                if (int.TryParse(validText, out int result))
                {
                    result = Math.Max(800, Math.Min(3840, result));
                    if (dimensionKey == "width")
                        Settings1.Default.width = result;
                    else
                        Settings1.Default.height = result;

                    Settings1.Default.Save();
                }
            }

            UpdateMinecraftWindowSize();
            textBox.CaretIndex = caretIndex;
        }

        private void UpdateMinecraftWindowSize()
        {
            MincraftWindowSize.Content = $"{Settings1.Default.width}x{Settings1.Default.height}";
        }

        private void Width_PreviewKeyDown(object sender, KeyEventArgs e) => HandleTextBoxInput(Width, "width");
        private void Height_PreviewKeyDown(object sender, KeyEventArgs e) => HandleTextBoxInput(Height, "height");
        private void Width_PreviewKeyUp(object sender, KeyEventArgs e) => HandleTextBoxInput(Width, "width");
        private void Height_PreviewKeyUp(object sender, KeyEventArgs e) => HandleTextBoxInput(Height, "height");

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.WindowState = WindowState.Maximized;
            }
        }

        private void FullScreenOff_On_MouseDown(object sender, RoutedEventArgs e)
        {
            Click();
            Settings1.Default.FullScreen = !Settings1.Default.FullScreen;
            Settings1.Default.Save();
            FullScreenToggle.IsChecked = Settings1.Default.FullScreen;
        }
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
            Settings1.Default.OP = (int)OPSlider.Value;
            Settings1.Default.Save();
        }
        private void DebugOff_On_Click(object sender, RoutedEventArgs e)
        {
            Click();
            Settings1.Default.EnableLog = !Settings1.Default.EnableLog;
            Settings1.Default.Save();
            DebugToggle.IsChecked = Settings1.Default.EnableLog;
        }

        private void CloseLaucnherPlayMinecraft_MouseDown(object sender, RoutedEventArgs e)
        {
            Click();
            Settings1.Default.CloseLaucnher = !Settings1.Default.CloseLaucnher;
            Settings1.Default.Save();
            CloseLauncherToggle.IsChecked = Settings1.Default.CloseLaucnher;
        }
        private void DisableGlassEffectToggle_MouseDown(object sender, RoutedEventArgs e)
        {
            Click();

            Settings1.Default.DisableGlassEffect = !Settings1.Default.DisableGlassEffect;
            Settings1.Default.Save();

            _themeService.ToggleGlassEffect(Settings1.Default.DisableGlassEffect);
            GlassEffectToggle.IsChecked = !Settings1.Default.DisableGlassEffect;
        }
        private void InitializeThemeSelection()
        {
            string currentSettingsTheme = Settings1.Default.Them; 

            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag.ToString() == currentSettingsTheme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedTheme = selectedItem.Tag.ToString();

                Click();

                _themeService.ApplyTheme(selectedTheme);

                if (selectedTheme == "Custom")
                {
                    _themeService.LoadBackgroundImage();
                }

                Settings1.Default.Them = selectedTheme;
                Settings1.Default.Save();
            }
        }
        private void Background_imageButton_Click(object sender, RoutedEventArgs e) => _themeService.HandleBackgroundImageClick();
        private void OnThemeColorButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagData)
            {
                var parts = tagData.Split('|');
                if (parts.Length == 2)
                {
                    _themeService.HandleColorChange(btn, parts[0], parts[1]);
                }
            }
        }
        private void SaveandAcceptCustomThem_Click(object sender, RoutedEventArgs e) => _themeService.HandleSaveCustomThemeClick();
        private void ResetCustomSetting_Click(object sender, RoutedEventArgs e) => _themeService.HandleResetCustomThemeClick();
        private void LoadScreenBgButton_Click(object sender, RoutedEventArgs e) => _themeService.HandleLoadScreenBackgroundClick();
        private void LoadScreenColorButton_Click(object sender, RoutedEventArgs e) => _themeService.HandleLoadScreenColorClick(LoadScreenColorButton);
        private void EditPhrasesButton_Click(object sender, RoutedEventArgs e) => _themeService.HandleEditPhrasesClick();
        private void ResetLoadScreen_Click(object sender, RoutedEventArgs e) => _themeService.HandleResetLoadScreenClick(LoadScreenColorButton);
        private void CopyLoadScreen_Click(object sender, RoutedEventArgs e)
        {
            string code = _themeService.ExportLoadScreen();
            LoadScreenCodeBox.Text = code;
            Clipboard.SetText(code);
            NotificationService.ShowNotification("Код LoadScreen скопійовано!", "Експорт", SnackbarPresenter, default, default, Wpf.Ui.Controls.ControlAppearance.Info);
        }

        private void CopyTheme_Click(object sender, RoutedEventArgs e)
        {
            string code = _themeService.ExportMainTheme();
            ThemeCodeBox.Text = code;
            Clipboard.SetText(code);
            NotificationService.ShowNotification("Код теми скопійовано!", "Експорт", SnackbarPresenter, default, default, Wpf.Ui.Controls.ControlAppearance.Info);
        }

        private void PasteTheme_Click(object sender, RoutedEventArgs e)
        {
            string code = Clipboard.ContainsText() ? Clipboard.GetText().Trim() : ThemeCodeBox.Text.Trim();
            if (!string.IsNullOrEmpty(code)) _themeService.ImportMainTheme(code);
        }

        private void PasteLoadScreen_Click(object sender, RoutedEventArgs e)
        {
            string code = Clipboard.ContainsText() ? Clipboard.GetText().Trim() : LoadScreenCodeBox.Text.Trim();
            if (!string.IsNullOrEmpty(code)) _themeService.ImportLoadScreen(code);
        }
        private void AutoBackupToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                Settings1.Default.EnableAutoBackup = toggle.IsChecked ?? false;
                Settings1.Default.Save();
            }
        }
        private void BackupCountMinus_Click(object sender, RoutedEventArgs e)
        {
            byte current = byte.Parse(BackupCountText.Text);
            if (current > 1) 
            {
                current--;
                UpdateBackupCount(current);
            }
        }
        private void BackupCountPlus_Click(object sender, RoutedEventArgs e)
        {
            byte current = byte.Parse(BackupCountText.Text);
            if (current < 20) 
            {
                current++;
                UpdateBackupCount(current);
            }
        }
        private void UpdateBackupCount(byte count)
        {
            BackupCountText.Text = count.ToString();
            Settings1.Default.MaxAutoBackups = count;
            Settings1.Default.Save();
        }
    }
}