using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml.Serialization;

namespace EXRviewer
{
    /// <summary>
    /// Interaction logic for ImageInfoWindow.xaml
    /// </summary>
    public partial class ImageInfoWindow : Window
    {
        public class DataText{
            public string FileName;
            public int Width;
            public int Height;
            public List<string> Layers;
            public List<string> Channals;
        }
        public ImageInfoWindow()
        {
            InitializeComponent();
        }

        public void SetContext(DataText data)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"file:{data.FileName}");
            sb.AppendLine($"width:{data.Width}");
            sb.AppendLine($"height:{data.Height}");
            sb.AppendLine("layers:");
            foreach (var item in data.Layers)
            {
                sb.AppendLine($"  {item}");
            }
            sb.AppendLine("channals:");
            foreach (var item in data.Channals)
            {
                sb.AppendLine($"  {item}");
            }
            ShowText.Text = sb.ToString();
        }
    }
}
