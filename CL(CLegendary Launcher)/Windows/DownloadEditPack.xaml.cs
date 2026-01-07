using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core;
using CurseForge.APIClient;
using CurseForge.APIClient.Models.Mods;
using Newtonsoft.Json;
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
using JsonSerializer = System.Text.Json.JsonSerializer;
using Path = System.IO.Path;
using System.Diagnostics;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Threading;
using Newtonsoft.Json.Linq;
using CL_CLegendary_Launcher_.Class;
using Wpf.Ui.Appearance;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class DownloadEditPack : Window
    {
        public ModpackInfo CurrentModpack { get; set; }
        protected byte SelectMod = 0;
        public bool IsModPackCreated = false;
        private List<string> iconUrl = new List<string>();
        public string LoderNow = "Forge";
        private string SiteDowload = "Modrinth";
        private readonly string apiKey = Secrets.CurseForgeKey;
        private static readonly HttpClient client = new HttpClient();
        private static readonly HttpClient httpClient = new HttpClient();
        List<string> fileUrlDowload = new List<string>();
        private ApiClient curseClient;
        private bool isLoading = false;

        public DownloadEditPack(ModpackInfo currentModpack,byte SelectMod)
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(
    ApplicationTheme.Dark,
    Wpf.Ui.Controls.WindowBackdropType.Mica
            );
            ApplicationThemeManager.Apply(this);

            CurrentModpack = currentModpack;
            this.SelectMod = SelectMod;
            var ModsType = SelectMod switch 
            {
                0 => "Моди",
                2 => "Шейдери",
                1 => "Ресурспаки",
                _ => "Моди"
            };

            LoderNow = CurrentModpack.LoaderType;
            NameWin.Text = $"Завантаження {ModsType} у збірку {CurrentModpack.Name}";

            LoadVaribaleCurrectPack();
            UpdateModsList();
        }

        private void LoadVaribaleCurrectPack()
        {
            if(CurrentModpack != null)
            {
                LoadLoaderVersion(CurrentModpack.Name);
                VersionVanil.Items.Add(CurrentModpack.MinecraftVersion);
                VersionVanil.SelectedItem = CurrentModpack.MinecraftVersion;

                VersionVanil.IsEnabled = false; ListModsList.IsEnabled = false;

                LoaderButton.Content = CurrentModpack.LoaderType;
            }
        }
        void LoadLoaderVersion(string modpackName)
        {
            string path = @$"{AppContext.BaseDirectory}Data\installed_modpacks.json";

            if (!File.Exists(path))
            {
                MascotMessageBox.Show(
                                    "Ой! Я не знайшла файл конфігурації `installed_modpacks.json`.\nПеревір папку Data, можливо файл зник.",
                                    "Файл не знайдено",
                                    MascotEmotion.Confused);
                return;
            }

            string json = File.ReadAllText(path);
            JArray modpacks = JArray.Parse(json);

            var found = modpacks.FirstOrDefault(x => (string)x["Name"] == modpackName);

            if (found != null)
            {
                string loaderVersion = (string)found["LoaderVersion"];
                if (!string.IsNullOrEmpty(loaderVersion))
                {
                    ListModsList.Items.Add(loaderVersion);
                    ListModsList.SelectedItem = loaderVersion;
                }
                else
                {
                    MascotMessageBox.Show(
                                            "Дивина! У налаштуваннях цієї збірки не вказано версію завантажувача (LoaderVersion).",
                                            "Дані відсутні",
                                            MascotEmotion.Confused);
                }
            }
            else
            {
                MascotMessageBox.Show(
                                    "Хм... Я перевірила список встановлених збірок, але цю там не знайшла.",
                                    "Збірка зникла",
                                    MascotEmotion.Confused);
            }
        }
        private void AddModpackToInstalled(InstalledModpack modpack)
        {
            string jsonPath = Path.Combine(ModpackPaths.DataDirectory, "modpack_temp_add.json");

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
            if (VersionVanil.SelectedItem == null || ListModsList.SelectedItem == null && CurrentModpack.LoaderType != "Vanila")
                return;

            if (ModsDowloadList.Items != null)
                ModsDowloadList.Items.Clear();

            try
            {
                if (SiteDowload == "Modrinth")
                {
                    var urls = new[] {
        $"https://api.modrinth.com/v2/search?query={SearchSystem.Text}&facets=[[%22categories:{LoderNow}%22],[%22project_type:mod%22]]&limit=10",
        $"https://api.modrinth.com/v2/search?query={SearchSystem.Text}&facets=[[%22project_type:resourcepack%22]]&limit=10",
        $"https://api.modrinth.com/v2/search?query={SearchSystem.Text}&facets=[[%22project_type:shader%22]]&limit=10",
            };
                    var response = await httpClient.GetStringAsync(urls[(int)SelectMod]);
                    dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

                    foreach (var mod in result["hits"])
                    {
                        string loaderType = SelectMod switch
                        {
                            0 => "mod",
                            1 => "shader",
                            2 => "resourcepack",
                            _ => "mod"
                        };
                        var item = CreateItemFromModrinth(mod, loaderType);
                        ModsDowloadList.Items.Add(item);
                    }
                }
                else if (SiteDowload == "CurseForge")
                {
                    int classId = SelectMod switch
                    {
                        0 => 6,
                        2 => 6552,
                        1 => 12,
                        _ => 6
                    };

                    dynamic modLoaderType = null; 
                    if (CurrentModpack.LoaderType != "Vanila")
                    {
                        modLoaderType = LoderNow switch
                        {
                            "Forge" => ModLoaderType.Forge,
                            "Fabric" => ModLoaderType.Fabric,
                            "Quilt" => ModLoaderType.Quilt,
                            "NeoForge" => ModLoaderType.NeoForge,
                            "LiteLoader" => ModLoaderType.LiteLoader,
                            "Optifine" => ModLoaderType.Any,
                            _ => null
                        };
                    }

                    curseClient ??= new ApiClient(apiKey);

                    var searchResponse = await curseClient.SearchModsAsync(
                        gameId: 432,
                        classId: classId,
                        modLoaderType: modLoaderType, 
                        gameVersion: VersionVanil.SelectedItem?.ToString(),
                        pageSize: 10,
                        searchFilter: SearchSystem.Text
                    );

                    foreach (var mod in searchResponse.Data)
                    {
                        var item = CreateItemJarFromCurseForge(mod, SelectMod);
                        ModsDowloadList.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ех, не вдалося завантажити список модів.\nСхоже на проблему з інтернетом або API.\n\nДеталі: {ex.Message}",
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
                : new BitmapImage(new Uri("pack://application:,,,/Icon/IconCL(Common).png"));

            item.MouseDoubleClick += (s, e) =>
            {
                WebHelper.OpenUrl(mod.Links.WebsiteUrl);
            };

            item.downloda_url = "";
            item.game_version = VersionVanil.SelectedItem?.ToString();
            item.loaders = LoderNow;
            item.ProjectId = mod.Id.ToString();
            item.Slug = mod.Slug;
            item.Name = mod.Name;

            item.Type = type switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                _ => 0
            };

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

                await GetLatestCompatibleModVersions_CurseForge(
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
                item.IconModPack.Source = new BitmapImage(new Uri("pack://application:,,,/Icon/IconCL(Common).png"));

            item.MouseDoubleClick += (s, e) =>
            {
                WebHelper.OpenUrl($"https://modrinth.com/{loaderType}/{mod["slug"]}");
            };

            item.downloda_url = $"https://api.modrinth.com/v2/project/{mod["project_id"]}/version";
            item.game_version = VersionVanil.SelectedItem.ToString();
            item.loaders = LoderNow;
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
                Loader = LoderNow,
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
                    var versions = JsonConvert.DeserializeObject<List<ModVersion>>(responseBody);

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
            string modpackFolder = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", CurrentModpack.Name);
            Directory.CreateDirectory(modpackFolder);

            string jsonPath = Path.Combine(modpackFolder, "modpack_temp_add.json");

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
                                    $"Ура! Мод \"{mod.Name}\" успішно додано до збірки \"{CurrentModpack.Name}\"!\n.",
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
            ListModsList.IsEnabled = false;
        }
        private async Task<List<ModInfo>> GetLatestCompatibleModVersions_CurseForge(List<ModInfo> mods, string gameVersion)
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

            curseClient ??= new ApiClient(apiKey);

            foreach (var mod in mods)
            {
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
                        _ => ModLoaderType.Forge
                    };

                    if (!int.TryParse(mod.ProjectId, out int modId))
                    {
                        MascotMessageBox.Show(
                                                    $"Щось не так з ID проєкту для {mod.Name}. Я не можу його розібрати.",
                                                    "Помилка ID",
                                                    MascotEmotion.Confused);
                        continue;
                    }

                    var response = await curseClient.GetModFilesAsync(modId, null, modLoaderType);

                    if (response?.Data == null || response.Data.Count == 0)
                    {
                        MascotMessageBox.Show(
                                                    $"Дивина, я не знайшла файлів для {mod.Type} \"{mod.Name}\".",
                                                    "Пусто",
                                                    MascotEmotion.Confused);
                        continue;
                    }

                    List<CurseForge.APIClient.Models.Files.File> compatibleFiles;

                    if (mod.Type == "mod")
                    {
                        compatibleFiles = response.Data
                            .Where(file =>
                                file.GameVersions.Any(v =>
                                    v.Equals(selectedGameVersion, StringComparison.OrdinalIgnoreCase)) &&
                                file.GameVersions.Any(v =>
                                    v.Equals(LoderNow, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                    }
                    else
                    {
                        compatibleFiles = response.Data
                            .Where(file =>
                                file.GameVersions.Any(v =>
                                    v.Equals(selectedGameVersion, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                    }

                    if (compatibleFiles.Count == 0)
                    {
                        MascotMessageBox.Show(
                                                    $"Ех, {mod.Type} \"{mod.Name}\" не має сумісних версій для Minecraft {selectedGameVersion}.",
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
                            string typePath = mod.Type switch
                            {
                                "shader" => "shaders",
                                "resourcepack" => "texture-packs",
                                _ => "mc-mods"
                            };
                            url = $"https://www.curseforge.com/minecraft/{typePath}/{mod.Slug}/download/{file.Id}";
                        }


                        fileUrlDowload.Add(url);
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
        private void DowloadTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string modpackFolder = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", CurrentModpack.Name);
            Directory.CreateDirectory(modpackFolder);

            string jsonPath = Path.Combine(modpackFolder, "modpack_temp_add.json");


            if (VersionMods.SelectedItem != null)
            {
                var selectedVersion = VersionVanil.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedVersion))
                {
                    var selectedModItem = ModsDowloadList.SelectedItem as ModPackItem;

                    var modInfo = new ModInfo
                    {
                        Name = $"{selectedModItem?.ModPackName?.Content?.ToString() ?? "Без назви"}:{VersionMods.SelectedItem?.ToString() ?? "?"}",
                        Loader = ListModsList.SelectedItem?.ToString(),
                        Version = VersionVanil.SelectedItem?.ToString(),
                        Url = (VersionMods.SelectedIndex >= 0 && VersionMods.SelectedIndex < fileUrlDowload.Count)
                            ? fileUrlDowload[VersionMods.SelectedIndex]
                            : null,
                        LoaderType = LoderNow,
                        Type = SelectMod switch
                        {
                            0 => "mod",
                            2 => "shader",
                            1 => "resourcepack",
                            _ => null
                        },
                        ImageURL = selectedModItem?.IconModPack?.Source?.ToString()
                    };

                    SaveModToModpackJson(modInfo);
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
        private void CreateModPacksButtonTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string modpackName = CurrentModpack.Name;
            if (string.IsNullOrWhiteSpace(modpackName) || modpackName == "Введіть назву збірки")
            {
                MascotMessageBox.Show(
                                    "А як ми назвемо це чудо?\nВведи назву збірки перед створенням!",
                                    "Назва?",
                                    MascotEmotion.Alert);
                return;
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
                LoaderType = LoderNow,
                LoaderVersion = ListModsList.SelectedItem?.ToString() ?? "???",
                Path = basePath,
                PathJson = pathJson,
                UrlImage = imageUrl
            };

            AddModpackToInstalled(modpack);
            MascotMessageBox.Show(
                            "Чудово! Збірку додано до твого списку.\nМожна грати!",
                            "Успіх",
                            MascotEmotion.Happy);
            IsModPackCreated = true;
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
        private void AnimateBorderObject(double targetX, double targetY, Border border, bool visibly)
        {
            if (visibly) { border.Visibility = Visibility.Visible; }

            DoubleAnimation moveXAnimation = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase()
            };

            DoubleAnimation moveYAnimation = new DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase()
            };
            moveXAnimation.Completed += (s, e) =>
            {
                if (!visibly) { border.Visibility = Visibility.Hidden; }
            };
            border.RenderTransform.BeginAnimation(TranslateTransform.XProperty, moveXAnimation);
            border.RenderTransform.BeginAnimation(TranslateTransform.YProperty, moveYAnimation);
        }

        private void SearchSystem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchSystem.Text) && SearchSystem.Text != "Пошук")
            {
                ModsDowloadList.Items.Clear();
                UpdateModsList();
            }
        }

        private async void CreateModPacksButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var progress = new DowloadProgress();
            progress.Show();

            string modpackFolder = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", CurrentModpack.Name);
            Directory.CreateDirectory(modpackFolder);

            string jsonPath = Path.Combine(modpackFolder, "modpack_temp_add.json");

            if (!File.Exists(jsonPath))
            {
                MascotMessageBox.Show(
                                    "Ой леле! Я не можу знайти файл списку модів (JSON).\nСпробуй перестворити збірку.",
                                    "Файл зник",
                                    MascotEmotion.Alert);
                return;
            }

            try
            {
                string json = await File.ReadAllTextAsync(jsonPath);
                var mods = JsonConvert.DeserializeObject<List<ModInfo>>(json) ?? new List<ModInfo>();
                if (mods.Count == 0)
                {
                    MascotMessageBox.Show(
                                            "Тут пусто! Список модів порожній, мені нічого качати.",
                                            "Пусто",
                                            MascotEmotion.Confused);
                    return;
                }

                int total = mods.Count;
                int completed = 0;

                foreach (var mod in mods)
                {
                    string subFolder = mod.Type switch
                    {
                        "mod" => "mods",
                        "shader" => "shaderpacks",
                        "resourcepack" => "resourcepacks",
                        _ => "mods"
                    };

                    string basePath = CurrentModpack.Path;
                    string pathPlural = Path.Combine(basePath, "overrides");
                    string pathSingular = Path.Combine(basePath, "override");

                    string finalBasePath;

                    if (Directory.Exists(pathPlural))
                    {
                        finalBasePath = pathPlural; 
                    }
                    else if (Directory.Exists(pathSingular))
                    {
                        finalBasePath = pathSingular; 
                    }
                    else
                    {
                        finalBasePath = basePath; 
                    }

                    string targetDir = Path.Combine(finalBasePath, subFolder);

                    Directory.CreateDirectory(targetDir);
                    string fileName = Path.GetFileName(mod.Url);
                    string filePath = Path.Combine(targetDir, fileName);

                    progress.DowloadProgressBarFileTask(total, completed, fileName);

                    if (File.Exists(filePath))
                    {
                        completed++;
                        progress.DowloadProgressBarFileTask(total, completed, fileName);
                        continue;
                    }

                    bool success = await DownloadFileWithProgress(mod.Url, filePath, progress);
                    if (!success)
                        await HandleManualDownloadPrompt(mod.Url, filePath, fileName);

                    completed++;
                    progress.DowloadProgressBarFileTask(total, completed, fileName);
                }

                File.Delete(jsonPath);

                progress.Close();

                MascotMessageBox.Show(
                                    "Всі моди успішно завантажені!\nПриємної гри!",
                                    "Готово",
                                    MascotEmotion.Happy);
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Біда! Сталася помилка під час завантаження модів.\n\nДеталі: {ex.Message}",
                                    "Збій завантаження",
                                    MascotEmotion.Sad);
            }
        }

        private async Task<bool> DownloadFileWithProgress(string url, string savePath, DowloadProgress progress)
        {
            try
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                bool canReport = totalBytes > 0;
                long totalRead = 0;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    totalRead += bytesRead;
                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    if (canReport)
                    {
                        int percent = (int)(totalRead * 100 / totalBytes);

                        progress.Dispatcher.Invoke(() =>
                        {
                            progress.DowloadProgressBarFile(percent);
                        });

                        await Task.Delay(10); 
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task HandleManualDownloadPrompt(string url, string fullPath, string filename, string errorMessage = "")
        {
            string message = $"Ой, я не можу завантажити цей файл автоматично.\n{filename}\n\nХочеш спробувати відкрити посилання у браузері і скачати вручну?";

            bool result = MascotMessageBox.Ask(
                message,
                "Помилка завантаження",
                MascotEmotion.Sad 
            );

            if (result)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });

                    MascotMessageBox.Show(
                        $"Добре, я відкрила посилання.\nЗбережи файл у цю папку:\n{fullPath}",
                        "Інструкція",
                        MascotEmotion.Alert
                    );
                }
                catch (Exception ex)
                {
                    MascotMessageBox.Show(
                        $"Не вдалося відкрити браузер. Схоже, сьогодні не мій день.\n{ex.Message}",
                        "Помилка",
                        MascotEmotion.Sad);
                }
            }
        }
    }
}
