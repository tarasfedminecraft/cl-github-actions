using CL_CLegendary_Launcher_.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CL_CLegendary_Launcher_.Class
{
    public static class MascotMessageBox
    {
        public static void Show(string message, string title = "Інформація", MascotEmotion emotion = MascotEmotion.Normal)
        {
            var dialog = new MascotDialogWindow(message, title, emotion, isQuestion: false);
            dialog.ShowDialog();
        }
        public static bool Ask(string message, string title = "Питання", MascotEmotion emotion = MascotEmotion.Alert)
        {
            var dialog = new MascotDialogWindow(message, title, emotion, isQuestion: true);
            var result = dialog.ShowDialog();
            return result == true; 
        }
    }
}
