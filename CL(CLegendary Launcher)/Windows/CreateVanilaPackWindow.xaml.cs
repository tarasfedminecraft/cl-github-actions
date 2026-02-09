using CL_CLegendary_Launcher_.Class;
using CmlLib.Core;
using CurseForge.APIClient;
using CurseForge.APIClient.Models.Mods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using static CL_CLegendary_Launcher_.Windows.ModJsonManager;
using Path = System.IO.Path;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class CreateVanilaPackWindow : FluentWindow
    {
        private readonly ModDownloadService _modDownloadService;
        private readonly ModpackService _modpackService;

        private List<ModInfo> _tempResourcePacks = new List<ModInfo>();

        protected byte SelectMod = 2; 
        private string SiteDowload = "Modrinth";
        public bool IsModPackCreated = false;

        private List<string> iconUrl = new List<string>();

        private int _currentPage = 0;
        private const int ITEMS_PER_PAGE = 10;
        private CancellationTokenSource _searchCts;

        private ModSearchResult _currentModToInstall;
        private List<ModVersionInfo> _currentAvailableVersions;

        private static readonly HttpClient httpClient = new HttpClient();

        public CreateVanilaPackWindow(ModDownloadService modDownloadService, ModpackService modpackService)
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);

            _modDownloadService = modDownloadService;
            _modpackService = modpackService;

            AddVersionList();

            UpdateWindowBackdrop();
        }
        private void UpdateWindowBackdrop()
        {
            bool glassDisabled = Settings1.Default.DisableGlassEffect;
            if (!glassDisabled)
            {
                this.WindowBackdropType = WindowBackdropType.Mica;
                this.Background = System.Windows.Media.Brushes.Transparent;
            }
            else
            {
                this.WindowBackdropType = WindowBackdropType.None;
                this.SetResourceReference(Control.BackgroundProperty, "MainBackgroundBrush");
            }
        }
        private async void AddVersionList()
        {
            VersionVanilBox.Items.Clear();

            try
            {
                var path = new MinecraftPath(Settings1.Default.PathLacunher);
                var launcher = new MinecraftLauncher(path);
                var allVersions = await launcher.GetAllVersionsAsync();

                var releaseVersions = allVersions
                    .Where(v => v.Type == "release")
                    .ToList();

                foreach (var ver in releaseVersions)
                {
                    VersionVanilBox.Items.Add(ver.Name);
                }

                if (VersionVanilBox.Items.Count > 0)
                    VersionVanilBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError($"Помилка завантаження версій: {ex.Message}");
            }
        }

        private async void UpdateModsList()
        {
            if (VersionVanilBox.SelectedItem == null) return;

            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            ModsDowloadList.Items.Clear();
            ModsSearchLoader.Visibility = Visibility.Visible;
            ModsDowloadList.Visibility = Visibility.Collapsed;

            if (PageNumberText != null) PageNumberText.Text = (_currentPage + 1).ToString();
            if (PrevPageBtn != null) PrevPageBtn.IsEnabled = _currentPage > 0;
            if (NextPageBtn != null) NextPageBtn.IsEnabled = false;

            try
            {
                string searchText = SearchSystemModsTXT.Text;
                if (searchText == "Пошук ресурс-паків...") searchText = "";

                int offset = _currentPage * ITEMS_PER_PAGE;

                var results = await _modDownloadService.SearchModsAsync(
                    searchText,
                    SiteDowload,
                    "Vanilla",
                    SelectMod,
                    offset
                );

                if (token.IsCancellationRequested) return;

                if (results == null || !results.Any()) return;

                if (NextPageBtn != null)
                    NextPageBtn.IsEnabled = results.Count >= ITEMS_PER_PAGE;

                foreach (var mod in results)
                {
                    var item = CreateItemFromSearchResult(mod);
                    ModsDowloadList.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching resource packs: {ex.Message}");
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    ModsSearchLoader.Visibility = Visibility.Collapsed;
                    ModsDowloadList.Visibility = Visibility.Visible;
                }
            }
        }

        private ModPackItem CreateItemFromSearchResult(ModSearchResult mod)
        {
            var item = new ModPackItem();
            item.ModPackName.Text = mod.Title;

            if (!string.IsNullOrEmpty(mod.IconUrl))
            {
                try { item.IconModPack.Source = new BitmapImage(new Uri(mod.IconUrl)); } catch { }
                iconUrl.Add(mod.IconUrl);
            }
            else
            {
                item.IconModPack.Source = new BitmapImage(new Uri("pack://application:,,,/Icon/IconCL(Common).png"));
            }

            item.MouseDoubleClick += (s, e) =>
            {
                string baseUrl = mod.Site == "Modrinth" ? "https://modrinth.com" : "https://www.curseforge.com/minecraft";
                string category = mod.Site == "Modrinth"
                    ? (SelectMod == 1 ? "shader" : SelectMod == 2 ? "resourcepack" : "mod")
                    : (SelectMod == 1 ? "shaders" : SelectMod == 2 ? "texture-packs" : "mc-mods");
                WebHelper.OpenUrl($"{baseUrl}/{category}/{mod.Slug}");
            };

            item.AddModInModPack.Visibility = Visibility.Visible;
            item.AddModInModPack.MouseDown += async (s, e) =>
            {
                await OpenVersionSelector(mod);
            };

            return item;
        }

        private async Task OpenVersionSelector(ModSearchResult mod)
        {
            if (VersionVanilBox.SelectedItem == null)
            {
                MascotMessageBox.Show("Спочатку оберіть версію Minecraft!", "Увага", MascotEmotion.Alert);
                return;
            }

            _currentModToInstall = mod;
            GameVersionMinecraft_Копировать.Text = mod.Title;

            MenuInstaller.Visibility = Visibility.Visible;
            VersionMods.Items.Clear();

            string selectedGameVersion = VersionVanilBox.SelectedItem.ToString();

            try
            {
                var allVersions = await _modDownloadService.GetVersionsAsync(mod);

                _currentAvailableVersions = allVersions
                    .Where(v => v.GameVersions.Contains(selectedGameVersion))
                    .ToList();

                if (_currentAvailableVersions.Any())
                {
                    foreach (var v in _currentAvailableVersions)
                    {
                        VersionMods.Items.Add(v.VersionName);
                    }
                    VersionMods.SelectedIndex = 0;
                }
                else
                {
                    MascotMessageBox.Show("Не знайдено ресурс-паків для цієї версії гри.", "Пусто", MascotEmotion.Confused);
                    MenuInstaller.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show($"Помилка: {ex.Message}", "Помилка", MascotEmotion.Sad);
                MenuInstaller.Visibility = Visibility.Hidden;
            }
        }

        private void DowloadTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (VersionMods.SelectedIndex == -1 || _currentModToInstall == null || _currentAvailableVersions == null) return;

            int index = VersionMods.SelectedIndex;
            if (index >= _currentAvailableVersions.Count) return;

            var selectedVerInfo = _currentAvailableVersions[index];

            var modInfo = new ModInfo
            {
                Name = _currentModToInstall.Title,
                ProjectId = selectedVerInfo.ModId,
                Loader = "Vanilla",
                Version = VersionVanilBox.SelectedItem.ToString(),
                Url = selectedVerInfo.DownloadUrl,
                LoaderType = "Vanilla",
                Type = "resourcepack",
                ImageURL = _currentModToInstall.IconUrl
            };

            AddResourcePackToMemory(modInfo);
            MenuInstaller.Visibility = Visibility.Hidden;
        }

        private void CloseMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MenuInstaller.Visibility = Visibility.Hidden;
        }

        private void AddResourcePackToMemory(ModInfo mod)
        {
            try
            {
                if (_tempResourcePacks.Any(m => m.ProjectId == mod.ProjectId))
                {
                    MascotMessageBox.Show("Цей ресурс-пак вже є в списку.", "Увага", MascotEmotion.Alert);
                    return;
                }

                _tempResourcePacks.Add(mod);
                AddItemToRightList(mod);

                MascotMessageBox.Show($"Ресурс-пак \"{mod.Name}\" додано!", "Успіх", MascotEmotion.Happy);

                VersionVanilBox.IsEnabled = false;
            }
            catch (Exception ex)
            {
                ShowError($"Помилка додавання: {ex.Message}");
            }
        }
        private void AddItemToRightList(ModInfo mod)
        {
            AddItemModPack moditem = new AddItemModPack();
            moditem.NameMod.Text = mod.Name;
            try { moditem.IconMod.Source = new BitmapImage(new Uri(mod.ImageURL ?? "pack://application:,,,/Icon/IconCL(Common).png")); } catch { }

            moditem.DeleteModFromModPack.MouseDown += (s, e) =>
            {
                AddModsInModPackList.Items.Remove(moditem);

                var itemToRemove = _tempResourcePacks.FirstOrDefault(m => m.ProjectId == mod.ProjectId);
                if (itemToRemove != null)
                {
                    _tempResourcePacks.Remove(itemToRemove);
                }

                if (_tempResourcePacks.Count == 0)
                {
                    VersionVanilBox.IsEnabled = true;
                }
            };

            AddModsInModPackList.Items.Add(moditem);
        }
        private void CreateModPacksButtonTXT_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                string modpackName = NameModPackTXT.Text.Trim();
                if (string.IsNullOrWhiteSpace(modpackName))
                {
                    ShowError("Введіть назву збірки!");
                    return;
                }

                string basePath = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", modpackName);
                string pathJson = Path.Combine(basePath, "modpack.json");

                Directory.CreateDirectory(basePath);

                string newJson = JsonConvert.SerializeObject(_tempResourcePacks, Formatting.Indented);
                File.WriteAllText(pathJson, newJson);

                string imageUrl = iconUrl.FirstOrDefault() ?? "pack://application:,,,/Icon/IconCL(Common).png";

                var modpack = new InstalledModpack
                {
                    Name = modpackName,
                    TypeSite = "Custom",
                    MinecraftVersion = VersionVanilBox.SelectedItem?.ToString(),
                    LoaderType = "Vanilla",
                    LoaderVersion = "Latest",
                    Path = basePath,
                    PathJson = pathJson,
                    UrlImage = imageUrl
                };

                _modpackService.AddModpack(modpack);

                MascotMessageBox.Show("Ванільну збірку створено!", "Успіх", MascotEmotion.Happy);
                IsModPackCreated = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"Помилка створення: {ex.Message}");
            }
        }
        private void VersionVanil_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionVanilBox.SelectedItem != null)
            {
                _currentPage = 0;
                UpdateModsList();
            }
        }

        private void SearchSystemModsTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            _currentPage = 0;
            UpdateModsList();
        }

        private void PrevPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                UpdateModsList();
            }
        }

        private void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            UpdateModsList();
        }

        private void ModrinthSite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SiteDowload == "Modrinth") return;

            SiteDowload = "Modrinth";
            _currentPage = 0;

            UpdateProviderUI();
            UpdateModsList();
        }

        private void CurseForgeSite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SiteDowload == "CurseForge") return;

            SiteDowload = "CurseForge";
            _currentPage = 0;

            UpdateProviderUI();
            UpdateModsList();
        }
        private void UpdateProviderUI()
        {
            if (SiteDowload == "Modrinth")
            {
                ModrinthSite.Opacity = 1.0;
                CurseForgeSite.Opacity = 0.5;
            }
            else
            {
                ModrinthSite.Opacity = 0.5;
                CurseForgeSite.Opacity = 1.0;
            }
        }
        private void Provider_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.Image img)
            {
                if (img.Name == "ModrinthSite" && SiteDowload != "Modrinth")
                {
                    img.Opacity = 0.8;
                }
                else if (img.Name == "CurseForgeSite" && SiteDowload != "CurseForge")
                {
                    img.Opacity = 0.8;
                }
            }
        }
        private void Provider_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.Image img)
            {
                if (img.Name == "ModrinthSite" && SiteDowload != "Modrinth")
                {
                    img.Opacity = 0.5;
                }
                else if (img.Name == "CurseForgeSite" && SiteDowload != "CurseForge")
                {
                    img.Opacity = 0.5;
                }
            }
        }
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        private void ShowError(string msg) => MascotMessageBox.Show(msg, "Помилка", MascotEmotion.Sad);
        private void ExitLauncher_MouseDown(object sender, RoutedEventArgs e) => this.Close();

        private void Window_Closed(object sender, EventArgs e)
        {
        }
    }
}