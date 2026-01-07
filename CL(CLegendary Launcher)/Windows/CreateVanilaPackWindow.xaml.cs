using CL_CLegendary_Launcher_.Class;
using CmlLib.Core;
using CurseForge.APIClient;
using CurseForge.APIClient.Models.Mods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Appearance;
using Path = System.IO.Path;

namespace CL_CLegendary_Launcher_.Windows
{
    public class ResourcePackInfo
    {
        public string Name { get; set; }
        public string ProjectId { get; set; }
        public string Loader { get; set; }
        public string LoaderType { get; set; }
        public string Version { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public string ImageURL { get; set; }
        public string Slug { get; set; }
    }
    public partial class CreateVanilaPackWindow : Window 
    {
        public bool IsModPackCreated = false;
        private List<string> iconUrl = new List<string>();
        private string SiteDowload = "Modrinth";
        private readonly string apiKey = Secrets.CurseForgeKey;
        private static readonly HttpClient client = new HttpClient();
        private static readonly HttpClient httpClient = new HttpClient();
        List<string> fileUrlDowload = new List<string>();
        private ApiClient curseClient;
        private List<ModInfo> ResourcePacksList = new();

        public CreateVanilaPackWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(
ApplicationTheme.Dark,
Wpf.Ui.Controls.WindowBackdropType.Mica
            );
            ApplicationThemeManager.Apply(this);

            AddVersionList();
        }

        private void AddModpackToInstalled(InstalledModpack modpack)
        {
            string jsonPath = Path.Combine(ModpackPaths.DataDirectory, "installed_modpacks.json");

            List<InstalledModpack> modpacks = new();
            if (File.Exists(jsonPath))
            {
                var existingJson = File.ReadAllText(jsonPath);
                modpacks = JsonSerializer.Deserialize<List<InstalledModpack>>(existingJson) ?? new();
            }

            if (!modpacks.Any(m => m.Name == modpack.Name))
                modpacks.Add(modpack);

            string newJson = JsonSerializer.Serialize(modpacks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonPath, newJson);
        }

        private async void UpdateModsList()
        {
            if (VersionVanil.SelectedItem == null)
                return;

            if (ModsDowloadList.Items != null)
                ModsDowloadList.Items.Clear();

            try
            {
                if (SiteDowload == "Modrinth")
                {
                   var urls = $"https://api.modrinth.com/v2/search?query={SearchSystemModsTXT.Text}&facets=[[%22project_type:resourcepack%22]]&limit=10";
                    var response = await httpClient.GetStringAsync(urls);
                    dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

                    foreach (var mod in result["hits"])
                    {
                        string loaderType = "resourcepack";
                        var item = CreateItemFromModrinth(mod, loaderType);
                        ModsDowloadList.Items.Add(item);
                    }
                }
                else if (SiteDowload == "CurseForge")
                {
                    curseClient ??= new ApiClient(apiKey);

                    var searchResponse = await curseClient.SearchModsAsync(
                        gameId: 432,
                        classId: 12,
                        gameVersion: VersionVanil.SelectedItem?.ToString(),
                        pageSize: 10,
                        searchFilter: SearchSystemModsTXT.Text
                    );

                    foreach (var mod in searchResponse.Data)
                    {
                        var item = CreateItemJarFromCurseForge(mod, 2);
                        ModsDowloadList.Items.Add(item);
                    }
                }
                if (VersionVanil.IsEnabled == false) await AddVersionList();
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ех, не вдалося завантажити список ресурс-паків.\nСхоже на проблему з інтернетом або API.\n\nДеталі: {ex.Message}",
                                    "Помилка списку",
                                    MascotEmotion.Sad);
            }
        }

        private ModPackItem CreateItemJarFromCurseForge(Mod mod, int type)
        {
            var item = new ModPackItem();

            item.ModPackName.Content = mod.Name;

            string icon = mod.Logo?.Url ?? "";
            iconUrl.Add(icon);

            item.IconModPack.Source = !string.IsNullOrEmpty(icon)
                ? new BitmapImage(new Uri(icon))
                : new BitmapImage(new Uri("pack://application:,,,/Windows/Frame_73.png"));

            item.MouseDoubleClick += (s, e) =>
            {
                WebHelper.OpenUrl(mod.Links.WebsiteUrl);
            };

            item.downloda_url = "";
            item.game_version = VersionVanil.SelectedItem?.ToString();
            item.loaders = "Vanila";
            item.ProjectId = mod.Id.ToString();
            item.Slug = mod.Slug;
            item.Name = mod.Name;
            item.Type = 2;

            item.AddModInModPack.Visibility = Visibility.Visible;

            item.AddModInModPack.MouseDown += async (s, e) =>
            {
                AnimationService.FadeIn(GirdModsDowload, 0.3);
                AnimationService.FadeIn(MenuInstaller, 0.3);

                var info = new ModInfo
                {
                    Name = item.Name,
                    ProjectId = item.ProjectId,
                    Type = type == 0 ? "mod" : type == 1 ? "shader" : "resourcepack",
                    ImageURL = mod.Logo.Url,
                    Slug = item.Slug,
                };

                await GetLatestCompatibleResourcePacks_CurseForge(
                    new List<ModInfo> { info },
                    VersionVanil.SelectedItem?.ToString()
                );

            };

            return item;
        }
        private ModPackItem CreateItemFromModrinth(dynamic mod, string loaderType)
        {
            var item = new ModPackItem();

            item.ModPackName.Content = mod["title"];
            var icon = mod["icon_url"]?.ToString();
            iconUrl.Add(icon);

            if (!string.IsNullOrEmpty(icon) && Uri.IsWellFormedUriString(icon, UriKind.Absolute))
                item.IconModPack.Source = new BitmapImage(new Uri(icon));
            else
                item.IconModPack.Source = new BitmapImage(new Uri("pack://application:,,,/Windows/Frame_73.png"));

            item.MouseDoubleClick += (s, e) =>
            {
                WebHelper.OpenUrl($"https://modrinth.com/{loaderType}/{mod["slug"]}");
            };

            item.downloda_url = $"https://api.modrinth.com/v2/project/{mod["project_id"]}/version";
            item.game_version = VersionVanil.SelectedItem.ToString();
            item.loaders = "Vanila";
            item.ProjectId = mod["project_id"];
            item.Slug = mod["slug"];
            item.Name = mod["title"];
            item.haseh = mod["hashes"];
            item.AddModInModPack.Visibility = Visibility.Visible;

            item.AddModInModPack.MouseDown += (s, e) =>
            {
                AnimationService.FadeIn(GirdModsDowload, 0.3);
                AnimationService.FadeIn(MenuInstaller, 0.3);
                GetCompatibleVersions(new List<ModInfo>
        {
            new ModInfo
            {
                Name = mod["title"].ToString(),
                ProjectId = mod["project_id"].ToString(),
                Loader = "Vanila",
                LoaderType = loaderType,
                Version = VersionVanil.SelectedItem.ToString(),
                Url = $"https://api.modrinth.com/v2/project/{mod["project_id"]}/version",
                Type = loaderType,
                ImageURL = icon,
                Slug = item.Slug,
            }
        }).ContinueWith(task =>
        {
            var updatedMods = task.Result;
            if (updatedMods.Count > 0)
            {
                foreach (var updatedMod in updatedMods)
                {
                    VersionMods.Items.Add(updatedMod.Name);
                }
            }
        });
            };
            return item;
        }

        private async Task<List<ModInfo>> GetCompatibleVersions(List<ModInfo> mods)
        {
            List<ModInfo> updatedMods = new();
            VersionMods.Items.Clear();
            fileUrlDowload?.Clear();

            if (VersionVanil.SelectedItem == null)
            {
                MascotMessageBox.Show(
                                    "Секундочку! Ти забув обрати версію Minecraft.\nБез цього я не знаю, що шукати.",
                                    "Версія?",
                                    MascotEmotion.Alert);
                return updatedMods;
            }

            string selectedGameVersion = VersionVanil.SelectedItem.ToString();
            AnimationService.FadeIn(GirdModsDowload, 0.3);
            AnimationService.FadeIn(MenuInstaller, 0.3);

            using HttpClient httpClient = new();

            foreach (var mod in mods)
            {
                try
                {
                    string url = $"https://api.modrinth.com/v2/project/{mod.ProjectId}/version";
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    var versions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ModVersion>>(responseBody);

                    if (versions == null || versions.Count == 0)
                    {
                        MascotMessageBox.Show(
                                                    $"Дивина, я не знайшла версій для {mod.LoaderType} \"{mod.Name}\".",
                                                    "Пусто",
                                                    MascotEmotion.Confused);
                        continue;
                    }

                    List<ModVersion> filteredVersions;

                    if (mod.LoaderType == "mod")
                    {
                        filteredVersions = versions
                            .Where(v => v.GameVersions.Contains(selectedGameVersion) &&
                                        v.Loaders.Any(l => l.Equals(mod.Loader, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                    }
                    else
                    {
                        filteredVersions = versions
                            .Where(v => v.GameVersions.Contains(selectedGameVersion))
                            .ToList();
                    }

                    if (filteredVersions.Count == 0)
                    {
                        MascotMessageBox.Show(
                                                    $"Ех, {mod.LoaderType} \"{mod.Name}\" не має сумісних версій для Minecraft {selectedGameVersion}.",
                                                    "Несумісність",
                                                    MascotEmotion.Sad);
                        continue;
                    }

                    foreach (var version in filteredVersions)
                    {
                        VersionMods.Items.Add(version.VersionNumber);
                        if (version.Files != null)
                        {
                            foreach (var file in version.Files)
                            {
                                fileUrlDowload.Add(file.Url);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MascotMessageBox.Show(
                                            $"Ой! Помилка при обробці \"{mod.Name}\":\n{ex.Message}",
                                            "Збій",
                                            MascotEmotion.Sad);
                }
            }

            return updatedMods;
        }

        private void SaveModToModpackJson(ModInfo mod)
        {
            ResourcePacksList.Add(mod);

            string modpackName = NameModPackTXT.Text.Trim();
            if (string.IsNullOrWhiteSpace(modpackName) || modpackName == "Введіть назву збірки")
            {
                MascotMessageBox.Show(
                                    "А як ми назвемо це чудо?\nВведи назву збірки перед створенням!",
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
                    modList = JsonSerializer.Deserialize<List<ModInfo>>(existingJson) ?? new List<ModInfo>();
                }
                catch
                {
                    modList = new List<ModInfo>();
                }
            }

            if (!modList.Any(m => m.Name == mod.Name))
            {
                modList.Add(mod);

                AddItemModPack moditem = new AddItemModPack();
                moditem.NameMod.Text = mod.Name;
                moditem.IconMod.Source = (ModsDowloadList.SelectedItem as ModPackItem)?.IconModPack?.Source;

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

                string newJson = JsonSerializer.Serialize(modList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, newJson);

                MascotMessageBox.Show(
                                    $"Ура! Мод \"{mod.Name}\" успішно додано до збірки \"{modpackName}\"!",
                                    "Готово!",
                                    MascotEmotion.Happy);
                AddModsInModPackList.Items.Add(moditem);
            }

            else
            {
                MascotMessageBox.Show(
                                    $"Чекай-но! Мод \"{mod.Name}\" вже є в цій збірці.\nНавіщо нам два однакових?",
                                    "Вже є",
                                    MascotEmotion.Alert);
            }

            VersionVanil.IsEnabled = false;
            NameModPackTXT.IsEnabled = false;
        }
        private void LoadModsByType(string type)
        {
            AddModsInModPackList.Items.Clear(); 

            List<ModInfo> sourceList = type switch
            {
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
                    ResourcePacksList.Remove(mod);

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


        private async Task AddVersionList()
        {
            if (VersionVanil.Items != null && VersionVanil.IsEnabled == true) VersionVanil.Items.Clear();

            string searchText = SearchSystemModsTXT.Text.ToLower().Trim();

            bool skipFilter = string.IsNullOrWhiteSpace(searchText) || searchText == "пошук";

            Regex regex = new Regex(skipFilter ? ".*" : Regex.Escape(searchText).Replace(@"\\*", ".*"), RegexOptions.IgnoreCase);
            try
            {
                IEnumerable<string> versions = null;

                var path = new MinecraftPath(Settings1.Default.PathLacunher);
                var launcher = new MinecraftLauncher(path);
                versions = (await launcher.GetAllVersionsAsync())
                    .Where(v => v.Type == "release")
                    .Select(v => v.Name);

                foreach (var version in versions)
                {
                    if (skipFilter || regex.IsMatch(version))
                        VersionVanil.Items.Add(version);
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Не змогла отримати список ванільних версій.\nПеревір папку лаунчера.\n\nДеталі: {ex.Message}",
                                    "Помилка версій",
                                    MascotEmotion.Sad);
            }
        }
        private async Task<List<ModInfo>> GetLatestCompatibleResourcePacks_CurseForge(List<ModInfo> packs, string gameVersion)
        {
            List<ModInfo> updatedPacks = new();

            VersionMods.Items.Clear();
            fileUrlDowload?.Clear();

            if (VersionVanil.SelectedItem == null)
            {
                MascotMessageBox.Show(
                                    "Секундочку! Ти забув обрати версію Minecraft.\nБез цього я не знаю, що шукати.",
                                    "Версія?",
                                    MascotEmotion.Alert);
                return updatedPacks;
            }

            string selectedGameVersion = VersionVanil.SelectedItem.ToString();

            AnimationService.FadeIn(GirdModsDowload, 0.3);
            AnimationService.FadeIn(MenuInstaller, 0.3);

            curseClient ??= new ApiClient(apiKey);

            foreach (var pack in packs)
            {
                try
                {
                    if (pack.Type != "resourcepack")
                        continue;

                    if (!int.TryParse(pack.ProjectId, out int projectId))
                    {
                        MascotMessageBox.Show(
                                                    $"Невірний ProjectId для {pack.Name}. Я не можу його обробити.",
                                                    "Помилка ID",
                                                    MascotEmotion.Confused);
                        continue;
                    }

                    var response = await curseClient.GetModFilesAsync(projectId, null, ModLoaderType.Any);

                    if (response?.Data == null || response.Data.Count == 0)
                    {
                        MascotMessageBox.Show(
                                                    $"Дивина, я не знайшла файлів для ресурс-паку \"{pack.Name}\".",
                                                    "Пусто",
                                                    MascotEmotion.Confused);
                        continue;
                    }

                    var compatibleFiles = response.Data
                        .Where(file => file.GameVersions.Any(v =>
                            v.Equals(selectedGameVersion, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (compatibleFiles.Count == 0)
                    {
                        MascotMessageBox.Show(
                                                    $"Ех, ресурс-пак \"{pack.Name}\" не має сумісних версій для Minecraft {selectedGameVersion}.",
                                                    "Несумісність",
                                                    MascotEmotion.Sad);
                        continue;
                    }

                    foreach (var file in compatibleFiles)
                    {
                        VersionMods.Items.Add($"{file.DisplayName} | ID: {file.Id}");

                        string url = file.DownloadUrl;
                        if (string.IsNullOrEmpty(url))
                        {
                            url = $"https://www.curseforge.com/minecraft/texture-packs/{pack.Slug}/download/{file.Id}";
                        }

                        fileUrlDowload.Add(url);
                    }
                }
                catch (Exception ex)
                {
                    MascotMessageBox.Show(
                                            $"Ой! Помилка при обробці ресурс-паку \"{pack.Name}\":\n{ex.Message}",
                                            "Збій",
                                            MascotEmotion.Sad);
                }
            }

            return updatedPacks;
        }

        private void ModrinthSite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ModsDowloadList.Items != null) ModsDowloadList.Items.Clear();
            if (VersionVanil.Items != null && VersionVanil.IsEnabled == true) VersionVanil.Items.Clear();

            SiteDowload = "Modrinth";
            AnimationService.AnimateBorderObject(0, 0, SelectSiteModPacksNow, true);
            UpdateModsList();
        }

        private void CurseForgeSite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ModsDowloadList.Items != null) ModsDowloadList.Items.Clear();
            if (VersionVanil.Items != null && VersionVanil.IsEnabled == true) VersionVanil.Items.Clear();

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
            if (VersionVanil.SelectedItem is not string selectedVanillaVersion)
                return;
            if (ModsDowloadList.Items != null)
                ModsDowloadList.Items.Clear();
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

        private void DowloadTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(NameModPackTXT.Text))
            {
                AnimationService.FadeOut(GirdModsDowload, 0.3);
                AnimationService.FadeOut(MenuInstaller, 0.3);
                MascotMessageBox.Show(
                                    "А як ми назвемо це чудо?\nВведи назву збірки, будь ласка.",
                                    "Назва?",
                                    MascotEmotion.Alert);
                return;
            }

            if (VersionMods.SelectedItem != null)
            {
                var selectedVersion = VersionVanil.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedVersion))
                {
                    SaveModToModpackJson(new ModInfo
                    {
                        Name = (ModsDowloadList.SelectedItem as ModPackItem)?.ModPackName?.Content?.ToString() + ":" + VersionMods.SelectedItem.ToString(),
                        Loader = null,
                        Version = VersionVanil.SelectedItem.ToString(),
                        Url = fileUrlDowload[VersionMods.SelectedIndex],
                        LoaderType = "Vanila",
                        Type = "resourcepack",
                        ImageURL = (ModsDowloadList.SelectedItem as ModPackItem)?.IconModPack?.Source.ToString()
                    });
                }
                GirdModsDowload.Visibility = Visibility.Hidden;
                MenuInstaller.Visibility = Visibility.Hidden;
            }
            else
            {
                GirdModsDowload.Visibility = Visibility.Hidden;
                MenuInstaller.Visibility = Visibility.Hidden;
                MascotMessageBox.Show(
                                    "Тицьни на потрібну версію зі списку, будь ласка!",
                                    "Обери версію",
                                    MascotEmotion.Alert);
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
                    }
                    catch (Exception ex)
                    {
                        MascotMessageBox.Show(
                                                    $"Ой, не вдалося видалити тимчасову папку модпаку.\n\nПомилка: {ex.Message}",
                                                    "Помилка очистки",
                                                    MascotEmotion.Sad);
                    }
                }
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
                                            "А як ми назвемо це чудо?\nВведи назву збірки перед створенням!",
                                            "Назва?",
                                            MascotEmotion.Alert); return;
                }

                string basePath = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", modpackName);
                string pathJson = Path.Combine(basePath, "modpack.json");

                if (!Directory.Exists(basePath) || !File.Exists(pathJson))
                {
                    MascotMessageBox.Show(
                                            "Хм, схоже цей модпак ще не готовий або його файли зникли.",
                                            "Проблема",
                                            MascotEmotion.Confused);
                    return;
                }

                string imageUrl = "pack://application:,,,/Icon/IconCL(Common).png"; 
                if (iconUrl.Count > 0)
                    imageUrl = iconUrl[0];

                var modpack = new InstalledModpack
                {
                    Name = modpackName,
                    TypeSite = "Custom",
                    MinecraftVersion = VersionVanil.SelectedItem?.ToString() ?? "???",
                    LoaderType = "Vanila",
                    LoaderVersion = null,
                    Path = basePath,
                    PathJson = pathJson,
                    UrlImage = imageUrl
                };

                AddModpackToInstalled(modpack);
                MascotMessageBox.Show(
                                    "Чудово! Ванільну збірку додано до твого списку.\nМожна грати!",
                                    "Успіх",
                                    MascotEmotion.Happy);
                IsModPackCreated = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Біда! Сталася помилка при створенні ванільної збірки.\n\nДеталі: {ex.Message}",
                                    "Критичний збій",
                                    MascotEmotion.Sad);
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
            if (VersionVanil.SelectedItem != null) { UpdateModsList(); }
            if (ResourcePacksList != null) { LoadModsByType("resourcepack"); }
        }
    }
}
