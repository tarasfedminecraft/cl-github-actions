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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CL_CLegendary_Launcher_.Windows
{
    public partial class ItemManegerProfile : UserControl
    {
        public enum AccountType
        {
            Microsoft,
            LittleSkin,
            Offline
        }
        public string NameAccount2 { get; set; }
        public string UUID { get; set; }
        public string AccessToken { get; set; }
        public string ImageUrl { get; set; }
        public int index { get; set; }
        public AccountType TypeAccount { get; set; }

        public ItemManegerProfile()
        {
            InitializeComponent();
        }
    }
}
