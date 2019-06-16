using AurelienRibon.Ui.SyntaxHighlightBox;
using Newtonsoft.Json;
using PakReader;
using Silver.ModelViewer;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static OpenTkControl.OpenTkControlBase;

namespace Silver
{
    /// <summary>
    /// Interaction logic for FileViewer.xaml
    /// </summary>
    public partial class FileViewer : Window
    {
        ObservableCollection<ExportItem> Exports = new ObservableCollection<ExportItem>();
        List<ExportObject> ExtraExports = new List<ExportObject>();

        PakPackage Package;

        Button SelectedBtn;
        ExportItem Selected;

        ModelInterface ModelInterface;

        public FileViewer(string title, PakPackage package, Func<string, PakPackage> packageFunc)
        {
            InitializeComponent();
            Package = package;
            ExportPanel.ItemsSource = Exports;
            Exports.Add(new ExportItem("Encoded Data", ExportItem.JsonImage, ExportType.JSON));
            Title = title;

            foreach (var exp in package.Exports)
            {
                switch (exp)
                {
                    case Texture2D tex:
                        var image = tex.GetImage();
                        Exports.Add(new ExportItem($"{image.Width}x{image.Height}", image, ExportType.IMAGE) { Data = image });
                        break;
                    case USkeletalMesh mesh:
                        Exports.Add(new ExportItem("Skeletal Mesh", ExportItem.ModelImage, ExportType.OPENGL) { Data = mesh });
                        ModelInterface = new ModelInterface(mesh, (int)GLPanel.Width, (int)GLPanel.Height, packageFunc);
                        break;
                    case UObject obj:
                        ExtraExports.Add(obj);
                        break;
                    default:
                        ExtraExports.Add(exp);
                        break;
                }
            }

            Exports[0].Enabled = false;
            if (ExtraExports.Count != 0)
            {
                JsonTxt.CurrentHighlighter = HighlighterManager.Instance.Highlighters["JSON"];
                JsonTxt.Text = JsonConvert.SerializeObject(ExtraExports, Formatting.Indented);
            }
        }
        
        private void ExportAsset(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;
            Console.WriteLine(menu.DataContext.GetType().ToString());
            var ctx = menu.DataContext as ExportItem;

            string ext, name;
            switch (ctx.Type)
            {
                case ExportType.JSON:
                    if (ExtraExports.Count == 0)
                    {
                        Helpers.AskConfirmation(this, "There isn't any JSON data to save", MessageBoxButton.OK);
                        return;
                    }
                    ext = "json";
                    name = "JSON File";
                    break;
                case ExportType.IMAGE:
                    ext = "png";
                    name = "PNG Image";
                    break;
                case ExportType.OPENGL:
                    ext = "psk";
                    name = "PSK File";
                    break;
                default:
                    // lol what
                    return;
            }
            var file = Helpers.ChooseSaveFile(name, ext);
            if (file == null) return;
            switch (ctx.Type)
            {
                case ExportType.JSON:
                    File.WriteAllText(file, JsonTxt.Text);
                    break;
                case ExportType.IMAGE:
                    using (var data = ((SKImage)ctx.Data).Encode())
                    using (var stream = data.AsStream())
                    using (var fStream = File.OpenWrite(file))
                    {
                        stream.CopyTo(fStream);
                    }
                    break;
                case ExportType.OPENGL:
                    using (var fStream = File.OpenWrite(file))
                    using (var writer = new BinaryWriter(fStream))
                    {
                        var cSkel = new CSkeletalMesh((USkeletalMesh)ctx.Data);
                        MeshExporter.ExportMesh(writer, cSkel, cSkel.Lods[0]);
                    }
                    break;
                default:
                    // lol what
                    return;
            }
        }

        private void SaveAsset(object sender, RoutedEventArgs e)
        {
            var file = Helpers.ChooseSaveFile("Asset Files (multiple extensions)", "uasset");
            if (file == null) return;

            if (file.EndsWith("uasset")) file = file.Substring(0, file.Length - 6);
            if (file.EndsWith(".")) file = file.Substring(0, file.Length - 1);

            if (Package.uasset != null) SaveFile(file, Package.AssetReader.GetPackageStream(Package.uasset), "uasset");
            if (Package.uexp != null) SaveFile(file, Package.ExpReader.GetPackageStream(Package.uexp), "uexp");
            if (Package.ubulk != null) SaveFile(file, Package.BulkReader.GetPackageStream(Package.ubulk), "ubulk");

            if (Package.Other != null && Package.Other.Count != 0)
            {
                foreach(var kv in Package.Other)
                {
                    SaveFile(file, kv.Value.Reader.GetPackageStream(kv.Value.Entry), kv.Key);
                }
            }
        }

        static void SaveFile(string path, Stream stream, string ext)
        {
            using (var fStream = File.OpenWrite(path + "." + ext))
            using (stream)
            {
                stream.CopyTo(fStream);
            }
        }

        private void SelectExport(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var ctx = btn.DataContext as ExportItem;
            SelectExport(btn, ctx);
        }

        void SelectExport(Button btn, ExportItem ctx)
        {
            switch (ctx.Type)
            {
                case ExportType.JSON:
                    JsonTxt.Visibility = Visibility.Visible;
                    break;
                case ExportType.IMAGE:
                    ImagePanelBg.Visibility = Visibility.Visible;
                    ImagePanel.Visibility = Visibility.Visible;
                    ImagePanel.Source = ctx.Thumbnail;
                    break;
                case ExportType.OPENGL:
                    GLPanel.Visibility = Visibility.Visible;
                    break;
            }
            if (Selected != null)
            {
                switch (Selected.Type)
                {
                    case ExportType.JSON:
                        JsonTxt.Visibility = Visibility.Collapsed;
                        break;
                    case ExportType.IMAGE:
                        ImagePanelBg.Visibility = Visibility.Collapsed;
                        ImagePanel.Visibility = Visibility.Collapsed;
                        break;
                    case ExportType.OPENGL:
                        GLPanel.Visibility = Visibility.Collapsed;
                        break;
                }
                SelectedBtn.IsEnabled = true;
            }
            btn.IsEnabled = false;
            Selected = ctx;
            SelectedBtn = btn;
        }

        private void LoadedExport(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var ctx = btn.DataContext as ExportItem;
            if (Selected == null)
            {
                btn.IsEnabled = false;
                if (ctx.Type == ExportType.JSON && ExtraExports.Count == 0)
                {
                    return;
                }
                if (ctx.Type != ExportType.JSON)
                {
                    SelectExport(btn, ctx);
                }
                Selected = ctx;
                SelectedBtn = btn;
            }
            else
            {
                btn.IsEnabled = true;
            }
        }

        private void GL_Render(object sender, GlRenderEventArgs e)
        {
            if (Selected.Type == ExportType.OPENGL)
                ModelInterface.OnRender(e);
        }

        private void GL_Error(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject);
        }

        class ExportItem
        {
            internal readonly static BitmapImage JsonImage;
            internal readonly static BitmapImage ModelImage;

            static ExportItem()
            {
                JsonImage = LoadImage("json.png");
                ModelImage = LoadImage("opengl.png");
            }

            static BitmapImage LoadImage(string file)
            {
                var ret = new BitmapImage();
                ret.BeginInit();
                ret.UriSource = new Uri($@"/Silver;component/Resources/{file}", UriKind.Relative);
                ret.EndInit();
                return ret;
            }

            public string Caption { get; set; }
            public ImageSource Thumbnail { get; set; }
            public bool Enabled { get; set; } // only used in startup for JSON because WPF doesn't want to use it after the button is loaded
            public ExportType Type;

            public object Data;

            public ExportItem(string caption, SKImage thumbnail, ExportType type)
            {
                Caption = caption;
                using (var data = thumbnail.Encode())
                using (var stream = data.AsStream())
                {
                    Thumbnail = Helpers.GetImageSource(stream);
                }
                Type = type;
            }

            public ExportItem(string caption, ImageSource source, ExportType type)
            {
                Caption = caption;
                Thumbnail = source;
                Type = type;
            }
        }

        enum ExportType
        {
            JSON,
            IMAGE,
            OPENGL
        }
    }
}
