using CL_CLegendary_Launcher_.Class;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class DowloadProgress : Window
    {
        public CancellationTokenSource CTS { get; set; }

        public DowloadProgress()
        {
            InitializeComponent();
        }

        public void DowloadProgressBarVersion(int progress, object version)
        {
            VersionTXT.Text = "Завантажується версія " + version;
            ProgressDowloadVersion.Value = progress;
            ProgressDowloadTXT.Text = progress + "%";
        }

        public void DowloadProgressBarFileTask(int filedowload, int filetotaldowload, string namefile)
        {
            FileTXTName.Text = $"{namefile}";
            FileTXT.Text = $"Завантажено {filetotaldowload} з {filedowload} файлів";
        }

        public void DowloadProgressBarFile(int progress)
        {
            ProgressDowloadFile.Value = progress;
            ProgressFileDowloadTXT.Text = progress + "%";
        }

        private void StopDownload_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            CTS?.Cancel();
            this.Close();
        }
        private void ExitLauncher_MouseEnter(object sender, MouseEventArgs e) { }
        private void ExitLauncher_MouseLeave(object sender, MouseEventArgs e) { }

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