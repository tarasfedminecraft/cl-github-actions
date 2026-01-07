using CmlLib.Core.ProcessBuilder;
using CmlLib.Core;
using CurseForge.APIClient;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Net.Http;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Diagnostics;
using System.Windows.Shapes;
using Path = System.IO.Path;
using CL_CLegendary_Launcher_.Windows;

namespace CL_CLegendary_Launcher_.Class
{
    public class InstalledModpack
    {
        public string Name { get; set; }
        public string TypeSite { get; set; }
        public string MinecraftVersion { get; set; }
        public string LoaderVersion { get; set; }  
        public string LoaderType { get; set; }
        public string Path { get; set; }
        public string PathJson { get; set; } 
        public string UrlImage { get; set; }
        public bool IsConsoleLogOpened { get; set; } = false;
        public int OPack { get; set; } = 4096;
        public int Wdith { get; set; } = 800;
        public int Height { get; set; } = 600;
        public bool EnterInServer { get; set; } = false;
        public string ServerIP { get; set; } = "IP Сервера";
    }
    public static class ModpackPaths
    {
        public static string DataDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        public static string InstalledModpacksJson => Path.Combine(DataDirectory, "installed_modpacks.json");
    }

    public class ModpackService
    {
        private readonly CL_Main_ _main;
        private readonly GameSessionManager _gameSessionManager;
        private readonly GameLaunchService _gameLaunchService;

        private readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(20);
        private readonly ApiClient _cfApiClient;
        private readonly HttpClient _httpClient;
        private string _apiKey = Secrets.CurseForgeKey;

        public ModpackService(CL_Main_ main, GameSessionManager gameSessionManager, GameLaunchService gameLaunchService)
        {
            _main = main;
            _gameSessionManager = gameSessionManager;
            _gameLaunchService = gameLaunchService;
            _httpClient = new HttpClient();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            _httpClient.DefaultRequestHeaders.ConnectionClose = false;
            _cfApiClient = new ApiClient(_apiKey);
        }

        public List<InstalledModpack> LoadInstalledModpacks()
        {
            string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "installed_modpacks.json");
            if (!File.Exists(jsonPath)) return new List<InstalledModpack>();

            string json = File.ReadAllText(jsonPath);
            return JsonConvert.DeserializeObject<List<InstalledModpack>>(json) ?? new List<InstalledModpack>();
        }
        public void DeleteModpack(string modpackName)
        {
            string pathToJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "installed_modpacks.json");
            if (!File.Exists(pathToJson)) return;

            var jsonText = File.ReadAllText(pathToJson);
            var modpacks = System.Text.Json.JsonSerializer.Deserialize<List<InstalledModpack>>(jsonText);
            if (modpacks == null) return;

            var modpackToDelete = modpacks.Find(mp => mp.Name == modpackName);
            if (modpackToDelete != null)
            {
                modpacks.Remove(modpackToDelete);
                var newJson = System.Text.Json.JsonSerializer.Serialize(modpacks, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(pathToJson, newJson);
            }
        }

        public void DeleteModpackFolder(InstalledModpack value)
        {
            string modpackFolder = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", value.Name);
            if (Directory.Exists(modpackFolder))
            {
                Directory.Delete(modpackFolder, true);
            }

            if (Directory.Exists(value.Path))
            {
                Directory.Delete(value.Path, true);
            }
        }

        public async void PlayModPack(string version, string versionMod, string loader, string nameModPack, string pathModPack, string pathJson, string typeSite)
        {
            if (_main.InstallVersionOnPlay) return;

            var cts = new CancellationTokenSource();
            var token = cts.Token;

            DowloadProgress versionDownloadWindow = new DowloadProgress() { CTS = cts };
            versionDownloadWindow.Title = "DownloadProgress - Завантаження версії та модів";
            _main.Dispatcher.Invoke(() => versionDownloadWindow.Show());
            _main.InstallVersionOnPlay = true;

            try
            {
                string overridePath = Path.Combine(pathModPack, "override");
                string overridesPath = Path.Combine(pathModPack, "overrides");

                string finalModPath = overridePath;
                if (!Directory.Exists(overridePath) && Directory.Exists(overridesPath))
                {
                    finalModPath = overridesPath;
                }
                Directory.CreateDirectory(finalModPath);

                bool downloadSuccess = false;
                if (typeSite == "Modrinth")
                    downloadSuccess = await DownloadModsFromIndexJsonAsync(pathJson, finalModPath, versionDownloadWindow, token);
                else if (typeSite == "CurseForge")
                    downloadSuccess = await DownloadModsFromManifestJsonAsync(pathJson, finalModPath, versionDownloadWindow, token);
                else if (typeSite == "Custom")
                    downloadSuccess = await DownloadModsFromCustomJsonAsync(Path.Combine(pathModPack, "modpack.json"), finalModPath, versionDownloadWindow, token);

                var installedModpack = LoadInstalledModpacks().FirstOrDefault(m => m.Name.Equals(nameModPack, StringComparison.OrdinalIgnoreCase));
                if (installedModpack == null) throw new Exception("Не вдалося знайти збережені налаштування збірки.");

                var path = new MinecraftPath(finalModPath);
                var launcher = new MinecraftLauncher(path);

                launcher.ByteProgressChanged += (sender, args) =>
                {
                    int byteProgress = args.TotalBytes > 0 ? (int)((double)args.ProgressedBytes / args.TotalBytes * 100) : 0;
                    _main.Dispatcher.Invoke(() => versionDownloadWindow.DowloadProgressBarFile(byteProgress));
                };
                launcher.FileProgressChanged += (sender, args) =>
                {
                    int fileProgress = args.TotalTasks > 0 ? (int)((double)args.ProgressedTasks / args.TotalTasks * 100) : 0;
                    _main.Dispatcher.Invoke(() =>
                    {
                        versionDownloadWindow.DowloadProgressBarFileTask(args.TotalTasks, args.ProgressedTasks, args.Name);
                        versionDownloadWindow.DowloadProgressBarVersion(fileProgress, version);
                    });
                };

                MLaunchOption mLaunch = new MLaunchOption
                {
                    MaximumRamMb = installedModpack.OPack,
                    Session = _main.session,
                    ScreenWidth = installedModpack.Wdith,
                    ScreenHeight = installedModpack.Height,
                    ServerIp = (installedModpack.EnterInServer && !string.IsNullOrWhiteSpace(installedModpack.ServerIP)) ? installedModpack.ServerIP.Split(':')[0] : null,
                    ServerPort = (installedModpack.EnterInServer && !string.IsNullOrWhiteSpace(installedModpack.ServerIP) && installedModpack.ServerIP.Contains(':') && int.TryParse(installedModpack.ServerIP.Split(':')[1], out int port)) ? port : 0,
                };
                var activeJvmArgs = new List<string>();

                if (_main.selectAccountNow == AccountType.LittleSkin)
                {
                    activeJvmArgs.Add($@"-javaagent:{AppContext.BaseDirectory}authlib-injector-1.2.7.jar=https://littleskin.cn/api/yggdrasil");
                }

                if (activeJvmArgs.Count > 0)
                {
                    mLaunch.JvmArgumentOverrides = activeJvmArgs
                        .Select(arg => new MArgument { Values = new[] { arg } })
                        .ToArray();
                }

                LoaderType loaderType;

                string lowerLoader = loader.ToLower();

                if (lowerLoader.Contains("vanilla") || lowerLoader.Contains("vanila"))
                {
                    loaderType = LoaderType.Vanilla;
                    versionMod = null;
                }
                else if (lowerLoader.Contains("quilt"))
                {
                    loaderType = LoaderType.Quilt;
                }
                else if (lowerLoader.Contains("fabric"))
                {
                    loaderType = LoaderType.Fabric;
                }
                else if (lowerLoader.Contains("neoforge"))
                {
                    loaderType = LoaderType.NeoForge;
                }
                else if (lowerLoader.Contains("forge"))
                {
                    loaderType = LoaderType.Forge;
                }
                else if (lowerLoader.Contains("optifine"))
                {
                    loaderType = LoaderType.Optifine;
                }
                else if (lowerLoader.Contains("liteloader"))
                {
                    loaderType = LoaderType.LiteLoader;
                }
                else
                {
                    if (Enum.TryParse(typeof(LoaderType), loader, true, out object result))
                    {
                        loaderType = (LoaderType)result;
                    }
                    else
                    {
                        loaderType = LoaderType.Custom_Local;
                    }
                }

                string versionName = await _gameLaunchService.InstallVersionAsync(loaderType, version, versionMod, launcher, token);

                var process = await launcher.InstallAndBuildProcessAsync(versionName, mLaunch, token);
                _gameSessionManager.StartGameSession("mod");

                _main.Dispatcher.Invoke(() =>
                {
                    versionDownloadWindow.Close();
                    _main.WindowState = WindowState.Minimized;                    
                });

                await DiscordController.UpdatePresence($"Грає в мод-збірку {nameModPack}");

                if (installedModpack.IsConsoleLogOpened)
                    _main.ShowGameLog(process);
                else
                    process.Start();

                if (Settings1.Default.CloseLaucnher)
                {
                    _main.Dispatcher.Invoke(() => _main.Close());
                }
                await MemoryCleaner.FlushMemoryAsync(trimWorkingSet: true);
                await process.WaitForExitAsync();
            }
            catch (OperationCanceledException)
            {
                _main.Dispatcher.Invoke(() => versionDownloadWindow.Close());
                MascotMessageBox.Show(
                                    "Добре, я зупинила завантаження модпаку.\nСпробуємо іншим разом!",
                                    "Скасовано",
                                    MascotEmotion.Normal);
            }
            catch (Exception ex)
            {
                _main.Dispatcher.Invoke(() => versionDownloadWindow.Close());
                MascotMessageBox.Show(
                                    $"Біда! Щось зламалося під час запуску модпаку.\n\nДеталі: {ex.Message}",
                                    "Помилка",
                                    MascotEmotion.Sad);
            }
            finally
            {
                _gameSessionManager.StopGameSession();
                _main.Dispatcher.Invoke(() =>
                {
                    _main.InstallVersionOnPlay = false;
                    _main.PlayTXT.Content = "ГРАТИ";
                });
            }
        }
        private async Task<bool> DownloadModsFromManifestJsonAsync(string pathJson, string packFolder, DowloadProgress progress, CancellationToken token)
        {
            if (!File.Exists(pathJson) || Settings1.Default.OfflineModLauncher) return false;

            try
            {
                string json = await File.ReadAllTextAsync(pathJson);
                var manifest = JsonConvert.DeserializeObject<JObject>(json);
                var files = manifest["files"] as JArray;
                if (files == null || files.Count == 0) return false;

                int total = files.Count;
                int completed = 0;
                var downloadTasks = new List<Task>();

                foreach (var modEntry in files)
                {
                    downloadTasks.Add(Task.Run(async () =>
                    {
                        await _downloadSemaphore.WaitAsync(token);
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            int projectId = modEntry.Value<int>("projectID");
                            int fileId = modEntry.Value<int>("fileID");

                            var file = await _cfApiClient.GetModFileAsync(projectId, fileId);
                            var data = file?.Data;
                            if (data == null) return;

                            string downloadUrl = data.DownloadUrl;
                            string fileName = data.FileName;

                            if (!string.IsNullOrEmpty(downloadUrl) && !string.IsNullOrEmpty(fileName))
                            {
                                string subFolder = GetFolderByFileType(fileName);
                                string targetDir = Path.Combine(packFolder, subFolder);
                                Directory.CreateDirectory(targetDir);
                                string fullPath = Path.Combine(targetDir, fileName);

                                if (!File.Exists(fullPath))
                                {
                                    _main.Dispatcher.BeginInvoke(() => progress.DowloadProgressBarFileTask(total, completed, fileName));
                                    bool success = await DownloadFileWithProgress(downloadUrl, fullPath, progress, token);
                                    if (!success) await HandleManualDownloadPrompt(downloadUrl, fullPath, fileName);
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            _downloadSemaphore.Release(); 
                            Interlocked.Increment(ref completed);
                            _main.Dispatcher.BeginInvoke(() => progress.DowloadProgressBarFileTask(total, completed, ""));
                        }
                    }, token));
                }
                await Task.WhenAll(downloadTasks);
                return true;
            }
            catch { return false; }
        }
        private async Task<bool> DownloadModsFromIndexJsonAsync(string pathJson, string packFolder, DowloadProgress progress, CancellationToken token)
        {
            if (!File.Exists(pathJson) || Settings1.Default.OfflineModLauncher) return false;
            try
            {
                string json = await File.ReadAllTextAsync(pathJson);
                JObject index = JObject.Parse(json);
                var files = index["files"] as JArray;
                if (files == null || files.Count == 0) return false;

                int total = files.Count;
                int completed = 0;
                var downloadTasks = new List<Task>();

                foreach (var file in files)
                {
                    downloadTasks.Add(Task.Run(async () =>
                    {
                        string fileName = "";

                        await _downloadSemaphore.WaitAsync(token);
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            string relativePath = file["path"]?.ToString();
                            var urls = file["downloads"] as JArray;
                            string downloadUrl = urls?[0]?.ToString();

                            if (!string.IsNullOrWhiteSpace(relativePath) && !string.IsNullOrWhiteSpace(downloadUrl))
                            {
                                fileName = Path.GetFileName(relativePath);
                                string subFolder = GetFolderByFileType(fileName);
                                string targetDir = Path.Combine(packFolder, subFolder);
                                Directory.CreateDirectory(targetDir);
                                string fullPath = Path.Combine(targetDir, fileName);

                                if (!File.Exists(fullPath))
                                {
                                    _main.Dispatcher.BeginInvoke(() => progress.DowloadProgressBarFileTask(total, completed, fileName));
                                    bool success = await DownloadFileWithProgress(downloadUrl, fullPath, progress, token);
                                    if (!success) await HandleManualDownloadPrompt(downloadUrl, fullPath, fileName);
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            _downloadSemaphore.Release();
                            Interlocked.Increment(ref completed);
                            _main.Dispatcher.BeginInvoke(() => progress.DowloadProgressBarFileTask(total, completed, fileName));
                        }
                    }, token));
                }
                await Task.WhenAll(downloadTasks);
                return true;
            }
            catch { return false; }
        }
        private async Task<bool> DownloadModsFromCustomJsonAsync(string jsonPath, string packFolder, DowloadProgress progress, CancellationToken token)
        {
            if (Settings1.Default.OfflineModLauncher || !File.Exists(jsonPath)) return false;
            try
            {
                string json = await File.ReadAllTextAsync(jsonPath);
                var mods = JsonConvert.DeserializeObject<List<ModInfo>>(json) ?? new List<ModInfo>();
                if (mods.Count == 0) return false;

                int total = mods.Count;
                int completed = 0;
                var downloadTasks = new List<Task>();

                foreach (var mod in mods)
                {
                    downloadTasks.Add(Task.Run(async () =>
                    {
                        await _downloadSemaphore.WaitAsync(token);
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            string subFolder = mod.Type switch
                            {
                                "mod" => "mods",
                                "shader" => "shaderpacks",
                                "resourcepack" => "resourcepacks",
                                _ => "mods"
                            };
                            string targetDir = Path.Combine(packFolder, subFolder);
                            Directory.CreateDirectory(targetDir);
                            string fileName = Path.GetFileName(mod.Url);
                            string filePath = Path.Combine(targetDir, fileName);

                            if (!File.Exists(filePath))
                            {
                                _main.Dispatcher.BeginInvoke(() => progress.DowloadProgressBarFileTask(total, completed, fileName));
                                bool success = await DownloadFileWithProgress(mod.Url, filePath, progress, token);
                                if (!success) await HandleManualDownloadPrompt(mod.Url, filePath, fileName);
                            }
                        }
                        catch { }
                        finally
                        {
                            _downloadSemaphore.Release();
                            Interlocked.Increment(ref completed);
                            _main.Dispatcher.BeginInvoke(() => progress.DowloadProgressBarFileTask(total, completed, ""));
                        }
                    }, token));
                }
                await Task.WhenAll(downloadTasks);
                return true;
            }
            catch { return false; }
        }

        private async Task<bool> DownloadFileWithProgress(string url, string savePath, DowloadProgress progress, CancellationToken token)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                bool canReport = totalBytes > 0;
                long totalRead = 0;

                string tempPath = savePath + ".tmp";

                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
                using var contentStream = await response.Content.ReadAsStreamAsync(token);

                byte[] buffer = new byte[81920]; 
                int bytesRead;
                long lastReportedBytes = 0; 

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                    totalRead += bytesRead;

                    if (canReport)
                    {
                        if (totalRead - lastReportedBytes > 102400 || totalRead == totalBytes)
                        {
                            lastReportedBytes = totalRead;
                            int percent = (int)(totalRead * 100 / totalBytes);
                            _main.Dispatcher.BeginInvoke(() => progress.DowloadProgressBarFile(percent));
                        }
                    }
                }

                await fileStream.DisposeAsync();
                File.Move(tempPath, savePath, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private string GetFolderByFileType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();

            if (ext == ".jar")
                return "mods";

            if (ext == ".zip")
            {
                string name = fileName.ToLower();
                if (name.Contains("shader") || name.Contains("bsl") || name.Contains("seus") || name.Contains("sildur"))
                    return "shaderpacks";
                if (name.Contains("resource") || name.Contains("pack") || name.Contains("texture"))
                    return "resourcepacks";
            }

            return "mods";
        }

        private async Task HandleManualDownloadPrompt(string url, string fullPath, string filename, string errorMessage = "")
        {
            bool result = MascotMessageBox.Ask(
                            $"Ой, я не змогла завантажити цей файл:\n{filename}\n\nСпробуєш скачати його вручну?",
                            "Помилка завантаження",
                            MascotEmotion.Sad);

            if (result == true)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });

                    MascotMessageBox.Show(
                                            $"Я відкрила посилання. Будь ласка, збережи файл ось сюди:\n{fullPath}",
                                            "Інструкція",
                                            MascotEmotion.Alert);
                }
                catch (Exception ex)
                {
                    MascotMessageBox.Show(
                                            $"Не вдалося відкрити посилання у браузері.\n{ex.Message}",
                                            "Збій",
                                            MascotEmotion.Sad);
                }
            }
        }
        public void AddModpack(InstalledModpack modpack)
        {
            string jsonPath = ModpackPaths.InstalledModpacksJson;

            List<InstalledModpack> modpacks = new();

            if (File.Exists(jsonPath))
            {
                try
                {
                    string existingJson = File.ReadAllText(jsonPath);
                    modpacks = JsonConvert.DeserializeObject<List<InstalledModpack>>(existingJson) ?? new();
                }
                catch (Exception ex) {
                    MascotMessageBox.Show(
                        $"Ой! Файл конфігурації збірок пошкоджено.\n{ex.Message}",
                        "Помилка",
                        MascotEmotion.Sad);
                }
            }

            if (!modpacks.Any(m => m.Name.Equals(modpack.Name, StringComparison.OrdinalIgnoreCase)))
            {
                modpacks.Add(modpack);

                string newJson = JsonConvert.SerializeObject(modpacks, Formatting.Indented);
                File.WriteAllText(jsonPath, newJson);
            }
        }
        public async Task ImportModpackAsync(string zipFilePath)
        {
            string extractPath = "";
            try
            {
                string packName = Path.GetFileNameWithoutExtension(zipFilePath);
                extractPath = Path.Combine(Settings1.Default.PathLacunher, "CLModpack", packName);

                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                Directory.CreateDirectory(extractPath);

                await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, extractPath));

                string modrinthPath = Path.Combine(extractPath, "modrinth.index.json");
                string cursePath = Path.Combine(extractPath, "manifest.json");
                string customPath = Path.Combine(extractPath, "modpack.json");

                InstalledModpack newPack = null;

                if (File.Exists(modrinthPath))
                {
                    string json = await File.ReadAllTextAsync(modrinthPath);
                    JObject index = JObject.Parse(json);

                    string version = index["dependencies"]?["minecraft"]?.ToString();
                    var deps = index["dependencies"] as JObject;
                    string loaderKey = deps?.Properties()
                        .FirstOrDefault(p => p.Name.Contains("fabric") || p.Name.Contains("forge") || p.Name.Contains("quilt") || p.Name.Contains("neoforge"))?.Name;
                    string loaderVer = index["dependencies"]?[loaderKey]?.ToString();

                    newPack = new InstalledModpack
                    {
                        Name = packName,
                        TypeSite = "Modrinth",
                        MinecraftVersion = version ?? "Unknown",
                        LoaderType = loaderKey ?? "Unknown",
                        LoaderVersion = loaderVer,
                        Path = extractPath,
                        PathJson = modrinthPath,
                        UrlImage = "pack://application:,,,/Icon/IconCL(Common).png"
                    };
                }
                else if (File.Exists(cursePath))
                {
                    string json = await File.ReadAllTextAsync(cursePath);
                    JObject manifest = JObject.Parse(json);

                    string version = manifest["minecraft"]?["version"]?.ToString();
                    string loaderFull = manifest["minecraft"]?["modLoaders"]?[0]?["id"]?.ToString();
                    string loader = loaderFull?.Split('-')[0];
                    string loaderVer = loaderFull?.Contains("-") == true ? loaderFull.Split('-')[1] : loaderFull;

                    newPack = new InstalledModpack
                    {
                        Name = manifest["name"]?.ToString() ?? packName,
                        TypeSite = "CurseForge",
                        MinecraftVersion = version,
                        LoaderType = loader,
                        LoaderVersion = loaderVer,
                        Path = extractPath,
                        PathJson = cursePath,
                        UrlImage = "pack://application:,,,/Icon/IconCL(Common).png"
                    };
                }
                else if (File.Exists(customPath))
                {
                    string json = await File.ReadAllTextAsync(customPath);
                    var modList = JsonConvert.DeserializeObject<List<ModInfo>>(json);

                    if (modList != null && modList.Count > 0)
                    {
                        var firstMod = modList[0];

                        newPack = new InstalledModpack
                        {
                            Name = packName,
                            TypeSite = "Custom",
                            MinecraftVersion = firstMod.Version ?? "Unknown",
                            LoaderType = firstMod.LoaderType ?? "Unknown",
                            LoaderVersion = firstMod.Loader ?? "Unknown",

                            Path = extractPath,
                            PathJson = customPath,
                            UrlImage = "pack://application:,,,/Icon/IconCL(Common).png"
                        };
                    }
                    else
                    {
                        newPack = new InstalledModpack
                        {
                            Name = packName,
                            TypeSite = "Custom",
                            MinecraftVersion = "Unknown",
                            LoaderType = "Unknown",
                            Path = extractPath,
                            PathJson = customPath,
                            UrlImage = "pack://application:,,,/Icon/IconCL(Common).png"
                        };
                    }
                }

                if (newPack != null)
                {
                    AddModpack(newPack);
                    MascotMessageBox.Show(
                                            "Ура! Модпак успішно імпортовано. Можеш грати!",
                                            "Готово!",
                                            MascotEmotion.Happy);
                }
                else
                {
                    MascotMessageBox.Show(
                                            "Хм, я розпакувала архів, але не знайшла там файлів конфігурації (modpack.json або manifest.json).\n" +
                                            "Можливо, це неправильний формат?",
                                            "Помилка структури",
                                            MascotEmotion.Confused);
                    if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ой леле! Сталася помилка під час імпорту модпаку.\n{ex.Message}",
                                    "Збій імпорту",
                                    MascotEmotion.Sad);
            }
        }
    }
}
