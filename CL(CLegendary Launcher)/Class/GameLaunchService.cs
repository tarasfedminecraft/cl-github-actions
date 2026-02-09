using CL_CLegendary_Launcher_.Windows;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installer.NeoForge.Installers;
using CmlLib.Core.Installers;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.LiteLoader;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using Optifine.Installer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Path = System.IO.Path;

namespace CL_CLegendary_Launcher_.Class
{
    public enum LoaderType
    {
        Vanilla,
        Forge,
        Fabric,
        Quilt,
        Optifine,
        NeoForge,
        LiteLoader,
        Custom_Local,
        OmniArchive
    }

    public class GameLaunchService
    {
        private readonly CL_Main_ _main;
        private readonly GameSessionManager _gameSessionManager;
        private readonly LastActionService _lastActionService;
        private CancellationTokenSource _cts;

        public GameLaunchService(CL_Main_ main, GameSessionManager sessionManager, LastActionService lastActionService)
        {
            _main = main;
            _gameSessionManager = sessionManager;
            _lastActionService = lastActionService;
        }
        public async Task LaunchGameAsync(LoaderType loaderType, string minecraftVersion, string loaderVersion, string serverIp = null, int? serverPort = null)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _main.Dispatcher.Invoke(() =>
            {
                _main.InstallVersionOnPlay = true;
                _main.PlayTXT.Text = "ЗАВАНТАЖЕННЯ";
            });

            DowloadProgress dowloadProgress = new DowloadProgress { CTS = _cts };
            _main.Dispatcher.Invoke(() => dowloadProgress.Show());

            try
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 256;
                var path = new MinecraftPath(Settings1.Default.PathLacunher);

                var httpClient = new HttpClient();
                int safeThreads = Math.Clamp(Environment.ProcessorCount * 2, 4, 16);

                var parallelInstaller = new ParallelGameInstaller(
                    maxChecker: 32,
                    maxDownloader: safeThreads,
                    boundedCapacity: 2048,
                    httpClient
                );

                var parameters = MinecraftLauncherParameters.CreateDefault(path);
                parameters.GameInstaller = parallelInstaller;

                var launcher = new MinecraftLauncher(parameters);

                launcher.FileProgressChanged += (sender, args) =>
                {
                    _main.Dispatcher.Invoke(() =>
                    {
                        int fileProgress = args.TotalTasks > 0 ? (int)((double)args.ProgressedTasks / args.TotalTasks * 100) : 0;
                        dowloadProgress.DowloadProgressBarFileTask(args.TotalTasks, args.ProgressedTasks, args.Name);
                        string versionLabel = string.IsNullOrEmpty(loaderVersion) ? minecraftVersion : $"{minecraftVersion} ({loaderVersion})";
                        dowloadProgress.DowloadProgressBarVersion(fileProgress, versionLabel);
                    });
                };

                launcher.ByteProgressChanged += (sender, args) =>
                {
                    _main.Dispatcher.Invoke(() =>
                    {
                        int byteProgress = args.TotalBytes > 0 ? (int)((double)args.ProgressedBytes / args.TotalBytes * 100) : 0;
                        dowloadProgress.DowloadProgressBarFile(byteProgress);
                    });
                };

                string versionName = await InstallVersionAsync(loaderType, minecraftVersion, loaderVersion, launcher, token);

                if (string.IsNullOrEmpty(versionName))
                {
                    MascotMessageBox.Show(
                                            "Ой леле! Я намагалася встановити цю версію, але нічого не вийшло.\nСпробуй ще раз пізніше.",
                                            "Помилка встановлення",
                                            MascotEmotion.Sad);
                    return; 
                }

                var launchOption = CreateLaunchOptions(serverIp, serverPort);

                if (Settings1.Default.EnableAutoBackup)
                {
                    _main.Dispatcher.Invoke(() => _main.PlayTXT.Text = "БЕКАП СВІТІВ...");

                    string gameDir = path.BasePath;
                    string savesPath = Path.Combine(gameDir, "saves");

                    if (Directory.Exists(savesPath))
                    {
                        await Task.Run(async () =>
                        {
                            try
                            {
                                var worlds = Directory.GetDirectories(savesPath);
                                foreach (var world in worlds)
                                {
                                    await WorldBackupService.AutoBackupWorldAsync(world);
                                }
                            }
                            catch (Exception ex)
                            {
                                _main.Dispatcher.Invoke(() =>
                                {
                                    NotificationService.ShowNotification("Йой! Помилка при створення бекапів!", $"Помилка авто-бекапу: {ex.Message}", _main.SnackbarPresenter, 10);
                                });

                                System.Diagnostics.Debug.WriteLine($"Backup error: {ex}");
                            }
                        });
                    }
                }
                _main.Dispatcher.Invoke(() => _main.PlayTXT.Text = "ЗАПУСК...");

                var process = await launcher.InstallAndBuildProcessAsync(versionName, launchOption, token);

                _main.Dispatcher.Invoke(() =>
                {
                    dowloadProgress.Close();
                    _main.WindowState = WindowState.Minimized;
                });

                await DiscordController.UpdatePresence($"Грає версію {versionName}");

                if (Settings1.Default.EnableLog)
                {
                    _main.ShowGameLog(process);
                }
                else
                {
                    process.Start();
                }

                string loaderName = loaderType.ToString();
                var action = new Dictionary<string, string>
                {
                    ["type"] = loaderType == LoaderType.Vanilla ? "version" : "version",
                    ["name"] = loaderName,
                    ["version"] = minecraftVersion,
                    ["loader"] = loaderName.ToLower(),
                    ["loaderVersion"] = loaderVersion
                };
                await _lastActionService.AddLastActionAsync(action);

                if (Settings1.Default.CloseLaucnher)
                {
                    _main.Dispatcher.Invoke(() => _main.Close());
                }

                _gameSessionManager.StartGameSession(loaderType == LoaderType.Vanilla && serverIp == null ? "vanilla" : "mod");
                await MemoryCleaner.FlushMemoryAsync(trimWorkingSet: true);
                await process.WaitForExitAsync();
            }
            catch (OperationCanceledException)
            {
                MascotMessageBox.Show(
                    "Гаразд, я зупинила завантаження.\nМи можемо спробувати знову, коли ти будеш готовий!",
                    "Скасовано",
                    MascotEmotion.Normal);
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                    $"Ой! Сталася помилка під час запуску гри.\n\nДеталі: {ex.Message}",
                    "Помилка",
                    MascotEmotion.Sad);
            }
            finally
            {
                _gameSessionManager.StopGameSession();
                _main.Dispatcher.Invoke(() =>
                {
                    _main.InstallVersionOnPlay = false;
                    _main.PlayTXT.Text = $"ГРАТИ ({Settings1.Default.LastSelectedVersion}:{Settings1.Default.LastSelectedModVersion})";
                    if (dowloadProgress.IsLoaded) dowloadProgress.Close();
                });
            }
        }
        #region OmniArchive LogicInstall
        //    private async Task<string> InstallOmniArchiveAsync(string versionName, string downloadUrl, string category, MinecraftLauncher launcher, CancellationToken token)
        //    {
        //        var path = launcher.MinecraftPath;
        //        string versionDir = Path.Combine(path.Versions, versionName);
        //        string jarPath = Path.Combine(versionDir, $"{versionName}.jar");
        //        string jsonPath = Path.Combine(versionDir, $"{versionName}.json");

        //        if (!Directory.Exists(versionDir))
        //            Directory.CreateDirectory(versionDir);

        //        if (!File.Exists(jarPath))
        //        {
        //            using (var client = new HttpClient())
        //            {
        //                var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, token);
        //                response.EnsureSuccessStatusCode();

        //                using (var fs = new FileStream(jarPath, FileMode.Create, FileAccess.Write, FileShare.None))
        //                {
        //                    await response.Content.CopyToAsync(fs);
        //                }
        //            }
        //        }

        //        if (!File.Exists(jsonPath))
        //        {
        //            string type = "old_alpha"; 
        //            string args = "";

        //            switch (category.ToLower())
        //            {
        //                case "classic":
        //                case "pre-classic":
        //                    type = "old_alpha";
        //                    args = "\"${auth_player_name} ${auth_session}\"";
        //                    break;
        //                case "indev":
        //                case "infdev":
        //                    type = "old_alpha";
        //                    args = "\"${auth_player_name} ${auth_session}\"";
        //                    break;
        //                case "beta":
        //                    type = "old_beta";
        //                    args = "\"${auth_player_name} ${auth_session}\"";
        //                    break;
        //                default:
        //                    type = "release";
        //                    args = "\"--username ${auth_player_name} --session ${auth_session} --version ${version_name}\"";
        //                    break;
        //            }
        //            string jsonContent = $@"{{
        //""id"": ""{versionName}"",
        //""inheritsFrom"": ""1.6.4"",
        //""type"": ""{type}"",
        //""mainClass"": ""net.minecraft.client.Minecraft"",
        //""minecraftArguments"": {args},
        //""libraries"": []
        //                }}";

        //            await File.WriteAllTextAsync(jsonPath, jsonContent);
        //        }

        //        await launcher.InstallAsync(versionName, token);

        //        string resourcesPath = Path.Combine(launcher.MinecraftPath.BasePath, "resources");
        //        if (!Directory.Exists(resourcesPath)) Directory.CreateDirectory(resourcesPath);

        //        return versionName;
        //    }
        #endregion
        public async Task<string> InstallVersionAsync(LoaderType loaderType, string mcVersion, string loaderVersion, MinecraftLauncher launcher, CancellationToken token)
        {
            switch (loaderType)
            {
                #region OmniArchive Loader
                //case LoaderType.OmniArchive:
                //    string category = "Infdev"; 

                //    if (OmniArchiveService.OmniDownloadLinks.ContainsKey(mcVersion))
                //    {
                //        var foundVersion = _main._cachedOmniVersions?.FirstOrDefault(v => v.Name == mcVersion);
                //        if (foundVersion != null) category = foundVersion.Category;
                //    }
                //    return await InstallOmniArchiveAsync(mcVersion, loaderVersion, category, launcher, token);
                #endregion
                case LoaderType.Forge:
                    var forge = new ForgeInstaller(launcher);
                    return await forge.Install(mcVersion, loaderVersion, new ForgeInstallOptions { CancellationToken = token });

                case LoaderType.Fabric:
                    var fabricInstaller = new FabricInstaller(new HttpClient());
                    return await fabricInstaller.Install($"{mcVersion}", $"{loaderVersion}", launcher.MinecraftPath);

                case LoaderType.Quilt:
                    var quiltInstaller = new QuiltInstaller(new HttpClient());
                    return await quiltInstaller.Install($"{mcVersion}", $"{loaderVersion}", launcher.MinecraftPath);

                case LoaderType.NeoForge:
                    var neoForge = new NeoForgeInstaller(launcher);
                    return await neoForge.Install($"{mcVersion}", $"{loaderVersion}", new NeoForgeInstallOptions { CancellationToken = token });

                case LoaderType.Optifine:
                    {
                        var loader = new OptifineInstaller(new HttpClient());
                        var versions = await loader.GetOptifineVersionsAsync();
                        var selectedVersion = versions.FirstOrDefault(x => x.Version == loaderVersion);

                        if (selectedVersion == null)
                            throw new Exception("Обрана версія Optifine не знайдена.");

                        await launcher.InstallAsync(selectedVersion.MinecraftVersion, token);

                        var optifineVersionName = $"{selectedVersion.MinecraftVersion}-OptiFine_{selectedVersion.OptifineEdition}";
                        var optifineDir = Path.Combine(launcher.MinecraftPath.Versions, optifineVersionName);
                        var jarPath = Path.Combine(optifineDir, $"{optifineVersionName}.jar");

                        string finalVersionName = optifineVersionName;

                        if (!File.Exists(jarPath))
                        {
                            finalVersionName = await loader.InstallOptifineAsync(launcher.MinecraftPath.BasePath, selectedVersion);

                            if (!File.Exists(jarPath))
                            {
                                await Task.Delay(2000);
                                if (!File.Exists(jarPath))
                                {
                                    throw new Exception("Інсталятор Optifine завершився, але .jar файл не знайдено.");
                                }
                            }
                        }

                        return finalVersionName;
                    }
                case LoaderType.LiteLoader:
                    {
                        var liteLoaderInstaller = new LiteLoaderInstaller(new HttpClient());
                        var loaders = await liteLoaderInstaller.GetAllLiteLoaders();
                        var loaderToInstall = loaders.First(loader => loader.BaseVersion == mcVersion);

                        return await liteLoaderInstaller.Install(loaderToInstall, await launcher.GetVersionAsync(mcVersion), launcher.MinecraftPath);
                    }
                case LoaderType.Custom_Local:
                    {
                        return mcVersion;
                    }
                default:
                    await launcher.InstallAsync($"{mcVersion}", token);
                    return mcVersion;
            }
        }
        public MLaunchOption CreateLaunchOptions(string serverIp, int? serverPort)
        {
            var baseOptions = new MLaunchOption
            {
                MaximumRamMb = (int)_main.OPSlider.Value,
                Session = _main.session,
                ScreenWidth = int.Parse(_main.Width.Text),
                ScreenHeight = int.Parse(_main.Height.Text),
                FullScreen = Settings1.Default.FullScreen,
                ServerIp = serverIp,
                ServerPort = serverPort ?? 0
            };

            if (_main.selectAccountNow == AccountType.LittleSkin)
            {
                var jvmArgs = new List<MArgument>
                {
                    new MArgument
                    {
                        Values = new[] { $@"-javaagent:{AppContext.BaseDirectory}authlib-injector-1.2.7.jar=https://littleskin.cn/api/yggdrasil" }
                    }
                };
                baseOptions.JvmArgumentOverrides = jvmArgs;
            }

            return baseOptions;
        }
    }
}