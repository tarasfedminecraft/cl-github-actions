using CL_CLegendary_Launcher_.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CL_CLegendary_Launcher_.Class
{
    internal class DragDropService
    {
        private readonly CL_Main_ _main; 

        public DragDropService(CL_Main_ main)
        {
            _main = main;
        }

        public void Initialize()
        {
            _main.AllowDrop = true;
            _main.DragEnter += Window_DragEnter;
            _main.DragLeave += Window_DragLeave;
            _main.Drop += Window_Drop;
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                _main.MainGirdDropFile.Visibility = Visibility.Visible;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            _main.MainGirdDropFile.Visibility = Visibility.Hidden;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            _main.MainGirdDropFile.Visibility = Visibility.Hidden; 

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var filePath in filePaths)
                {
                    string fileExtension = Path.GetExtension(filePath).ToLower();

                    if (fileExtension == ".jar")
                    {
                        MoveFileToLauncherFolder(filePath, "mods");
                    }
                    else if (fileExtension == ".litemod")
                    {
                        MoveFileToLauncherFolder(filePath, "mods");
                    }
                    else if (fileExtension == ".zip")
                    {
                        string fileType = DetermineZipType(filePath);

                        if (fileType == "resourcepack")
                        {
                            MoveFileToLauncherFolder(filePath, "resourcepacks");
                        }
                        else if (fileType == "shader")
                        {
                            MoveFileToLauncherFolder(filePath, "shaderpacks");
                        }
                        else
                        {
                            MascotMessageBox.Show(
                                                            "Хм, я заглянула в цей архів, але не зрозуміла, що це таке.\n" +
                                                            "Це не схоже ні на ресурспак, ні на шейдери.",
                                                            "Що це?",
                                                            MascotEmotion.Confused);
                        }
                    }
                    else
                    {
                        MascotMessageBox.Show(
                                                    $"Ой, я не знаю, що робити з файлом формату \"{fileExtension}\".\n" +
                                                    "Я вмію працювати тільки з .jar та .zip.",
                                                    "Невідомий файл",
                                                    MascotEmotion.Sad);
                    }
                }
            }
        }

        private string DetermineZipType(string zipPath)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    bool isResourcePack = archive.Entries.Any(entry => entry.FullName.Equals("pack.mcmeta", StringComparison.OrdinalIgnoreCase));
                    bool isShader = archive.Entries.Any(entry => entry.FullName.StartsWith("shaders/", StringComparison.OrdinalIgnoreCase));

                    if (isResourcePack)
                        return "resourcepack";
                    if (isShader)
                        return "shader";
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Не змогла відкрити цей архів. Можливо, він пошкоджений?\n\nПомилка: {ex.Message}",
                                    "Збій архіву",
                                    MascotEmotion.Sad);
            }
            return "unknown";
        }

        private void MoveFileToLauncherFolder(string filePath, string folderName)
        {
            try
            {
                string launcherPath = Settings1.Default.PathLacunher; 
                string destinationFolder = Path.Combine(launcherPath, folderName);

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                string fileName = Path.GetFileName(filePath);
                string destinationPath = Path.Combine(destinationFolder, fileName);

                File.Move(filePath, destinationPath);

                MascotMessageBox.Show(
                                    $"Я дбайливо перенесла \"{fileName}\" у папку {folderName}!\nМожеш перевірити.",
                                    "Готово!",
                                    MascotEmotion.Happy);
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ой леле! Я намагалася перемістити файл, але щось пішло не так.\n" +
                                    $"Перевір, чи не відкритий він в іншій програмі.\n\nДеталі: {ex.Message}",
                                    "Помилка файлу",
                                    MascotEmotion.Sad);
            }
        }

    }
}
