using CmlLib.Core;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CurseForge.APIClient.Models.Mods;
using CurseForge.APIClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using MessageBox = System.Windows.MessageBox;
using CL_CLegendary_Launcher_.Class;
using Wpf.Ui.Controls;
using MessageBoxButton = System.Windows.MessageBoxButton;
using Wpf.Ui.Appearance;
using MenuItem = System.Windows.Controls.MenuItem;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class DowloadModPack : Window
    {
        private List<string> iconUrl = new List<string>();
        private string LoderNow = "Forge";
        private string SiteDowload = "Modrinth";
        private readonly string apiKey = Secrets.CurseForgeKey;
        private static readonly HttpClient httpClient = new HttpClient();
        private ApiClient curseClient;
        private ModpackService _modpackService;

        public DowloadModPack(ModpackService modpackService)
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);

            curseClient = new ApiClient(apiKey);
            _modpackService = modpackService;
        }
        private void ExitLauncher_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            AnimationService.FadeIn(BordrExitTXT, 0.5);
        }
        private void ExitLauncher_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
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
        private void MainWin_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void ModrinthSite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ModsDowloadList.Items != null) ModsDowloadList.Items.Clear();
            if (VersionVanil.SelectedItem != null)
                LoadModrinthModsAsync(SearchSystemModsTXT.Text);

            SiteDowload = "Modrinth";
            AnimationService.AnimateBorderObject(0, 0, SelectSiteModPacksNow, true);
        }

        private void CurseForgeSite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ModsDowloadList.Items != null) ModsDowloadList.Items.Clear();
            if (VersionVanil.SelectedItem != null) LoadCurseForgeModpacksAsync(SearchSystemModsTXT.Text);

            SiteDowload = "CurseForge";
            AnimationService.AnimateBorderObject(30, 0, SelectSiteModPacksNow, true);
        }
        private async Task LoadModrinthModsAsync(string searchText)
        {
            if (SiteDowload != "Modrinth") return;

            ModsDowloadList.Items.Clear();
            try
            {
                string version = VersionVanil.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(version)) return;

                string url = $"https://api.modrinth.com/v2/search?query={searchText}&facets=[[\"categories:{LoderNow}\"],[\"project_type:modpack\"],[\"versions:{version}\"]]&limit=10&sort=downloads";
                var response = await httpClient.GetStringAsync(url);
                dynamic result = JsonConvert.DeserializeObject(response);

                foreach (var mod in result["hits"])
                {
                    var item = CreateItemJarFromModrinth(mod);
                    await AddItemWithAnimation(item);
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ой леле! Я намагалася завантажити список модпаків з Modrinth, але щось пішло не так.\n\nПомилка: {ex.Message}",
                                    "Збій Modrinth",
                                    MascotEmotion.Sad);
            }
        }

        private ModPackItem CreateItemJarFromModrinth(dynamic mod)
        {
            var item = new ModPackItem();

            item.ModPackName.Content = mod["title"];
            var icon = mod["icon_url"]?.ToString();
            iconUrl.Add(icon);

            if (!string.IsNullOrEmpty(icon) && Uri.IsWellFormedUriString(icon, UriKind.Absolute))
                item.IconModPack.Source = new BitmapImage(new Uri(icon));
            else
                item.IconModPack.Source = new BitmapImage(new Uri("pack://application:,,,/Icon/IconCL(Common).png"));

            item.MouseDown += (s, e) =>
            {
                GetModpackDownloadUrlAsync(
                  (string)mod["project_id"],
                  VersionVanil.SelectedItem.ToString(),
                  LoderNow
                );
            };

            item.MouseDoubleClick += (s, e) =>
            {
                WebHelper.OpenUrl($"https://modrinth.com/modpack/{mod["slug"]}");
            };

            item.downloda_url = $"https://api.modrinth.com/v2/download/{mod["project_id"]}?version={VersionVanil.SelectedItem}&loader={LoderNow}";
            item.game_version = VersionVanil.SelectedItem.ToString();
            item.loaders = LoderNow;
            item.ProjectId = mod["project_id"];
            item.Slug = mod["slug"];
            item.Name = mod["title"];
            item.haseh = mod["hashes"];
            item.AddModInModPack.Visibility = Visibility.Hidden;

            return item;
        }

        private async Task AddItemWithAnimation(UIElement item)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                ModsDowloadList.Items.Add(item);
                await Task.Delay(100);
            });
        }

        private async void SearchSystemModsTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            ModsDowloadList.Items.Clear();
            if (VersionVanil.SelectedValue != null && SiteDowload == "Modrinth")
                await LoadModrinthModsAsync(SearchSystemModsTXT.Text);
            if (VersionVanil.SelectedValue != null && SiteDowload == "CurseForge")
                await LoadCurseForgeModpacksAsync(SearchSystemModsTXT.Text);
        }

        private async void VersionVanil_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModsDowloadList.Items.Clear();
            if (VersionVanil.SelectedValue != null && SiteDowload == "Modrinth")
                await LoadModrinthModsAsync(SearchSystemModsTXT.Text);
            if (VersionVanil.SelectedValue != null && SiteDowload == "CurseForge")
                await LoadCurseForgeModpacksAsync(SearchSystemModsTXT.Text);
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string content = (sender as MenuItem).Header.ToString();
            switch (content)
            {
                case "Forge":
                    LoderNow = "Forge";
                    SelectLoader.Content = content;
                    AddVersionList();
                    break;
                case "Fabric":
                    LoderNow = "Fabric";
                    SelectLoader.Content = content;
                    AddVersionList();
                    break;
                case "Quilt":
                    LoderNow = "Quilt";
                    SelectLoader.Content = content;
                    AddVersionList();
                    break;
                case "NeoForge":
                    LoderNow = "NeoForge";
                    SelectLoader.Content = content;
                    AddVersionList();
                    break;
            }
        }
        private async Task AddVersionList()
        {
            VersionVanil.Items.Clear();
            ModsDowloadList.Items.Clear();

            string searchText = SearchSystemModsTXT.Text.ToLower().Trim();
            Regex regex = new Regex(string.IsNullOrEmpty(searchText) ? ".*" : Regex.Escape(searchText).Replace(@"\*", ".*"), RegexOptions.IgnoreCase);

            try
            {
                IEnumerable<string> versions = null;
                var path = new MinecraftPath(Settings1.Default.PathLacunher);
                var launcher = new MinecraftLauncher(path);

                var allVersions = await launcher.GetAllVersionsAsync();

                if (LoderNow == "Forge")
                {
                    var minVer = new Version(1, 7, 10);

                    versions = allVersions
                        .Where(v => v.Type == "release")
                        .Where(v => ParseGameVersion(v.Name) >= minVer) 
                        .Select(v => v.Name);
                }
                else if (LoderNow == "Fabric")
                {
                    var fabricInstaller = new FabricInstaller(httpClient);
                    versions = await fabricInstaller.GetSupportedVersionNames();
                }
                else if (LoderNow == "Quilt")
                {
                    var quiltInstaller = new QuiltInstaller(httpClient);
                    versions = await quiltInstaller.GetSupportedVersionNames();
                }
                else if (LoderNow == "NeoForge")
                {
                    var minVer = new Version(1, 20, 1);

                    versions = allVersions
                        .Where(v => v.Type == "release")
                        .Where(v => ParseGameVersion(v.Name) >= minVer)
                        .Select(v => v.Name);
                }
                else if (LoderNow == "LiteLoader")
                {
                    var minVer = new Version(1, 5, 2);
                    var maxVer = new Version(1, 12, 2);

                    versions = allVersions
                        .Where(v => v.Type == "release")
                        .Where(v => {
                            var ver = ParseGameVersion(v.Name);
                            return ver >= minVer && ver <= maxVer;
                        })
                        .Select(v => v.Name);
                }
                else if (LoderNow == "Optifine")
                {
                    var minVer = new Version(1, 7, 2);

                    versions = allVersions
                        .Where(v => v.Type == "release")
                        .Where(v => ParseGameVersion(v.Name) >= minVer)
                        .Select(v => v.Name);
                }

                if (versions != null)
                {
                    foreach (var version in versions)
                    {
                        if (regex.IsMatch(version))
                            VersionVanil.Items.Add(version);
                    }
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Не змогла отримати список версій для {LoderNow}.\nПеревір з'єднання з інтернетом.\n\nДеталі: {ex.Message}",
                                    "Помилка версій",
                                    MascotEmotion.Sad);
            }

            if (VersionVanil.SelectedValue != null)
                await LoadModrinthModsAsync(SearchSystemModsTXT.Text);
        }
        private System.Version ParseGameVersion(string versionStr)
        {
            try
            {
                var cleanStr = Regex.Match(versionStr, @"^[0-9\.]+").Value;
                var parts = cleanStr.Split('.');
                if (parts.Length == 2) cleanStr += ".0";

                if (System.Version.TryParse(cleanStr, out var version))
                    return version;
            }
            catch { }

            return new System.Version(0, 0, 0);
        }
        private async Task<string> GetModpackDownloadUrlAsync(string projectId, string version, string loader)
        {
            string url = $"https://api.modrinth.com/v2/project/{projectId}/version";
            string response = await httpClient.GetStringAsync(url);
            var versions = JsonConvert.DeserializeObject<List<dynamic>>(response);

            foreach (var ver in versions)
            {
                var version_number = ver["version_number"].ToString();
                var loaders = ver["loaders"].ToObject<List<string>>();
                var gameVersions = ver["game_versions"].ToObject<List<string>>();

                if (gameVersions.Contains(version) && loaders.Contains(loader.ToLower()))
                {
                    foreach (var file in ver["files"])
                    {
                        if (file["filename"].ToString().EndsWith(".mrpack") || file["primary"].ToObject<bool>())
                        {
                            return file["url"].ToString();
                        }
                    }
                }
            }

            return null;
        }
        private async void DowloaadModPacksButtonTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ModsDowloadList.SelectedItem is not ModPackItem selectedModpack)
                return;

            string tempFolder = Path.Combine(Settings1.Default.PathLacunher, $@"CLModpack\{selectedModpack.Name}");
            Directory.CreateDirectory(tempFolder);

            string zipFileName = $"{selectedModpack.Name}.zip";
            string zipFilePath = Path.Combine(tempFolder, zipFileName);

            try
            {
                DowloaadModPacksButtonTXT.IsEnabled = false;
                DowloaadModPacksButtonTXT.Content = "Завантаження...";

                string realDownloadUrl = SiteDowload switch
                {
                    "Modrinth" => await GetModpackDownloadUrlAsync(
                      selectedModpack.ProjectId,
                      selectedModpack.game_version,
                      selectedModpack.loaders),
                    "CurseForge" => await GetCurseForgeDownloadUrl(int.Parse(selectedModpack.ProjectId)),
                    _ => null
                };

                if (string.IsNullOrEmpty(realDownloadUrl))
                {
                    MascotMessageBox.Show(
                                            "Дивина! Я перерила весь сервер, але не знайшла посилання на завантаження цього мод-паку.",
                                            "Файл не знайдено",
                                            MascotEmotion.Sad);
                    return;
                }

                await DownloadModpackAsync(realDownloadUrl, zipFilePath);

                string extractPath = Path.Combine(tempFolder, selectedModpack.Name);
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(zipFilePath, extractPath);

                string loaderName = selectedModpack.loaders;
                string loaderVersion = "";
                string vanillaVersion = selectedModpack.game_version;
                string pathJson = SiteDowload == "Modrinth"
                  ? Path.Combine(extractPath, "modrinth.index.json")
                  : Path.Combine(extractPath, "manifest.json");

                if (File.Exists(pathJson))
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(pathJson);
                        var jsonObj = JsonConvert.DeserializeObject<JObject>(jsonContent);

                        if (SiteDowload == "Modrinth")
                        {
                            var deps = jsonObj["dependencies"] as JObject;
                            if (deps != null)
                            {
                                foreach (var property in deps.Properties())
                                {
                                    if (property.Name.Contains("fabric", StringComparison.OrdinalIgnoreCase) ||
                                      property.Name.Contains("forge", StringComparison.OrdinalIgnoreCase) ||
                                      property.Name.Contains("quilt", StringComparison.OrdinalIgnoreCase))
                                    {
                                        loaderName = property.Name;
                                        loaderVersion = property.Value?.ToString();
                                        break;
                                    }
                                }
                            }
                        }
                        else if (SiteDowload == "CurseForge")
                        {
                            vanillaVersion = jsonObj["minecraft"]?["version"]?.ToString() ?? vanillaVersion;
                            loaderName = jsonObj["minecraft"]?["modLoaders"]?.FirstOrDefault()?["id"]?.ToString().Split('-')[0] ?? loaderName;
                            loaderVersion = jsonObj["minecraft"]?["modLoaders"]?.FirstOrDefault()?["id"]?.ToString().Split('-')[1] ?? "";
                        }
                    }
                    catch (Exception ex)
                    {
                        MascotMessageBox.Show(
                                                    $"Ой! Я не змогла прочитати конфігурацію мод-паку (JSON).\nМожливо, файл пошкоджено.\n\nПомилка: {ex.Message}",
                                                    "Збій читання",
                                                    MascotEmotion.Sad);
                    }
                }
                SaveModpackToJson(new InstalledModpack
                {
                    Name = selectedModpack.Name,
                    TypeSite = SiteDowload,
                    MinecraftVersion = vanillaVersion,
                    LoaderType = loaderName,
                    LoaderVersion = loaderVersion,
                    Path = extractPath,
                    UrlImage = iconUrl[ModsDowloadList.SelectedIndex],
                    PathJson = pathJson
                });

                MascotMessageBox.Show(
                                    $"Ура! Мод-пак успішно завантажено та розпаковано!\nВін знаходиться тут:\n{extractPath}",
                                    "Готово!",
                                    MascotEmotion.Happy); 
                File.Delete(zipFilePath);
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ех, сталася критична помилка під час встановлення мод-паку.\n\nДеталі: {ex.Message}",
                                    "Критичний збій",
                                    MascotEmotion.Sad);
            }
            finally
            {
                DowloaadModPacksButtonTXT.IsEnabled = true;
                DowloaadModPacksButtonTXT.Content = "Завантажити";
            }
        }
        private async Task DownloadModpackAsync(string url, string savePath)
        {
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int read;

                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (totalBytes.HasValue)
                        {
                            double progress = (double)totalRead / totalBytes.Value * 100;
                            DowloaadModPacksButtonTXT.Content = $"Завантажено: {progress:F2}%";
                        }
                    }
                }
            }
        }
        private void SaveModpackToJson(InstalledModpack modpack)
        {
            Directory.CreateDirectory(ModpackPaths.DataDirectory);
            string jsonPath = ModpackPaths.InstalledModpacksJson;

            List<InstalledModpack> modpacks = new();

            if (File.Exists(jsonPath))
            {
                string existingJson = File.ReadAllText(jsonPath);
                modpacks = JsonSerializer.Deserialize<List<InstalledModpack>>(existingJson) ?? new List<InstalledModpack>();
            }

            if (!modpacks.Any(m => m.Name == modpack.Name))
                modpacks.Add(modpack);

            string newJson = JsonSerializer.Serialize(modpacks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonPath, newJson);
        }
        private async Task LoadCurseForgeModpacksAsync(string searchText)
        {
            if (SiteDowload != "CurseForge") return;

            ModsDowloadList.Items.Clear();

            try
            {
                var modLoaderType = LoderNow switch
                {
                    "Forge" => ModLoaderType.Forge,
                    "Fabric" => ModLoaderType.Fabric,
                    "Quilt" => ModLoaderType.Quilt,
                    "NeoForge" => ModLoaderType.NeoForge,
                    "LiteLoader" => ModLoaderType.LiteLoader,
                    "Optifine" => ModLoaderType.Any,
                    _ => ModLoaderType.Any 
                };

                var gameVersion = VersionVanil?.SelectedItem?.ToString();

                var searchResponse = await curseClient.SearchModsAsync(
                  gameId: 432, 
                            searchFilter: searchText,
                  classId: 4471,
                            pageSize: 10,
                  sortField: ModsSearchSortField.Popularity,
                  modLoaderType: modLoaderType,
                  gameVersion: gameVersion 
                        );

                foreach (var mod in searchResponse.Data)
                {
                    var item = CreateItemJarFromCurseForge(mod);
                    await AddItemWithAnimation(item);
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Не вдалося завантажити список модпаків з CurseForge.\nПеревір API ключ або інтернет.\n\nПомилка: {ex.Message}",
                                    "Збій CurseForge",
                                    MascotEmotion.Sad);
            }
        }
        private ModPackItem CreateItemJarFromCurseForge(CurseForge.APIClient.Models.Mods.Mod mod)
        {
            var item = new ModPackItem();

            item.ModPackName.Content = mod.Name;
            iconUrl.Add(mod.Logo?.Url ?? "");

            if (!string.IsNullOrEmpty(mod.Logo?.Url))
                item.IconModPack.Source = new BitmapImage(new Uri(mod.Logo.Url));
            else
                item.IconModPack.Source = new BitmapImage(new Uri("pack://application:,,,/Icon/IconCL(Common).png"));

            item.MouseDoubleClick += (s, e) =>
            {
                WebHelper.OpenUrl(mod.Links.WebsiteUrl);
            };

            item.downloda_url = ""; 

            item.game_version = VersionVanil.SelectedItem.ToString();

            item.loaders = LoderNow;
            item.ProjectId = mod.Id.ToString();
            item.Slug = mod.Slug;
            item.Name = mod.Name;
            item.AddModInModPack.Visibility = Visibility.Hidden;

            return item;
        }
        private async Task<string> GetCurseForgeDownloadUrl(int modId)
        {
            try
            {
                var files = await curseClient.GetModFilesAsync(modId);
                var file = files.Data.FirstOrDefault(f => f.FileName.EndsWith(".zip") || f.FileName.EndsWith(".mrpack"));

                return file?.DownloadUrl;
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Не змогла отримати пряме посилання на файл з CurseForge.\n\nПомилка: {ex.Message}",
                                    "Збій посилання",
                                    MascotEmotion.Sad);
                return null;
            }
        }

        private async void ImportFileModPacksTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Modpack Files (*.zip;*.rar;*.mrpack)|*.zip;*.rar;*.mrpack",
                Title = "Оберіть файл мод-паку"
            };

            if (dialog.ShowDialog() == true)
            {
                await _modpackService.ImportModpackAsync(dialog.FileName);
            }
        }
    }
}