using CL_CLegendary_Launcher_.Class;
using CL_CLegendary_Launcher_.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Appearance;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class ItemManegerPack : UserControl
    {
        public ModpackInfo CurrentModpack { get; set; }
        public string pathmods;
        public bool Off_OnMod = true;
        public bool IsModPack = false;
        public int Index { get; set; }

        public ItemManegerPack()
        {
            InitializeComponent();
        }
        public void Off_OnMods_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(pathmods) || !File.Exists(pathmods))
            {
                string altPath = Off_OnMod ? pathmods + ".disabled" : pathmods.Replace(".disabled", "");
                if (File.Exists(altPath))
                {
                    pathmods = altPath; 
                }
                else
                {
                    MessageBox.Show($"Файл мода не знайдено:\n{pathmods}", "Помилка");
                    return;
                }
            }

            try
            {
                string directory = System.IO.Path.GetDirectoryName(pathmods);
                string fileName = System.IO.Path.GetFileName(pathmods);
                string newPath;

                if (fileName.EndsWith(".disabled"))
                {
                    newPath = System.IO.Path.Combine(directory, fileName.Replace(".disabled", ""));
                    Off_OnMod = true;
                    Description.Text = "Активний";
                }
                else
                {
                    newPath = pathmods + ".disabled";
                    Off_OnMod = false;
                    Description.Text = "Вимкнено";
                }

                File.Move(pathmods, newPath);
                pathmods = newPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося змінити статус мода.\n{ex.Message}", "Помилка");
                IsOnOffSwitch.IsChecked = !IsOnOffSwitch.IsChecked;
            }
        }
        private void DowloadTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(pathmods) || !File.Exists(pathmods))
            {
                if (File.Exists(pathmods + ".disabled")) pathmods += ".disabled";
                else if (File.Exists(pathmods.Replace(".disabled", ""))) pathmods = pathmods.Replace(".disabled", "");
                else
                {
                    MessageBox.Show("Файл не знайдено для видалення.", "Помилка");
                    return;
                }
            }

            try
            {
                string fileNameToDelete = System.IO.Path.GetFileName(pathmods).Replace(".disabled", "");

                File.Delete(pathmods); 

                if (IsModPack && CurrentModpack != null && !string.IsNullOrEmpty(CurrentModpack.PathJson))
                {
                    string jsonPath = CurrentModpack.PathJson;

                    if (File.Exists(jsonPath))
                    {
                        try
                        {
                            string jsonContent = File.ReadAllText(jsonPath);
                            bool updated = false;
                            string newJsonContent = "";

                            try
                            {
                                var manifest = JsonConvert.DeserializeObject<CustomModpackManifest>(jsonContent);
                                if (manifest != null && manifest.Files != null)
                                {
                                    var itemToRemove = manifest.Files.FirstOrDefault(m =>
                                        (m.FileName != null && m.FileName.Equals(fileNameToDelete, StringComparison.OrdinalIgnoreCase)) ||
                                        (m.Name != null && m.Name.Equals(Title.Text, StringComparison.OrdinalIgnoreCase))
                                    );

                                    if (itemToRemove != null)
                                    {
                                        manifest.Files.Remove(itemToRemove);
                                        newJsonContent = JsonConvert.SerializeObject(manifest, Formatting.Indented);
                                        updated = true;
                                    }
                                }
                            }
                            catch
                            {
                            }

                            if (!updated)
                            {
                                try
                                {
                                    var modsList = JsonConvert.DeserializeObject<List<ModInfo>>(jsonContent);
                                    if (modsList != null)
                                    {
                                        var itemToRemove = modsList.FirstOrDefault(m =>
                                            (m.FileName != null && m.FileName.Equals(fileNameToDelete, StringComparison.OrdinalIgnoreCase)) ||
                                            (m.Name != null && m.Name.Equals(Title.Text, StringComparison.OrdinalIgnoreCase))
                                        );

                                        if (itemToRemove != null)
                                        {
                                            modsList.Remove(itemToRemove);
                                            newJsonContent = JsonConvert.SerializeObject(modsList, Formatting.Indented);
                                            updated = true;
                                        }
                                    }
                                }
                                catch { }
                            }

                            if (updated && !string.IsNullOrEmpty(newJsonContent))
                            {
                                File.WriteAllText(jsonPath, newJsonContent);
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Помилка оновлення JSON: {jsonEx.Message}");
                        }
                    }
                }

                var parentList = FindParent<ItemsControl>(this);
                if (parentList != null && parentList.ItemsSource == null) 
                {
                    parentList.Items.Remove(this);
                }
                else if (parentList != null && parentList.ItemsSource != null)
                {
                    try { parentList.Items.Remove(this); } catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка видалення: {ex.Message}", "Помилка");
            }
        }
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }
    }
}