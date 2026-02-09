using CL_CLegendary_Launcher_.Class;
using CL_CLegendary_Launcher_.Windows;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Path = System.IO.Path;

namespace CL_CLegendary_Launcher_
{
    public enum AccountType
    {
        Microsoft,
        LittleSkin,
        Offline
    }

    public partial class CL_Main_ : FluentWindow
    {
        byte VersionSelect = 0;
        public bool InstallVersionOnPlay = false;

        public AccountType selectAccountNow;
        bool MicosoftAccount = false;
        JELoginHandler loginHandler;
        public MSession session;

        public List<string> donateLink = new List<string>();
        public List<string> siteLink = new List<string>();
        public List<string> discordLink = new List<string>();

        private bool isSliderDragging = false;
        private double previousSliderValue = 2048;

        string VersionType = "Fabric";
        string SiteMods = "Modrinth";
        string ModType = "Collection";
        byte selectmodificed = 0;
        byte SelectModPackCreate = 0;

        private int _currentPage = 0;
        private const int ITEMS_PER_PAGE = 10; 
        private CancellationTokenSource _searchCts;
        private List<ModVersionInfo> _currentModVersions;
        private List<InstalledModpack> allInstalledModpacks = new List<InstalledModpack>();

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private TutorialOverlayService _tutorialService;
        private  ScreenshotService _screenshotService;
        public  ServerListService _serverListService;
        protected ProfileManagerService _profileManagerService;
        private AccountService _accountService;
        protected GameSessionManager _gameSessionManager;
        protected GameLaunchService _gameLaunchService;
        protected LastActionService _lastActionService;
        private ModDownloadService _modDownloadService;
        public ModpackService _modpackService;
        private ThemeService _themeService;
        private LauncherSettingsService _launcherSettingsService;
        private LauncherNavigationService _navigationService;
        public VersionService _versionService;
        private NewsService _newsService;
        public CL_Main_()
        {
            InitializeComponent();
            this.AllowDrop = false;
            CheckAndCreateDefaultPath();
            InitializeServices();

            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            ApplicationThemeManager.Apply(this);
            _themeService.InitializeTheme();
            LoadCustomSettings();
            InitToggles();       

            _serverListService.InitializeServersAsync(false, null);
            _lastActionService.LoadLastActionsFromJsonAsync();
            LoadChangeLogMinecraft(); 
            LoadProfilesAsync(); 

            _launcherSettingsService.Initialize();

            DiscordController.Initialize("В головному вікні");
            InitializeThemeSelection();
        }
        public void InitializeServices()
        {
            loginHandler = JELoginHandlerBuilder.BuildDefault();

            _versionService = new VersionService(Settings1.Default.PathLacunher);

            _tutorialService = new TutorialOverlayService(
                this, TutorialOverlay, OverlayHoleRect, OverlayScreenRect, TutorialMessageParams, TutorialTitleText, TutorialBodyText
            );

            _themeService = new ThemeService(this);
            _newsService = new NewsService();
            _navigationService = new LauncherNavigationService(this);
            _launcherSettingsService = new LauncherSettingsService(this);
            _profileManagerService = new ProfileManagerService();
            _accountService = new AccountService(_profileManagerService);
            _gameSessionManager = new GameSessionManager();
            _serverListService = new ServerListService(this);
            _lastActionService = new LastActionService(this);
            _gameLaunchService = new GameLaunchService(this, _gameSessionManager, _lastActionService);
            _modDownloadService = new ModDownloadService(this);

            _modpackService = new ModpackService(this, _gameSessionManager, _gameLaunchService,_modDownloadService);
            _screenshotService = new ScreenshotService();
        }

        private void CheckAndCreateDefaultPath()
        {
            if (string.IsNullOrWhiteSpace(Settings1.Default.PathLacunher))
            {
                string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".ClMinecraft");
                if (!Directory.Exists(defaultPath))
                {
                    Directory.CreateDirectory(defaultPath);
                }
                Settings1.Default.PathLacunher = defaultPath;
                Settings1.Default.Save();
            }
        }
        private async void MainTitleBar_MinimizeClicked(TitleBar sender, System.Windows.RoutedEventArgs args)
        {
           await MemoryCleaner.FlushMemoryAsync(true);
        }
    }
}