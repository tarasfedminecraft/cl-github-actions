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
        public ImageSource ModIconValue { get; set; }
        public string pathmods; 
        public bool Off_OnMod = true; 
        public bool IsModPack = false;
        public int Index { get; set; } 

        public ItemManegerPack()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
            IsOnOffSwitch.IsChecked = Off_OnMod;

        }
        private void Off_OnMods_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(pathmods) || !File.Exists(pathmods))
            {
                MessageBox.Show("Файл мода не знайдено. Перевірте шлях.", "Помилка");
                return;
            }

            string newFilePath;
            try
            {
                if (Off_OnMod == true)
                {
                    Off_OnMod = false;
                    IsOnOffSwitch.IsChecked = Off_OnMod;
                    newFilePath = pathmods + ".disabled";
                }
                else
                {
                    Off_OnMod = true;
                    IsOnOffSwitch.IsChecked = Off_OnMod;
                    newFilePath = pathmods.Replace(".disabled", "");
                }

                File.Move(pathmods, newFilePath);
                pathmods = newFilePath; 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося змінити статус мода.\nПомилка: {ex.Message}", "Помилка");
            }
        }

        private void DowloadTXT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(pathmods) || !File.Exists(pathmods))
            {
                MessageBox.Show("Файл мода не знайдено. Перевірте шлях.", "Помилка");
                return;
            }

            try
            {
                File.Delete(pathmods);
                if (IsModPack && CurrentModpack != null)
                {
                    if (CurrentModpack.TypeSite == "CurseForge")
                    {
                        ModJsonManager.RemoveModFromCurseForgeManifest(CurrentModpack.PathJson, Index);
                    }
                    else if (CurrentModpack.TypeSite == "Custom")
                    {
                        string modFileName = System.IO.Path.GetFileName(pathmods);
                        ModJsonManager.RemoveModFromJson(pathmods, CurrentModpack);
                    }
                }
                var parentItemsControl = this.Parent as ItemsControl;
                if (parentItemsControl != null)
                {
                    parentItemsControl.Items.Remove(this);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося видалити мод.\nПомилка: {ex.Message}", "Помилка");
            }
        }
    }
}
