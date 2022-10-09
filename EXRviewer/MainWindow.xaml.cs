using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace EXRviewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {


        [DllImport("EXRf.dll")]
        static private extern void exrf_init(ref UIntPtr exrfc);

        [DllImport("EXRf.dll", CharSet = CharSet.Ansi)]
        static private extern void exrf_open_file(UIntPtr exrfc, string path, int path_len);

        [DllImport("EXRf.dll")]
        static private extern void exrf_get_res_size(UIntPtr exrfc, ref int w, ref int h);

        [DllImport("EXRf.dll")]
        static private extern int exrf_get_channel_first(UIntPtr exrfc, StringBuilder name);

        [DllImport("EXRf.dll")]
        static private extern int exrf_get_channel_next(UIntPtr exrfc, StringBuilder name);

        [DllImport("EXRf.dll")]
        static private extern int exrf_get_layer_first(UIntPtr exrfc, StringBuilder name);

        [DllImport("EXRf.dll")]
        static private extern int exrf_get_layer_next(UIntPtr exrfc, StringBuilder name);

        [DllImport("EXRf.dll")]
        static private extern void exrf_read_pdata(UIntPtr exrfc);

        [DllImport("EXRf.dll", CharSet = CharSet.Ansi)]
        static private extern void exrf_bind_channel_data(UIntPtr exrc, string channel, float[] data_buffer);

        [DllImport("EXRf.dll")]
        static private extern void exrf_clean(UIntPtr exrfc);

        [DllImport("EXRf.dll")]
        static private extern void exrf_testc(UIntPtr exrfc);

        [DllImport("EXRf.dll")]
        static private extern void exrf_testcg(UIntPtr exrfc, ref int r);

        [DllImport("EXRf.dll")]
        static private extern void exrf_testcc(UIntPtr exrfc, IntPtr pbuff);


        public interface ICorrectColorMathBase
        {
            float Calc(float _in);
        }
        public class TestCC : ICorrectColorMathBase
        {
            public float Calc(float _in)
            {
                return Convert.ToSingle(Math.Pow(_in, 1.0 / 2.5));
            }
        }
        public class ImgFData
        {
            public class PData
            {
                public float max = 1.0f;
                public float min = 0.0f;
                public float[] pd = null;
                public PData(int size)
                {
                    pd = new float[size];
                }
            }
            public string filePath = "D:\\001\\Works\\net_files\\1\\image.000.exr";
            //public string filePath = "D:\\001\\Works\\net_files\\2\\0001.exr";
            public int width = 0;
            public int height = 0;
            public List<string> layers = new List<string>();
            //public List<string> channels = new List<string>();
            public Dictionary<string, PData> data = new Dictionary<string, PData>();
        }
        private ImgFData efData = new ImgFData();
        private readonly double scaleValueChangeBase = 0.001;
        private Matrix mainImageMatrix = new Matrix();
        private Matrix mouseMDMatrix = new Matrix();
        private bool mouseMDown = false;
        private Point mousePPos = new Point();
        private readonly XmlDataProvider xmld;
        private string toReadFilePath;
        private readonly List<string> testa = new List<string> { "aa", "bb" };
        private WriteableBitmap MainImageWriteableBitmap;
        public MainWindow()
        {
            var xmldd = new XmlDocument();
            xmldd.LoadXml("<root><control><gamma>1.0</gamma><layerbox><current/><layers/></layerbox><channelmap><r/><g/><b/><a/></channelmap><channelshow>R G B</channelshow></control></root>");
            xmld = new XmlDataProvider
            {
                Document = xmldd
            };
            InitializeComponent();
            DataContext = xmld;
            xmld.XPath = "/root";
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            ReadExr();
            UpdateImageView();
        }

        private void OpenFileMenu_Click(object sender, RoutedEventArgs e)
        {
            var dg = new OpenFileDialog
            {
                Multiselect = false,
                Title = "Choose image"
            };
            var res = dg.ShowDialog();
            if (res != true) { return; }
            toReadFilePath = dg.FileName;
            if (toReadFilePath.ToLower().EndsWith(".exr"))
            {
                ReadExr();
            } else
            {
                ReadAImage();
            }
            UpdateImageView();
        }

        private void UpdateImageView()
        {
            //var nbm = new BitmapImage(new Uri(ImageFilePathTextBox.Text));
            var nbm = new WriteableBitmap(efData.width, efData.height, 96, 96, PixelFormats.Bgr32, null);
            MainImageWriteableBitmap = nbm;
            MainImage.Source = nbm;
            mainImageMatrix = new Matrix();
            MainImageTransform.Matrix = mainImageMatrix;
            nbm.Lock();
            IntPtr pBackBuffer = nbm.BackBuffer;
            byte[] buffer = new byte[efData.width * efData.height * 4];
            List<float[]> cnnb = new List<float[]>();
            string[] cnnbnm = { "b", "g", "r", "a" };
            var layerprefix = xmld.Document.SelectSingleNode("/root/control/layerbox/current").InnerText;
            for (int i = 0; i < 4; i++)
            {
                var _cn = layerprefix + "." + xmld.Document.SelectSingleNode("/root/control/channelmap/" + cnnbnm[i]).InnerText;
                _cn = _cn.Trim('.');
                cnnb.Add(efData.data.ContainsKey(_cn) ? efData.data[_cn].pd : null);
            }
            var ccobj = new TestCC();
            double gamma_bv = Convert.ToDouble(xmld.Document.SelectSingleNode("/root/control/gamma").InnerText);
            float gamma_fp(float _in) => Convert.ToSingle(Math.Pow(_in, 1.0 / gamma_bv));
            byte cmc(float pv) => Convert.ToByte(Math.Max(Math.Min(gamma_fp(pv), 1.0f), 0.0f) * 255.0f);
            for (int hi = 0; hi < efData.height; hi++)
            {
                for (int wi = 0; wi < efData.width; wi++)
                {
                    var hdi = hi * efData.width + wi;
                    for (int i = 0; i < 4; i++)
                    {
                        var bb = cnnb[i];
                        if (bb == null)
                        {
                            buffer[hdi * 4 + i] = 0xFF;
                        } else
                        {
                            buffer[hdi * 4 + i] = cmc(bb[hdi]);
                        }
                    }
                }
            }
            nbm.WritePixels(new Int32Rect(0, 0, efData.width, efData.height), buffer, efData.width * 4, 0);
            nbm.AddDirtyRect(new Int32Rect(0, 0, efData.width, efData.height));
            nbm.Unlock();
        }

        private void ReadAImage()
        {
            efData = new ImgFData();
            efData.filePath = toReadFilePath;
            var bitm = new BitmapImage();
            bitm.BeginInit();
            bitm.UriSource = new Uri(efData.filePath);
            bitm.EndInit();

            efData.width = bitm.PixelWidth;
            efData.height = bitm.PixelHeight;
            byte[] buffer = new byte[efData.width * efData.height * 4];
            bitm.CopyPixels(buffer, efData.width * 4, 0);
            var fdata_r = new ImgFData.PData(efData.width * efData.height);
            var fdata_g = new ImgFData.PData(efData.width * efData.height);
            var fdata_b = new ImgFData.PData(efData.width * efData.height);
            var fdata_a = new ImgFData.PData(efData.width * efData.height);
            efData.data.Clear();
            efData.data.Add("R", fdata_r);
            efData.data.Add("G", fdata_g);
            efData.data.Add("B", fdata_b);
            efData.data.Add("A", fdata_a);
            efData.layers.Clear();
            efData.layers.Add("");
            xmld.Document.DocumentElement.SelectSingleNode("/root/control/channelshow").InnerText = "R G B A";
            float cmc(byte pv) => (float)pv / 255.0f;
            for (int hi = 0; hi < efData.height; hi++)
            {
                for (int wi = 0; wi < efData.width; wi++)
                {
                    var ci = hi * efData.width + wi;
                    //B
                    fdata_b.pd[ci] = cmc(buffer[ci * 4 + 0]);
                    //G
                    fdata_g.pd[ci] = cmc(buffer[ci * 4 + 1]);
                    //R
                    fdata_r.pd[ci] = cmc(buffer[ci * 4 + 2]);
                    //A
                    fdata_a.pd[ci] = cmc(buffer[ci * 4 + 3]);
                }
            }
        }

        private void ReadExr() {
            efData = new ImgFData();
            var exrcp = new UIntPtr();
            // init
            exrf_init(ref exrcp);
            // open file
            efData.filePath = toReadFilePath;
            exrf_open_file(exrcp, efData.filePath, efData.filePath.Length);
            // sample reslustion
            exrf_get_res_size(exrcp, ref efData.width, ref efData.height);
            // sample layer
            StringBuilder sb = new StringBuilder(256);
            int cret = -1;
            var xcnode = xmld.Document.DocumentElement.SelectSingleNode("control/layerbox/layers");
            xcnode.InnerXml = "<layer/>";
            efData.layers.Clear();
            efData.layers.Add("");
            cret = exrf_get_layer_first(exrcp, sb);
            if (cret == 0) {
                do
                {
                    var cn = sb.ToString();
                    efData.layers.Add(cn);
                    sb.Clear();
                    var nxn = xmld.Document.CreateNode("element", "layer", "");
                    nxn.InnerText = cn;
                    xcnode.AppendChild(nxn);
                } while (exrf_get_layer_next(exrcp, sb) == 0);
            }
            // sample channel
            efData.data.Clear();
            cret = exrf_get_channel_first(exrcp, sb);
            if (cret == 0) {
                do
                {
                    var cn = sb.ToString();
                    if (cn.Length < 1)
                    {
                        continue;
                    }
                    //efData.channels.Add(cn);
                    sb.Clear();
                    var nd = new ImgFData.PData(efData.height * efData.width);
                    efData.data.Add(cn, nd);
                    exrf_bind_channel_data(exrcp, cn, nd.pd);
                } while (exrf_get_channel_next(exrcp, sb) == 0);
            }
            exrf_read_pdata(exrcp);
            exrf_clean(exrcp);
            xmld.Document.SelectSingleNode("/root/control/channelshow").InnerXml = "";
            xmld.Document.SelectSingleNode("/root/control/layerbox/current").InnerXml = "";
        }

        private void UpdateImageView_ButtonClick(object sender, RoutedEventArgs e)
        {
            UpdateImageView();
        }

        private void MainImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Console.WriteLine(e.Delta);
            var scv = 1 + e.Delta * scaleValueChangeBase;
            var rpos = e.GetPosition(MainImage) * mainImageMatrix;
            Console.WriteLine(rpos);
            mainImageMatrix.ScaleAt(scv, scv, rpos.X, rpos.Y);
            MainImageTransform.Matrix = mainImageMatrix;
        }

        private void MainImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                mouseMDown = true;
                mousePPos = e.GetPosition(MainImage);
                mouseMDMatrix = new Matrix() * mainImageMatrix;
            }
        }

        private void MainImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                mouseMDown = false;
            }
        }

        private void MainImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseMDown)
            {
                var cnpos = e.GetPosition((MainImage));
                var inposv = (cnpos - mousePPos) * mainImageMatrix;
                var nm = new Matrix();
                nm.Translate(inposv.X, inposv.Y);
                mainImageMatrix *= nm;
                MainImageTransform.Matrix = mainImageMatrix;
            }
        }

        private void SetDefaultChannelControl(string selectedlayer)
        {
            var cchs = new HashSet<string>();
            var scchs = new List<string>();
            foreach (var item in efData.data.Keys)
            {
                if (selectedlayer.Length > 0)
                {
                    if (Regex.IsMatch(item, "^" + Regex.Escape(selectedlayer) + "\\.\\w+"))
                    {
                        cchs.Add(item.Substring(selectedlayer.Length + 1));
                    }
                } else
                {
                    if (!item.Contains('.'))
                    {
                        cchs.Add(item);
                    }
                }
            }
            foreach (var item in "RGBAXYZ")
            {
                string ns = "";
                ns += item;
                if (cchs.Contains(ns))
                {
                    scchs.Add(ns);
                    cchs.Remove(ns);
                }
            }
            scchs.AddRange(cchs);
            xmld.Document.SelectSingleNode("/root/control/channelshow").InnerText = string.Join(" ", scchs.ToArray());
            string[] cs = { "r", "g", "b", "a" };
            for (int i = 0; i < cs.Length; i++)
            {
                var cn = xmld.Document.SelectSingleNode("/root/control/channelmap/" + cs[i]);
                if (i < scchs.Count)
                {
                    cn.InnerText = scchs[i];
                } else
                {
                    cn.InnerText = string.Empty;
                }
            }
            var anode = xmld.Document.SelectSingleNode("/root/control/channelmap/a");
            if (anode.InnerText != "A") { anode.InnerText = ""; }
        }

        private void LayerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetDefaultChannelControl((sender as ComboBox).SelectedValue.ToString());
            if (efData.data.Count > 0) { UpdateImageView(); }
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            var tmp = e.Data.GetData(DataFormats.FileDrop) as System.Array;
            if (tmp.Length < 1) { return; }
            toReadFilePath = tmp.GetValue(0) as string;
            if (toReadFilePath.EndsWith(".exr"))
            {
                ReadExr();
            } else {
                ReadAImage();
            }
            xmld.Document.SelectSingleNode("/root/control/layerbox/current").InnerText = "";
            SetDefaultChannelControl("");
            UpdateImageView();
        }

        private void SaveAsMenu_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "图像格式(*.bmp,*.gif,*.jpg,*.png,*.tif,*.wdp)|*.bmp,*.gif,*.jpg,*.png,*.tif,*.wdp",
                CheckPathExists = true,
                Title = "Save as"
            };
            var res = saveFileDialog.ShowDialog();
            if (res != true) { return; }
            var sf = new FileStream(saveFileDialog.FileName, FileMode.Create);
            BitmapEncoder encoder;
            switch (System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower())
            {
                case ".bmp": encoder = new BmpBitmapEncoder(); break;
                case ".gif": encoder = new GifBitmapEncoder(); break;
                case ".jpg": encoder = new JpegBitmapEncoder(); break;
                case ".png": encoder = new PngBitmapEncoder(); break;
                case ".tif": encoder = new TiffBitmapEncoder(); break;
                case ".wdp": encoder = new WmpBitmapEncoder(); break;
                default: encoder = new PngBitmapEncoder(); break;
            }
            encoder.Frames.Add(BitmapFrame.Create(MainImageWriteableBitmap));
            encoder.Save(sf);
            sf.Close();
        }

        private void InfoViewMenu_Click(object sender, RoutedEventArgs e)
        {
            var win = new ImageInfoWindow();
            var data = new ImageInfoWindow.DataText {
                FileName = efData.filePath,
                Width = efData.width,
                Height = efData.height,
                Channals = efData.data.Keys.ToList(),
                Layers = efData.layers
            };
            win.SetContext(data);
            win.Show();
        }
    }
}
