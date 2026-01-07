using CL_CLegendary_Launcher_.Class;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CL_CLegendary_Launcher_
{
    public partial class DowloadProgress : Window
    {
        public CancellationTokenSource CTS { get; set; }

        public DowloadProgress()
        {
            InitializeComponent();
        }
        public void DowloadProgressBarVersion(int progress,object version)
        {
            VersionTXT.Content = "Завантажується версія " + version;
            ProgressDowloadVersion.Value = progress;
            ProgressDowloadTXT.Content = progress+"%";
        }
        public void DowloadProgressBarFileTask(int filedowload,int filetotaldowload,string namefile)
        {
            FileTXTName.Content = $"{namefile}";
            FileTXT.Content = $"Завантажено {filetotaldowload} з {filedowload}";
        }
        public void DowloadProgressBarFile(int progress)
        {
            ProgressDowloadFile.Value = progress;
            ProgressFileDowloadTXT.Content = progress + "%";
        }
        private void StopDownload_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            CTS?.Cancel();
            this.Close();
        }
        private void ExitLauncher_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationService.FadeIn(BordrExitTXT, 0.5);
        }
        private void ExitLauncher_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationService.FadeOut(BordrExitTXT, 0.5);
        }
        private void ExitLauncher_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        private void BorderTool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

    }
}
