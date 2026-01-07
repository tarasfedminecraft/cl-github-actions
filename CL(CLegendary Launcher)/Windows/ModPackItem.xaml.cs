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
    public partial class ModPackItem : UserControl
    {
        public string game_version { get; set; }
        public string loaders { get; set; }
        public string ProjectId { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public string downloda_url { get; set; }
        public object haseh { get; set; }
        public string icon_url { get; set; }
        public int Type { get; set; }
        public ModPackItem()
        {
            InitializeComponent();
        }
    }
}
