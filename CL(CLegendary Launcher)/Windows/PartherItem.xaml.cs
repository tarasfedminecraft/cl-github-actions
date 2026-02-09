using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class PartherItem : UserControl
    {
        private Image ImageBg;
        private Image Image;
        private string _title;
        public string _description;
        private string port;

        public PartherItem()
        {
            InitializeComponent();
        }
        [Category("Custom Props")]
        public string _Title
        {
            get { return _title; }
            set { _title = value; TitleMain1.Text = value; }
        }
        [Category("Custom Props")]
        public Image ImageMain_
        {
            get { return Image; }
            set
            {
                Image = value;
                BitmapImage newImage = new BitmapImage();
                newImage.BeginInit();
                newImage.UriSource = new Uri(value.ToString()); 
                newImage.EndInit();
                MainIcon3.Source = newImage;
            }
        }

        [Category("Custom Props")]
        public Image ImageMainBg_
        {
            get { return ImageBg; } 
            set
            {
                ImageBg = value;
                BitmapImage newImage = new BitmapImage();
                newImage.BeginInit();
                newImage.UriSource = new Uri(value.ToString()); 
                newImage.EndInit();
                //BgImageMincraftServer.Source = newImage;
            }
        }

        private void IPServerTXT_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Clipboard.SetText(IPServerTXT.Text.ToString());
        }
    }
}
