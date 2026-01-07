using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CL_CLegendary_Launcher_.Class
{
    public class LauncherNavigationService
    {
        private readonly CL_Main_ _main;
        private bool _isNavigating = false; 

        public LauncherNavigationService(CL_Main_ main)
        {
            _main = main;
        }

        public async void NavigateToHome()
        {
            if (_isNavigating || _main.GirdPanelFooter.Visibility == Visibility.Visible) return;

            try
            {
                _isNavigating = true;

                await DiscordController.UpdatePresence("В головному вікні");
                _main.Click();

                AnimationService.AnimateBorder(0, 0, _main.PanelSelectNow);

                await _main.HideAllPages();

                AnimationService.AnimatePageTransition(_main.GirdPanelFooter);
                AnimationService.AnimatePageTransition(_main.SelectGirdAccount);

                ClearAllLists();

                if (_main.PartnerServer.Items.Count == 0)
                {
                    await _main._serverListService.ReloadServers();
                }

                await MemoryCleaner.FlushMemoryAsync(trimWorkingSet: false);
            }
            finally
            {
                await Task.Delay(200);
                _isNavigating = false;
            }
        }

        public async void NavigateToMods()
        {
            if (_isNavigating || _main.ListModsGird.Visibility == Visibility.Visible) return;

            try
            {
                _isNavigating = true;

                _main.Click();
                AnimationService.AnimateBorder(145, 0, _main.PanelSelectNow);

                await _main.HideAllPages();
                AnimationService.AnimatePageTransition(_main.ListModsGird);

                _main.ModsDowloadList.Items.Clear();
                await _main.UpdateModsMinecraftAsync();

                ClearAllLists();
                await MemoryCleaner.FlushMemoryAsync(trimWorkingSet: false);
            }
            finally
            {
                await Task.Delay(200);
                _isNavigating = false;
            }
        }

        public async void NavigateToModPacks()
        {
            if (_isNavigating || _main.ListModsBuild.Visibility == Visibility.Visible) return;

            try
            {
                _isNavigating = true;

                _main.ModsDowloadList1.Items.Clear();
                await DiscordController.UpdatePresence("Дивиться збірки-модів");
                _main.Click();

                AnimationService.AnimateBorder(70, 0, _main.PanelSelectNow);

                await _main.HideAllPages();
                AnimationService.AnimatePageTransition(_main.ListModsBuild);

                var valueList = _main._modpackService.LoadInstalledModpacks();
                var installedPacks = valueList.Where(x => Directory.Exists(x.Path)).ToList();

                _main.UpdateDisplayedModpacks(installedPacks);

                ClearAllLists();
                await MemoryCleaner.FlushMemoryAsync(trimWorkingSet: false);
            }
            finally
            {
                await Task.Delay(200);
                _isNavigating = false;
            }
        }

        public async void NavigateToServers()
        {
            if (_isNavigating || _main.ServerName.Visibility == Visibility.Visible) return;

            try
            {
                _isNavigating = true;

                await DiscordController.UpdatePresence("Дивиться список серверів");
                _main.Click();

                _main._serverListService.InitializeServersAsync(true, null);

                AnimationService.AnimateBorder(213, 0, _main.PanelSelectNow);

                await _main.HideAllPages();
                AnimationService.AnimatePageTransition(_main.ServerName);

                ClearAllLists();
                await MemoryCleaner.FlushMemoryAsync(trimWorkingSet: false);
            }
            finally
            {
                await Task.Delay(200);
                _isNavigating = false;
            }
        }

        public async void NavigateToGallery()
        {
            if (_isNavigating || _main.GalleryContainer.Visibility == Visibility.Visible) return;

            try
            {
                _isNavigating = true;

                _main.Click();
                AnimationService.AnimateBorder(285, 0, _main.PanelSelectNow);

                await _main.HideAllPages();
                AnimationService.AnimatePageTransition(_main.GalleryContainer);

                ClearAllLists();
                _main.InitializeGallery();
                await MemoryCleaner.FlushMemoryAsync(trimWorkingSet: false);
            }
            finally
            {
                await Task.Delay(200);
                _isNavigating = false;
            }
        }

        private void ClearAllLists()
        {
            if (_main.GirdNews.Visibility != Visibility.Visible)
            {
                _main.ListNews.Items?.Clear();
                _main.TextNews.Text = null;
            }

            _main.ScreenshotsList.Items?.Clear();
            _main.DescriptionServer.Text = null;

            if (_main.ListModsGird.Visibility != Visibility.Visible)
                _main.ModsDowloadList.Items.Clear();

            if (_main.ServerName.Visibility != Visibility.Visible)
                _main.ServerList.Items?.Clear();

        }
    }
}