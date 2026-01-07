using CL_CLegendary_Launcher_.Class;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class LoadScreen : Window
    {
        private List<string> RandomPhrases = new List<string>
        {
            "Сніг приємно рипить під ногами...",
            "Запарюємо какао з маршмелоу...",
            "Homka ліпить ідеального сніговика...",
            "Deeplay ловить сніжинки на камеру ❄️...",
            "Мороз малює візерунки на вікнах...",
            "Час закутатись у теплий плед...",
            "Пахне мандаринами та ялинкою...",
            "Вулиці сяють святковими вогнями...",
            "Данило перевіряє запаси феєрверків...",
            "WER_Clegendary шукає подарунки під ялинкою...",
            "Зимова казка вже за вікном...",
            "Гріємо руки об горнятко чаю...",
            "Сніжинки танцюють у світлі ліхтарів...",
            "Час передивлятися 'Сам у дома'...",
            "Крижане повітря бадьорить...",
            "Готуємось до новорічного дива...",
            "Всі чекають на перший сніг (або вже копають)...",
            "Холодно на вулиці, тепло на душі..."
        };

        private Random _random = new Random();
        private readonly string versionLauncher = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        public LoadScreen()
        {
            InitializeComponent();

            LoadCustomPhrases();
            ApplyCustomSettings();

            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this, WindowBackdropType.Mica);
            VersionLauncherTXT.Content = versionLauncher + "-Beta";

            Settings1.Default.OfflineModLauncher = false;
            Settings1.Default.Save();

            Loaded += LoadScreen_Loaded;
        }

        private void LoadCustomPhrases()
        {
            try
            {
                string phrasesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Data", "loading_phrases.txt");
                if (File.Exists(phrasesPath))
                {
                    var lines = File.ReadAllLines(phrasesPath)
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .Where(x => !x.Trim().StartsWith("//"))
                                    .Where(x => !x.Trim().StartsWith("#"))
                                    .ToList();

                    if (lines.Count > 0)
                    {
                        RandomPhrases = lines;
                    }
                }
            }
            catch { }
        }
        private void ApplyCustomSettings()
        {
            string bgPath = Settings1.Default.LoadScreenBackground;
            if (!string.IsNullOrEmpty(bgPath) && File.Exists(bgPath))
            {
                try
                {
                    BG.Source = new BitmapImage(new Uri(bgPath));
                }
                catch { }
            }

            string colorHex = Settings1.Default.LoadScreenBarColor;
            if (!string.IsNullOrEmpty(colorHex))
            {
                try
                {
                    var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex);
                    LoadingProgressBar.Foreground = brush;
                }
                catch { }
            }
        }

        private async void LoadScreen_Loaded(object sender, RoutedEventArgs e)
        {
            await UpgradeSettings();

            await RunStartupProcessAsync();
        }

        private async Task RunStartupProcessAsync()
        {
            await DiscordController.Initialize("В віконці завантаження");

            var animationTask = SimulateLoadingAnimationAsync();

            bool updateAvailable = false;

            try
            {
                updateAvailable = await CheckForUpdatesAsync();
            }
            catch (Exception)
            {
                var result = MascotMessageBox.Ask(
                    "Ех, не вийшло перевірити оновлення. Спробувати офлайн?",
                    "Помилка оновлення",
                    MascotEmotion.Sad
                );

                if (result == true)
                {
                    Settings1.Default.OfflineModLauncher = true;
                    Settings1.Default.Save();
                    updateAvailable = false;
                }
                else
                {
                    this.Close();
                    return;
                }
            }

            if (updateAvailable)
            {
                UpdaterWindow updater = new UpdaterWindow();
                updater.Show();
                this.Close();
                return;
            }

            try
            {
                bool eulaAccepted = await CheckEulaAsync();

                if (!eulaAccepted)
                {
                    DiscordController.Deinitialize();
                    Application.Current.Shutdown();
                    return;
                }

                await animationTask;
                OpenMainWindow();

            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                    $"Критичний збій запуску:\n{ex.Message}",
                    "Error",
                    MascotEmotion.Dead
                );
                this.Close();
            }
        }

        private async Task<bool> CheckForUpdatesAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("CL-Launcher");
                client.Timeout = TimeSpan.FromSeconds(5);

                string json = await client.GetStringAsync("https://raw.githubusercontent.com/WER-CORE/CL-Win-Edition--Update/main/update.json");

                var info = JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (info != null && !string.IsNullOrEmpty(info.version))
                {
                    if (versionLauncher.Trim() != info.version.Trim())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task SimulateLoadingAnimationAsync()
        {
            LoadingProgressBar.Value = 0;
            RandomPhraseText.Text = RandomPhrases[_random.Next(RandomPhrases.Count)];

            for (int i = 0; i <= 100; i++)
            {
                if (!this.IsVisible) return;

                await Task.Delay(_random.Next(30, 70));

                DoubleAnimation progressAnimation = new DoubleAnimation
                {
                    From = LoadingProgressBar.Value,
                    To = i,
                    Duration = TimeSpan.FromMilliseconds(100)
                };
                LoadingProgressBar.BeginAnimation(ProgressBar.ValueProperty, progressAnimation);

                if (i % 20 == 0 && i > 0)
                {
                    string randomPhrase = RandomPhrases[_random.Next(RandomPhrases.Count)];
                    RandomPhraseText.Text = randomPhrase;
                }
            }
        }

        private void OpenMainWindow()
        {
            DiscordController.Deinitialize();
            var mainWindow = new CL_Main_();
            mainWindow.Show();
            this.Close();
        }

        private async Task<bool> CheckEulaAsync()
        {
            var eulaConfig = await EulaService.GetEulaAsync();

            bool showEula = false;

            if (eulaConfig != null)
            {
                if (EulaService.IsEulaOutdated(eulaConfig.LastUpdated))
                {
                    showEula = true;
                }
            }
            else
            {
                if (Settings1.Default.EulaAcceptedDate == DateTime.MinValue)
                {
                    showEula = true;
                }
            }

            if (showEula)
            {
                this.Visibility = Visibility.Hidden;

                EulaWindow eulaWin = new EulaWindow(eulaConfig);
                bool? result = eulaWin.ShowDialog();

                this.Visibility = Visibility.Visible;

                return result == true;
            }

            return true;
        }
        private async Task UpgradeSettings()
        {
            try
            {
                if (Settings1.Default.CallUpgrade)
                {
                    Settings1.Default.Upgrade();
                    Settings1.Default.CallUpgrade = false;
                    Settings1.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Settings1.Default.Reset();
                Settings1.Default.CallUpgrade = false;
                Settings1.Default.Save();
            }
            return;
        }
    }

    public class UpdateInfo
    {
        public string version { get; set; } = "";
        public string url { get; set; } = "";
    }
}