using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CL_CLegendary_Launcher_.Class
{
    public class ScreenshotService
    {
        public List<ScreenshotSourceItem> GetScreenshotSources(string launcherPath)
        {
            var sources = new List<ScreenshotSourceItem>();

            string globalPath = Path.Combine(launcherPath, "screenshots");
            sources.Add(new ScreenshotSourceItem
            {
                Name = "Глобальні (.ClMinecraft)",
                FullScreenshotsPath = globalPath
            });

            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "installed_modpacks.json");

            if (File.Exists(jsonPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var modpacks = JsonConvert.DeserializeObject<List<ModpackInfo>>(jsonContent);

                    if (modpacks != null)
                    {
                        foreach (var pack in modpacks)
                        {
                            if (!string.IsNullOrWhiteSpace(pack.Path))
                            {
                                string packScreenPath = Path.Combine(pack.Path, "override", "screenshots");
                                if (!Directory.Exists(packScreenPath))
                                    packScreenPath = Path.Combine(pack.Path, "overrides", "screenshots");

                                sources.Add(new ScreenshotSourceItem
                                {
                                    Name = $"{pack.Name} (Збірка)",
                                    FullScreenshotsPath = packScreenPath
                                });
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            return sources;
        }

        public async Task<List<ScreenshotItem>> LoadScreenshotsAsync(string folderPath)
        {
            var list = new List<ScreenshotItem>();

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return list; 

            return await Task.Run(() =>
            {
                try
                {
                    var files = Directory.GetFiles(folderPath, "*.png");
                    Array.Sort(files);
                    Array.Reverse(files);

                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        long length = fileInfo.Length;

                        string sizeString = length >= 1024 * 1024
                            ? $"{length / (1024.0 * 1024.0):F2} MB"
                            : $"{length / 1024.0:F2} KB";

                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(file);
                        bitmap.DecodePixelWidth = 300; 
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        string resolution = "Unknown";
                        try
                        {
                            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                            {
                                var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                                resolution = $"{frame.PixelWidth}x{frame.PixelHeight}";
                            }
                        }
                        catch { }

                        list.Add(new ScreenshotItem
                        {
                            FilePath = file,
                            FileName = fileInfo.Name,
                            CreationDate = fileInfo.CreationTime.ToString("dd.MM.yyyy HH:mm"),
                            FileSize = sizeString,
                            Resolution = resolution,
                            ImagePath = bitmap
                        });
                    }
                }
                catch { }

                return list;
            });
        }

        public bool DeleteScreenshot(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
