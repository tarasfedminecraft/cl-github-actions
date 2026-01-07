using CL_CLegendary_Launcher_.Class;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class EulaWindow : Window
    {
        private DateTime _versionDate;

        public EulaWindow(EulaConfig config)
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);

            if (config != null)
            {
                MascotMessageText.Text = "Привіт! Я оновила правила нашої Спільноти. Перед тим як ми продовжимо, будь ласка, прочитай і підтвердь їх, щоб ми були на одній хвилі! (´• ω •`)";
                EulaContentText.Text = config.Text;
                _versionDate = config.LastUpdated;
            }
            else
            {
                MascotMessageText.Text = "Ой, я не змогла завантажити актуальні правила з інтернету. Але ось локальна копія.";
                EulaContentText.Text = "Перезавантажіть запускач";
                _versionDate = DateTime.Now;
            }
        }
        private void MascotImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            HideMascot();
        }

        private void HideMascot()
        {
            MascotPanel.Visibility = Visibility.Collapsed;

            MascotColumn.Width = new GridLength(0);
        }
        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            Settings1.Default.EulaAcceptedDate = _versionDate;
            Settings1.Default.Save();

            this.DialogResult = true;
            this.Close();
        }

        private void Decline_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
