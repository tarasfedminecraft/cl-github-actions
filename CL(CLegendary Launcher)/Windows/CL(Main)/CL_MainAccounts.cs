using CL_CLegendary_Launcher_.Class;
using CL_CLegendary_Launcher_.Models;
using CL_CLegendary_Launcher_.Windows;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CL_CLegendary_Launcher_
{
    public partial class CL_Main_
    {
        public async Task LoadProfilesAsync()
        {
            ListAccount.Items.Clear();

            var profiles = await Task.Run(() => _accountService.GetProfiles());

            if (profiles == null || profiles.Count == 0)
            {
                Settings1.Default.SelectIndexAccount = -1;
                Settings1.Default.Save();
                return;
            }

            foreach (var profile in profiles)
            {
                int index = profiles.IndexOf(profile);

                var uiItem = LauncherUIFactory.CreateAccountControl(
                    profile,
                    index,
                    OnDeleteProfileClicked,
                    OnSelectProfileClicked
                );

                uiItem.Tag = profile;
                ListAccount.Items.Add(uiItem);
            }

            int savedIndex = Settings1.Default.SelectIndexAccount;
            if (savedIndex >= 0 && savedIndex < profiles.Count)
            {
                OnSelectProfileClicked(profiles[savedIndex]);
            }
        }

        private void OnDeleteProfileClicked(ProfileItem profile)
        {
            var result = MascotMessageBox.Ask($"Видалити {profile.NameAccount}?", "Видалення", MascotEmotion.Alert);
            if (result != true) return;

            _accountService.DeleteProfile(profile);

            var itemToRemove = ListAccount.Items.OfType<ItemManegerProfile>()
                                                .FirstOrDefault(x => x.UUID == profile.UUID);

            if (itemToRemove != null)
            {
                ListAccount.Items.Remove(itemToRemove);
            }

            if (NameNik.Text == profile.NameAccount)
            {
                NameNik.Text = "Відсутній акаунт";
                IconAccount.Source = null;
                selectAccountNow = 0;

                Settings1.Default.SelectIndexAccount = -1;
                Settings1.Default.Save();
            }
            else
            {
                UpdateSavedIndex();
            }

            NotificationService.ShowNotification("Успіх", "Профіль стерто.", SnackbarPresenter, 3);
        }

        private async void OnSelectProfileClicked(ProfileItem profile)
        {
            try
            {
                await _accountService.SelectProfileAsync(profile);

                NameNik.Text = profile.NameAccount;
                selectAccountNow = profile.TypeAccount;

                if (!string.IsNullOrEmpty(profile.ImageUrl))
                {
                    try
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(profile.ImageUrl, UriKind.RelativeOrAbsolute);
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.DecodePixelWidth = 64;
                        image.EndInit();
                        image.Freeze();
                        IconAccount.Source = image;
                    }
                    catch
                    {
                        IconAccount.Source = null;
                    }
                }
                else
                {
                    IconAccount.Source = null;
                }

                UpdateSavedIndex(profile);

                session = _accountService.CurrentSession;
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(ex.Message, "Помилка входу", MascotEmotion.Sad);
            }
        }

        private void UpdateSavedIndex(ProfileItem currentProfile = null)
        {
            var allProfiles = _accountService.GetProfiles();

            if (currentProfile == null)
            {
                return;
            }

            int index = allProfiles.FindIndex(p => p.UUID == currentProfile.UUID);
            Settings1.Default.SelectIndexAccount = index;
            Settings1.Default.Save();
        }
        private async void CreateAccount_Offline_Click(object sender, RoutedEventArgs e)
        {
            Click(); 

            if (string.IsNullOrWhiteSpace(NameNikManeger.Text)) return;

            try
            {
                await _accountService.AddOfflineAccountAsync(NameNikManeger.Text);

                NameNikManeger.Text = null;
                CloseAccountSelectionUI();

                await LoadProfilesAsync();
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show($"Не вдалося створити профіль: {ex.Message}", "Помилка", MascotEmotion.Sad);
            }
        }
        private async void MicrosoftLoginButton_Click(object sender, RoutedEventArgs e)
        {
            Click();
            try
            {
                await _accountService.AddMicrosoftAccountAsync();

                Settings1.Default.MicrosoftAccount = true;
                Settings1.Default.Save();

                await LoadProfilesAsync();
                MascotMessageBox.Show("Вхід успішний!", "Ура", MascotEmotion.Happy);
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(ex.Message, "Помилка", MascotEmotion.Sad);
            }
            finally
            {
                CloseAccountSelectionUI();
            }
        }
        private async void LoginAccountLittleSkin_Click(object sender, RoutedEventArgs e)
        {
            Click();

            try
            {
                await _accountService.AddLittleSkinAccountAsync(Login_LittleSkin.Text, PasswordLittleSkin.Password); 

                Login_LittleSkin.Text = null;
                PasswordLittleSkin.Password = null;

                CloseAccountSelectionUI();

                await LoadProfilesAsync();
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                    $"Ех, біда! Не вдалося підключитися до LittleSkin.\n" +
                    $"Ти впевнений, що ввів правильний логін та пароль?\n\n" +
                    $"Ось що пішло не так: {ex.Message}",
                    "Помилка LittleSkin",
                    MascotEmotion.Sad
                );
            }
        }

        private void CloseAccountSelectionUI()
        {
            AnimationService.FadeOut(GridOfflineMode, 0.2); 
            AnimationService.FadeOut(GridOnlineMode, 0.2);
            AnimationService.FadeOut(GridFormAccountAdd, 0.2);
            AnimationService.FadeOut(GridSelectAccountType, 0.2);
            AnimationService.FadeOut(GridLittleSkinMode, 0.2);
        }

        private void AddProfile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();

            AnimationService.AnimatePageTransition(GridFormAccountAdd);
            AnimationService.AnimatePageTransition(GridOfflineMode);
            AnimationService.AnimatePageTransition(GridSelectAccountType);

            string directoryPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
        }

        private void GirdFormAccountAdd_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();
            if (GridSelectAccountType.Visibility == Visibility.Visible)
            {
                CloseAccountSelectionUI();
            }
            if (SelectCreatePackMinecraft.Visibility == Visibility.Visible)
            {
                AnimationService.AnimatePageTransitionExit(SelectCreatePackMinecraft);
                AnimationService.AnimatePageTransitionExit(GridFormAccountAdd);
            }
        }
        private void MicrosoftAccount_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();
            AnimationService.AnimatePageTransitionExit(GridLittleSkinMode, default, 0.2);
            AnimationService.AnimatePageTransitionExit(GridOfflineMode, default, 0.2);
            AnimationService.AnimatePageTransition(GridOnlineMode);
        }

        private void OfflineAccount_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();
            AnimationService.AnimatePageTransitionExit(GridLittleSkinMode, default, 0.2);
            AnimationService.AnimatePageTransitionExit(GridOnlineMode, default, 0.2);
            AnimationService.AnimatePageTransition(GridOfflineMode);
        }

        private void LittleSkinAccount_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();
            AnimationService.AnimatePageTransitionExit(GridOfflineMode, default, 0.2);
            AnimationService.AnimatePageTransitionExit(GridOnlineMode, default, 0.2);
            AnimationService.AnimatePageTransition(GridLittleSkinMode);
        }

        private void IconAccount_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LoadProfilesAsync();

                if (PanelManegerAccount.Visibility == Visibility.Visible)
                {
                    IconRotateTransform.Angle = 0;
                    AnimationService.AnimatePageTransitionExit(PanelManegerAccount, -20);
                    ListAccount.Items?.Clear();
                }
                else
                {
                    IconRotateTransform.Angle = 180;
                    AnimationService.AnimatePageTransition(PanelManegerAccount, -20);
                }

                if (PanelListStats.Visibility == Visibility.Visible)
                {
                    AnimationService.FadeOut(PanelListStats, 0.3);
                }

                Click();
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show($"Помилка меню акаунтів: {ex.Message}", "Помилка", MascotEmotion.Sad);
            }
        }

        private void StatsTextOpen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Click();
            TextStatsGameMinecraft.Text = _gameSessionManager.GetFormattedStats();
            AnimationService.AnimatePageTransitionExit(PanelManegerAccount, -20, 0.2);
            AnimationService.AnimatePageTransition(PanelListStats, 20);
        }
    }
}