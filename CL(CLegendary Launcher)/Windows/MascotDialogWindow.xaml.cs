using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace CL_CLegendary_Launcher_.Windows
{
    public enum MascotEmotion
    {
        Normal,
        Happy,
        Sad,
        Confused,
        Alert,
        Dead
    }

    public partial class MascotDialogWindow : Window
    {
        public MascotDialogWindow(string message, string title, MascotEmotion emotion, bool isQuestion)
        {
            InitializeComponent();

            TitleTxt.Text = title;
            MessageTxt.Text = message;

            if (isQuestion)
            {
                BtnOK.Content = "Так";
                BtnCancel.Visibility = Visibility.Visible;
                BtnCancel.Content = "Ні, чекай";
            }
            else
            {
                BtnOK.Content = "Зрозуміло";
                BtnCancel.Visibility = Visibility.Collapsed;
            }

            SetMascotImage(emotion);

            System.Media.SystemSounds.Asterisk.Play();
        }

        private void SetMascotImage(MascotEmotion emotion)
        {
            //string imageName = "СMD.png";

            //switch (emotion)
            //{
            //    case MascotEmotion.Happy: imageName = "Mascot_Happy.png"; break;
            //    case MascotEmotion.Sad: imageName = "Mascot_Sad.png"; break;
            //    case MascotEmotion.Confused: imageName = "Mascot_Confused.png"; break;
            //    case MascotEmotion.Alert: imageName = "Mascot_Alert.png"; break;
            //    case MascotEmotion.Dead: imageName = "Mascot_Dead.png"; break;
            //    default: imageName = "СMD.png"; break;
            //}

            try
            {
                //var uri = new Uri($"pack://application:,,,/Assets/{imageName}");
                //var bitmap = new BitmapImage(uri);

                //if (bitmap.Width > 0)
                //{
                //    MascotImage.Source = bitmap;
                //    ShowMascotColumn();
                //}
            }
            catch
            {
                HideMascotColumn();
            }
        }

        private void HideMascotColumn()
        {
            MascotImage.Visibility = Visibility.Collapsed;
            MascotColumn.Width = new GridLength(0); 
        }

        private void ShowMascotColumn()
        {
            MascotImage.Visibility = Visibility.Visible;
            MascotColumn.Width = new GridLength(140); 
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void MainBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
