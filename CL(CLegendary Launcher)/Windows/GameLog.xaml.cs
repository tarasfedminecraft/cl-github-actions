using CL_CLegendary_Launcher_.Class;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class GameLog : FluentWindow
    {
        public GameLog()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
        }
        public void MinecraftProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendTextToLog(e.Data);
            }
        }
        public void MinecraftProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendTextToLog(e.Data);
            }
        }
        public void AppendTextToLog(string text)
        {
            if (GameLogTXTMincraft.Dispatcher.CheckAccess())
            {
                GameLogTXTMincraft.AppendText(text + Environment.NewLine);
            }
            else
            {
                GameLogTXTMincraft.Dispatcher.BeginInvoke(new Action(() => AppendTextToLog(text)));
            }
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