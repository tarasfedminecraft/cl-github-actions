using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge.Installers;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core;
using Optifine.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;
using System.IO;
using CmlLib.Core.ModLoaders.LiteLoader;
using Path = System.IO.Path;
using CL_CLegendary_Launcher_.Windows;

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
                _main.PlayTXT.Content = "ЗАВАНТАЖЕННЯ";
            });

            DowloadProgress dowloadProgress = new DowloadProgress { CTS = _cts };
            _main.Dispatcher.Invoke(() => dowloadProgress.Show());

            try
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 1000000;

                var path = new MinecraftPath(Settings1.Default.PathLacunher);
                var launcher = new MinecraftLauncher(path);

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
                }

                var launchOption = CreateLaunchOptions(serverIp, serverPort);

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
                    _main.PlayTXT.Content = $"ГРАТИ ({Settings1.Default.LastSelectedVersion}:{Settings1.Default.LastSelectedModVersion})";
                    if (dowloadProgress.IsLoaded) dowloadProgress.Close();
                });
            }

        }
        public async Task<string> InstallVersionAsync(LoaderType loaderType, string mcVersion, string loaderVersion, MinecraftLauncher launcher, CancellationToken token)
        {
            switch (loaderType)
            {
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