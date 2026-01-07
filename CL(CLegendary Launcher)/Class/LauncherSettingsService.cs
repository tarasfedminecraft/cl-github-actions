using CL_CLegendary_Launcher_.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CL_CLegendary_Launcher_.Class
{
    public class LauncherSettingsService
    {
        private readonly CL_Main_ _main; 

        public LauncherSettingsService(CL_Main_ main)
        {
            _main = main;
        }

        public void Initialize()
        {
            InitLauncherPath();
            InitMemorySlider();
        }

        private void InitLauncherPath()
        {
            if (string.IsNullOrWhiteSpace(Settings1.Default.PathLacunher))
            {
                string basePath;

                if (OperatingSystem.IsWindows())
                {
                    basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".ClMinecraft");
                }
                else if (OperatingSystem.IsLinux())
                {
                    basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".clminecraft");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "CLMinecraft");
                }
                else
                {
                    basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".clminecraft");
                }

                Settings1.Default.PathLacunher = basePath;
                Settings1.Default.Save();
            }

            _main.LauncherFloderButton.Content = Settings1.Default.PathLacunher;
        }

        private void InitMemorySlider()
        {
            double totalMemoryInMB = GetTotalMemoryInMB() / 2;
            _main.OPSlider.Maximum = (int)totalMemoryInMB;
            _main.OPSlider.Value = Settings1.Default.OP;
            _main.SliderOPTXT.Content = $"{Settings1.Default.OP:0}MB";
        }

        private double GetTotalMemoryInMB()
        {
            try
            {
                double totalMemoryInBytes = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
                return totalMemoryInBytes / (1024 * 1024);
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Я намагалася дізнатися, скільки у тебе оперативної пам'яті, але не вийшло.\n" +
                                    $"Тому я встановила 2 ГБ як безпечний варіант.\n\nПомилка: {ex.Message}",
                                    "Скільки пам'яті?",
                                    MascotEmotion.Confused); return 2048; 
            }
        }
        public void HandleChangePathClick()
        {
            _main.Click(); 

            using (var openFileDlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                openFileDlg.Description = "Виберіть шлях, за яким буде зберігатись лаунчер і тека .ClMinecraft";

                if (openFileDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = openFileDlg.SelectedPath;
                    string launcherPath = Path.Combine(selectedPath, ".ClMinecraft");

                    try
                    {
                        if (Directory.Exists(Settings1.Default.PathLacunher))
                        {
                            Directory.Delete(Settings1.Default.PathLacunher, true);
                        }

                        Settings1.Default.PathLacunher = launcherPath;
                        Settings1.Default.Save();

                        _main.LauncherFloderButton.Content = launcherPath;
                        Directory.CreateDirectory(launcherPath);
                    }
                    catch (Exception ex)
                    {
                        MascotMessageBox.Show(
                                                    $"Ой! Я не змогла перемістити теку лаунчера.\n" +
                                                    $"Можливо, у мене немає прав доступу до цієї папки?\n\nДеталі: {ex.Message}",
                                                    "Помилка шляху",
                                                    MascotEmotion.Sad);
                    }
                }
            }
        }

        public void HandleResetPathClick()
        {
            _main.Click();

            string defaultPath = "";
            if (OperatingSystem.IsWindows())
            {
                defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".ClMinecraft");
            }

            Settings1.Default.PathLacunher = defaultPath;
            _main.LauncherFloderButton.Content = Settings1.Default.PathLacunher;
            Settings1.Default.Save();
        }

        public void HandleResetOpClick()
        {
            _main.Click();
            _main.OPSlider.Value = 2048;
            _main.SliderOPTXT.Content = "2048MB";

            Settings1.Default.OP = 2048;
            Settings1.Default.Save();
        }

        public void HandleResetResolutionClick()
        {
            _main.Click();
            _main.Width.Text = "800";
            _main.Height.Text = "600";
            _main.MincraftWindowSize.Content = "800x600";

            Settings1.Default.width = 800;
            Settings1.Default.height = 600;
            Settings1.Default.Save();
        }
    }
}
