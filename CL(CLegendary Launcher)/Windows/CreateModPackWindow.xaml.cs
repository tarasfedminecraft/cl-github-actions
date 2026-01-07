using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core;
using CurseForge.APIClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CmlLib.Core.Installer.Forge.Versions;
using CurseForge.APIClient.Models.Mods;
using Path = System.IO.Path;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using CmlLib.Core.Installer.NeoForge;
using CL_CLegendary_Launcher_.Class;
using Wpf.Ui.Appearance;
using System.Text.RegularExpressions;
using CmlLib.Core.ModLoaders.LiteLoader;
using Optifine.Installer;

namespace CL_CLegendary_Launcher_.Windows
{

    public partial class CreateModPackWindow : Window
    {
        private readonly ModDownloadService _modDownloadService;
        private readonly ModpackService _modpackService;

        protected byte SelectMod = 0; 
        public bool IsModPackCreated = false;
        public string LoderNow = "Forge";
        private string SiteDowload = "Modrinth";

        private List<ModInfo> ModsList = new();
        private List<ModInfo> ShadersList = new();
        private List<ModInfo> ResourcePacksList = new();
        private List<string> iconUrl = new List<string>();

        private List<ModVersionInfo> _currentModVersions;
        private List<string> fileUrlDowload = new List<string>();
        private HttpClient httpClient = new HttpClient();

        public CreateModPackWindow(ModDownloadService modDownloadService, ModpackService modpackService)
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(
ApplicationTheme.Dark,
Wpf.Ui.Controls.WindowBackdropType.Mica
            );
            ApplicationThemeManager.Apply(this);
            _modDownloadService = modDownloadService;
            _modpackService = modpackService;
        }
        private async void UpdateModsList()
        {
            if (VersionVanil.SelectedItem == null || ListModsList.SelectedItem == null)
                return;

            if (ModsDowloadList.Items != null)
                ModsDowloadList.Items.Clear();

            try
            {
                string searchText = SearchSystemModsTXT.Text;

                var results = await _modDownloadService.SearchModsAsync(
                    searchText,
                    SiteDowload,
                    LoderNow,
                    SelectMod
                );

                if (results == null) return;

                foreach (var mod in results)
                {
                    var item = CreateItemFromSearchResult(mod);
                    ModsDowloadList.Items.Add(item);
                }

                if (VersionVanil.IsEnabled == false && ListModsList.IsEnabled == false)
                    AddVersionList();
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ой леле! Не вдалося завантажити список модів.\nПеревір інтернет або спробуй пізніше.\n\nДеталі: {ex.Message}",
                                    "Помилка завантаження",
                                    MascotEmotion.Sad);
            }
        }

        private ModPackItem CreateItemFromSearchResult(ModSearchResult mod)
        {
            var item = new ModPackItem();
            item.ModPackName.Content = mod.Title;

            string icon = mod.IconUrl;
            if (!string.IsNullOrEmpty(icon))
            {
                iconUrl.Add(icon);
                item.IconModPack.Source = new BitmapImage(new Uri(icon));
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

            item.game_version = VersionVanil.SelectedItem?.ToString();
            item.loaders = LoderNow;
            item.ProjectId = mod.ModId;
            item.Slug = mod.Slug;
            item.Name = mod.Title;
            item.Type = SelectMod; 

            item.AddModInModPack.Visibility = Visibility.Visible;

            item.AddModInModPack.MouseDown += async (s, e) =>
            {
                AnimationService.FadeIn(GirdModsDowload, 0.3);
                AnimationService.FadeIn(MenuInstaller, 0.3);

                await LoadCompatibleVersions(mod);
            };

            return item;
        }
        private async Task LoadCompatibleVersions(ModSearchResult mod)
        {
            VersionMods.Items.Clear();
            fileUrlDowload.Clear();
            _currentModVersions = null;

            if (VersionVanil.SelectedItem == null)
            {
                MascotMessageBox.Show(
                                    "Секундочку! Ти забув обрати версію Minecraft.\nЯ ж не знаю, під що шукати моди!",
                                    "Яка версія?",
                                    MascotEmotion.Alert); return;
            }
            string selectedGameVersion = VersionVanil.SelectedItem.ToString();

            try
            {
                var allVersions = await _modDownloadService.GetVersionsAsync(mod);

                if (allVersions == null || !allVersions.Any())
                {
                    MascotMessageBox.Show(
                                            $"Дивина! Я не знайшла жодної доступної версії для \"{mod.Title}\".",
                                            "Пусто",
                                            MascotEmotion.Confused);
                    return;
                }

                List<ModVersionInfo> filteredVersions;

                if (SelectMod == 0) 
                {
                    filteredVersions = allVersions
                        .Where(v => v.GameVersions.Contains(selectedGameVersion) &&
                                    v.Loaders.Any(l => l.Equals(LoderNow, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
                else 
                {
                    filteredVersions = allVersions
                        .Where(v => v.GameVersions.Contains(selectedGameVersion))
                        .ToList();
                }

                if (!filteredVersions.Any())
                {
                    MascotMessageBox.Show(
                                            $"Ех, \"{mod.Title}\" не має сумісних файлів для Minecraft {selectedGameVersion}.\nСпробуй іншу версію гри.",
                                            "Несумісність",
                                            MascotEmotion.Sad);
                    return;
                }

                _currentModVersions = filteredVersions;

                foreach (var version in filteredVersions)
                {
                    VersionMods.Items.Add(version.VersionName);
                    fileUrlDowload.Add(version.DownloadUrl);
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ой! Щось пішло не так під час обробки мода \"{mod.Title}\".\n\nПомилка: {ex.Message}",
                                    "Збій",
                                    MascotEmotion.Sad);
            }
        }

        private void DowloadTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(NameModPackTXT.Text)) {
                AnimationService.FadeOut(GirdModsDowload, 0.3);
                AnimationService.FadeOut(MenuInstaller, 0.3);
                MascotMessageBox.Show(
                                    "А як ми назвемо це чудо?\nВведи назву збірки, будь ласка!",
                                    "Назва?",
                                    MascotEmotion.Alert);
                return; 
            }

            if (VersionMods.SelectedItem != null && _currentModVersions != null)
            {
                var selectedIndex = VersionMods.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex < _currentModVersions.Count)
                {
                    var selectedVerInfo = _currentModVersions[selectedIndex];

                    var modInfo = new ModInfo
                    {
                        Name = (ModsDowloadList.SelectedItem as ModPackItem)?.Name + ":" + selectedVerInfo.VersionName,
                        ProjectId = selectedVerInfo.ModId,
                        Loader = ListModsList.SelectedItem?.ToString() ?? LoderNow,
                        Version = VersionVanil.SelectedItem.ToString(),
                        Url = selectedVerInfo.DownloadUrl,
                        LoaderType = LoderNow,
                        Type = SelectMod switch { 0 => "mod", 1 => "shader", 2 => "resourcepack", _ => "mod" },
                        ImageURL = (ModsDowloadList.SelectedItem as ModPackItem)?.IconModPack?.Source.ToString()
                    };

                    SaveModToModpackJson(modInfo);
                }

                AnimationService.FadeOut(GirdModsDowload, 0.3);
                AnimationService.FadeOut(MenuInstaller, 0.3);
            }
            else
            {
                MascotMessageBox.Show(
                                    "Тицьни на потрібну версію зі списку, щоб я знала, що качати!",
                                    "Обери файл",
                                    MascotEmotion.Alert);
            }
        }

        private void SaveModToModpackJson(ModInfo mod)
        {
            try
            {
                switch (mod.Type)
                {
                    case "mod": ModsList.Add(mod); break;
                    case "shader": ShadersList.Add(mod); break;
                    case "resourcepack": ResourcePacksList.Add(mod); break;
                }

                string modpackName = NameModPackTXT.Text.Trim();
                if (string.IsNullOrWhiteSpace(modpackName) || modpackName == "Введіть назву збірки")
                {
                    MascotMessageBox.Show(
                                            "Будь ласка, введи назву збірки перед додаванням модів.",
                                            "Назва?",
                                            MascotEmotion.Alert);
                    return;
                }

                string modpackFolder = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", modpackName);
                Directory.CreateDirectory(modpackFolder);
                string jsonPath = Path.Combine(modpackFolder, "modpack.json");

                List<ModInfo> modList = new();
                if (File.Exists(jsonPath))
                {
                    try
                    {
                        string existingJson = File.ReadAllText(jsonPath);
                        modList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ModInfo>>(existingJson) ?? new List<ModInfo>();
                    }
                    catch { }
                }

                if (!modList.Any(m => m.Name == mod.Name))
                {
                    modList.Add(mod);

                    AddItemModPack moditem = new AddItemModPack();
                    moditem.NameMod.Text = mod.Name;
                    try { moditem.IconMod.Source = new BitmapImage(new Uri(mod.ImageURL)); } catch { }

                    moditem.DeleteModFromModPack.MouseDown += (s, e) =>
                    {
                        AddModsInModPackList.Items.Remove(moditem);

                        if (!File.Exists(jsonPath)) return;

                        var existingJson = File.ReadAllText(jsonPath);
                        var mods = JsonSerializer.Deserialize<List<ModInfo>>(existingJson) ?? new List<ModInfo>();

                        mods = mods.Where(m => m.Name != moditem.NameMod.Text).ToList();

                        string updatedJson = JsonSerializer.Serialize(mods, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(jsonPath, updatedJson);
                    };

                    string newJson = Newtonsoft.Json.JsonConvert.SerializeObject(modList, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(jsonPath, newJson);

                    MascotMessageBox.Show(
                                            $"Ура! Мод \"{mod.Name}\" успішно додано!",
                                            "Готово",
                                            MascotEmotion.Happy);
                    AddModsInModPackList.Items.Add(moditem);
                }
                else
                {
                    MascotMessageBox.Show(
                                            $"Чекай-но! Мод \"{mod.Name}\" вже є в цій збірці.",
                                            "Вже є",
                                            MascotEmotion.Alert);
                }

                VersionVanil.IsEnabled = false;
                ListModsList.IsEnabled = false;
                NameModPackTXT.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Біда! Не вдалося додати мод.\n{ex.Message}",
                                    "Помилка",
                                    MascotEmotion.Sad);
            }
        }
        private void CreateModPacksButtonTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string modpackName = NameModPackTXT.Text.Trim();
                if (string.IsNullOrWhiteSpace(modpackName) || modpackName == "Введіть назву збірки")
                {
                    MascotMessageBox.Show(
                                            "Ей, ти забув дати ім'я нашій збірці! Введи назву.",
                                            "Ім'я?",
                                            MascotEmotion.Alert);
                    return;
                }

                string basePath = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", modpackName);
                string pathJson = Path.Combine(basePath, "modpack.json");

                if (!Directory.Exists(basePath) || !File.Exists(pathJson))
                {
                    MascotMessageBox.Show(
                                            "Хм, збірка порожня або ще не створена. Додай хоча б один мод!",
                                            "Пусто",
                                            MascotEmotion.Confused);
                    return;
                }

                string imageUrl = "pack://application:,,,/Icon/IconCL(Common).png";
                if (iconUrl.Count > 0) imageUrl = iconUrl[0];

                var modpack = new InstalledModpack
                {
                    Name = modpackName,
                    TypeSite = "Custom",
                    MinecraftVersion = VersionVanil.SelectedItem?.ToString() ?? "???",
                    LoaderType = LoderNow,
                    LoaderVersion = ListModsList.SelectedItem?.ToString() ?? "???",
                    Path = basePath,
                    PathJson = pathJson,
                    UrlImage = imageUrl
                };

                _modpackService.AddModpack(modpack);

                MascotMessageBox.Show(
                                    "Чудово! Модпак успішно створено і збережено.\nМожна грати!",
                                    "Успіх!",
                                    MascotEmotion.Happy);
                IsModPackCreated = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ой леле! Сталася помилка при фінальному створенні модпаку:\n{ex.Message}",
                                    "Критичний збій",
                                    MascotEmotion.Sad);
            }
        }
        private void LoadModsByType(string type)
        {
            AddModsInModPackList.Items.Clear(); 

            List<ModInfo> sourceList = type switch
            {
                "mod" => ModsList,
                "shader" => ShadersList,
                "resourcepack" => ResourcePacksList,
                _ => new List<ModInfo>()
            };

            string modpackName = NameModPackTXT.Text.Trim();
            string modpackFolder = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", modpackName);
            string jsonPath = Path.Combine(modpackFolder, "modpack.json");

            foreach (var mod in sourceList)
            {
                AddItemModPack modItem = new AddItemModPack();
                modItem.NameMod.Text = mod.Name;

                if (!string.IsNullOrEmpty(mod.ImageURL) && Uri.IsWellFormedUriString(mod.ImageURL, UriKind.Absolute))
                {
                    modItem.IconMod.Source = new BitmapImage(new Uri(mod.ImageURL));
                }
                else
                {
                    modItem.IconMod.Source = new BitmapImage(new Uri("pack://application:,,,/Icon/IconCL(Common).png"));
                }
                modItem.DeleteModFromModPack.MouseDown += (s, e) =>
                {
                    AddModsInModPackList.Items.Remove(modItem);

                    switch (type)
                    {
                        case "mod":
                            ModsList.Remove(mod);
                            break;
                        case "shader":
                            ShadersList.Remove(mod);
                            break;
                        case "resourcepack":
                            ResourcePacksList.Remove(mod);
                            break;
                    }

                    if (File.Exists(jsonPath))
                    {
                        try
                        {
                            string existingJson = File.ReadAllText(jsonPath);
                            var mods = JsonSerializer.Deserialize<List<ModInfo>>(existingJson) ?? new List<ModInfo>();

                            mods = mods.Where(m => m.Name != mod.Name).ToList();

                            string updatedJson = JsonSerializer.Serialize(mods, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(jsonPath, updatedJson);
                        }
                        catch (Exception ex)
                        {
                            MascotMessageBox.Show(
                                                            $"Ой! Помилка при оновленні JSON файлу: {ex.Message}",
                                                            "Збій збереження",
                                                            MascotEmotion.Sad);
                        }
                    }
                };

                AddModsInModPackList.Items.Add(modItem);
            }
        }
        private void ModrinthSite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ModsDowloadList.Items != null) ModsDowloadList.Items.Clear();
            if (VersionVanil.Items != null && VersionVanil.IsEnabled == true) VersionVanil.Items.Clear();
            if (ListModsList.Items != null && ListModsList.IsEnabled == true) ListModsList.Items.Clear();

            SiteDowload = "Modrinth";
            AnimationService.AnimateBorderObject(0, 0, SelectSiteModPacksNow, true);
            UpdateModsList();
        }

        private void CurseForgeSite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ModsDowloadList.Items != null) ModsDowloadList.Items.Clear();
            if (VersionVanil.Items != null && VersionVanil.IsEnabled == true) VersionVanil.Items.Clear();
            if (ListModsList.Items != null && ListModsList.IsEnabled == true) ListModsList.Items.Clear();

            SiteDowload = "CurseForge";
            AnimationService.AnimateBorderObject(30, 0, SelectSiteModPacksNow, true);
            UpdateModsList();
        }
        private void NameModPackTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (NameModPackTXT.Text == "Введіть назву збірки")
            {
                NameModPackTXT.Text = "";
            }
        }

        private void SearchSystemModsTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SearchSystemModsTXT.Text == "Пошук")
            {
                SearchSystemModsTXT.Text = "";
                UpdateModsList();
            }
        }

        private async void VersionVanil_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionVanil.SelectedItem == null)
                return;

            if (ModsDowloadList.Items != null)
                ModsDowloadList.Items.Clear();
            if (ListModsList.Items != null)
                ListModsList.Items.Clear();

            try
            {
                if (LoderNow == "Forge")
                {
                    var versionLoader = new ForgeVersionLoader(httpClient);
                    var forgeList = await versionLoader.GetForgeVersions(VersionVanil.SelectedItem.ToString());

                    foreach (var forge in forgeList)
                        ListModsList.Items.Add(forge.ForgeVersionName);
                }
                else if (LoderNow == "Fabric")
                {
                    var fabricInstaller = new FabricInstaller(httpClient);
                    var fabricVersions = await fabricInstaller.GetLoaders(VersionVanil.SelectedItem.ToString());

                    foreach (var fabric in fabricVersions)
                        ListModsList.Items.Add(fabric.Version);
                }
                else if (LoderNow == "Quilt")
                {
                    var quiltInstaller = new QuiltInstaller(httpClient);
                    var quiltVersions = await quiltInstaller.GetLoaders(VersionVanil.SelectedItem.ToString());

                    foreach (var quilt in quiltVersions)
                        ListModsList.Items.Add(quilt.Version);
                }
                else if (LoderNow == "NeoForge")
                {
                    MinecraftLauncher launcher = new MinecraftLauncher(new MinecraftPath(Settings1.Default.PathLacunher));
                    var versionLoader = new NeoForgeInstaller(launcher);
                    var neoForgeList = await versionLoader.GetForgeVersions(VersionVanil.SelectedItem.ToString());
                    foreach (var neoForge in neoForgeList)
                        ListModsList.Items.Add(neoForge.VersionName);
                }
                else if (LoderNow == "LiteLoader")
                {
                    var liteLoaderInstaller = new LiteLoaderInstaller(new HttpClient());
                    var allLiteLoaders = await liteLoaderInstaller.GetAllLiteLoaders();

                    var compatibleLoaders = allLiteLoaders
                        .Where(loader => loader.BaseVersion == VersionVanil.SelectedItem.ToString())
                        .ToList();

                    foreach (var loader in compatibleLoaders)
                    {
                        ListModsList.Items.Add(loader.Version);
                    }

                    if (ListModsList.Items.Count == 0)
                    {
                        MascotMessageBox.Show(
                                                    $"Для версії {VersionVanil.SelectedItem.ToString()} я не знайшла LiteLoader.",
                                                    "Пусто",
                                                    MascotEmotion.Confused);
                    }
                }
                else if (LoderNow == "Optifine")
                {
                    var loader = new OptifineInstaller(new HttpClient());
                    var versions = await loader.GetOptifineVersionsAsync();

                    var selectedMCVersion = VersionVanil.SelectedItem?.ToString();

                    if (!string.IsNullOrEmpty(selectedMCVersion))
                    {
                        var optifineVersions = versions
                            .Where(v => v.MinecraftVersion == selectedMCVersion)
                            .Select(v => v.Version)
                            .ToList();

                        foreach (var ver in optifineVersions)
                        {
                            ListModsList.Items.Add(ver);
                        }

                        if (ListModsList.Items.Count == 0)
                        {
                            MascotMessageBox.Show(
                                                            "Для цієї версії Minecraft на жаль немає OptiFine.",
                                                            "Сумно",
                                                            MascotEmotion.Sad);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Не змогла завантажити список версій для {LoderNow}.\n\nПомилка: {ex.Message}",
                                    "Збій версій",
                                    MascotEmotion.Sad);
            }
        }
        private async void ListModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateModsList();
        }

        private void SearchSystemModsTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchSystemModsTXT.Text) && SearchSystemModsTXT.Text != "Пошук")
            {
                ModsDowloadList.Items.Clear();
                UpdateModsList();
            }
        }
        private void GirdModsDowload_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AnimationService.FadeOut(GirdModsDowload, 0.3);
            AnimationService.FadeOut(MenuInstaller, 0.3);
            if (fileUrlDowload.Count != 0) fileUrlDowload.Clear();
        }
        private void VersionMods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (IsModPackCreated == false)
            {
                string modpackName = NameModPackTXT.Text.Trim();
                if (string.IsNullOrWhiteSpace(modpackName)) return;

                string modpackFolder = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", modpackName);

                if (Directory.Exists(modpackFolder))
                {
                    try
                    {
                        Directory.Delete(modpackFolder, true);
                        MascotMessageBox.Show(
                            $"Я прибрала за собою. Тимчасовий модпак \"{modpackName}\" видалено.",
                            "Очистка",
                            MascotEmotion.Normal);
                    }
                    catch (Exception ex)
                    {
                        MascotMessageBox.Show(
                            $"❌ Ой, не вдалося видалити тимчасові файли: {ex.Message}",
                            "Помилка",
                            MascotEmotion.Sad);
                    }
                }
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

        private void ResourcePackTxt_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AnimationService.AnimateBorderObject(100, 0, PanelSelectNow, true);
            SelectMod = 2;
            if (VersionVanil.SelectedItem != null && ListModsList.SelectedItem != null) { UpdateModsList(); }
            if (ResourcePacksList != null) { LoadModsByType("resourcepack"); }
        }

        private void ModsTxt_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AnimationService.AnimateBorderObject(0, 0, PanelSelectNow, true);
            SelectMod = 0;
            if (VersionVanil.SelectedItem != null && ListModsList.SelectedItem != null) { UpdateModsList(); }
            if (ModsList != null) { LoadModsByType("mod"); }
        }

        private void ShaderPackTxt_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AnimationService.AnimateBorderObject(210, 0, PanelSelectNow, true);
            SelectMod = 1;
            if (VersionVanil.SelectedItem != null && ListModsList.SelectedItem != null) {; UpdateModsList(); }
            if (ShadersList != null) { LoadModsByType("shader"); }
        }
        private async void AddVersionList()
        {
            if (VersionVanil.Items != null && VersionVanil.IsEnabled == true) VersionVanil.Items.Clear();
            if (ListModsList.Items != null && ListModsList.IsEnabled == true) ListModsList.Items.Clear();

            try
            {
                var path = new MinecraftPath(Settings1.Default.PathLacunher);
                var launcher = new MinecraftLauncher(path);
                var httpClient = new HttpClient();

                if (LoderNow == "Fabric")
                {
                    var fabricInstaller = new FabricInstaller(httpClient);
                    var versions = await fabricInstaller.GetSupportedVersionNames();

                    foreach (var version in versions)
                        VersionVanil.Items.Add(version);

                    return;
                }

                if (LoderNow == "Quilt")
                {
                    var quiltInstaller = new QuiltInstaller(httpClient);
                    var versions = await quiltInstaller.GetSupportedVersionNames();

                    foreach (var version in versions)
                        VersionVanil.Items.Add(version);

                    return;
                }

                var allVersions = await launcher.GetAllVersionsAsync();

                if (LoderNow == "Forge")
                {
                    foreach (var ver in allVersions)
                    {
                        if (ver.Type == "release")
                        {
                            VersionVanil.Items.Add(ver.Name);
                            if (ver.Name == "1.7.10") break;
                        }
                    }
                }
                else if (LoderNow == "NeoForge")
                {
                    foreach (var ver in allVersions)
                    {
                        if (ver.Type == "release")
                        {
                            VersionVanil.Items.Add(ver.Name);
                            if (ver.Name == "1.20.2") break;
                        }
                    }
                }
                else if (LoderNow == "LiteLoader")
                {
                    Version minVer = new Version(1, 5, 2);
                    Version maxVer = new Version(1, 12, 2);

                    var liteLoaderVersions = allVersions
                        .Where(v => v.Type == "release") 
                        .Select(v => new { Name = v.Name, Ver = ParseGameVersion(v.Name) }) 
                        .Where(v => v.Ver >= minVer && v.Ver <= maxVer) 
                        .OrderByDescending(v => v.Ver) 
                        .ToList();

                    foreach (var ver in liteLoaderVersions)
                    {
                        VersionVanil.Items.Add(ver.Name);
                    }
                }
                else if (LoderNow == "Optifine")
                {
                    foreach (var ver in allVersions)
                    {
                        if (ver.Type == "release")
                        {
                            VersionVanil.Items.Add(ver.Name);
                            if (ver.Name == "1.7.2") break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Помилка при завантаженні версій {LoderNow}: {ex.Message}",
                                    "Помилка",
                                    MascotEmotion.Sad);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string content = (sender as MenuItem).Header.ToString();
            switch (content)
            {
                case "Forge":
                    LoderNow = "Forge";
                    LoaderDropDownButton.Content = content;
                    AddVersionList();
                    break;
                case "Fabric":
                    LoderNow = "Fabric";
                    LoaderDropDownButton.Content = content;
                    AddVersionList();
                    break;
                case "Quilt":
                    LoderNow = "Quilt";
                    LoaderDropDownButton.Content = content;
                    AddVersionList();
                    break;
                case "NeoForge":
                    LoderNow = "NeoForge";
                    LoaderDropDownButton.Content = content;
                    AddVersionList();
                    break;
                case "LiteLoader":
                    LoderNow = "LiteLoader";
                    LoaderDropDownButton.Content = content;
                    AddVersionList();
                    break;
                case "Optifine":
                    LoderNow = "Optifine";
                    LoaderDropDownButton.Content = content;
                    AddVersionList();
                    break;
            }
        }
        private System.Version ParseGameVersion(string versionStr)
        {
            try
            {
                var cleanStr = Regex.Match(versionStr, @"^[0-9\.]+").Value;
                var parts = cleanStr.Split('.');
                if (parts.Length == 2) cleanStr += ".0";
                else if (parts.Length == 1 && !string.IsNullOrEmpty(cleanStr)) cleanStr += ".0.0";

                if (System.Version.TryParse(cleanStr, out var version))
                    return version;
            }
            catch { }
            return new System.Version(0, 0, 0);
        }
    }
}
