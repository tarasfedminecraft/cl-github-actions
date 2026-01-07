using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace CL_CLegendary_Launcher_.Class
{
    public static class NotificationService
    {
        public static void ShowNotification(string title, string message, SnackbarPresenter snackbarPresenter,int sec = 5,IconElement icon = null,ControlAppearance appearance = ControlAppearance.Primary)
        {
            var snackbar = new Snackbar(snackbarPresenter)
            {
                Title = title,
                Content = message,
                Appearance = appearance, 
                Timeout = TimeSpan.FromSeconds(sec) ,
                Icon = icon
            };

            snackbar.Show();
        }
    }
}
