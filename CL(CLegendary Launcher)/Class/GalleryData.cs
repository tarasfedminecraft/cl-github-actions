using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CL_CLegendary_Launcher_.Class
{
    public class ScreenshotItem
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string CreationDate { get; set; }
        public string FileSize { get; set; } 
        public string Resolution { get; set; } 
        public ImageSource ImagePath { get; set; }
    }

    public class ScreenshotSourceItem
    {
        public string Name { get; set; } 
        public string FullScreenshotsPath { get; set; }
    }
}
