using Newtonsoft.Json;
using PakReader;
using Silver.ModelViewer;
using Silver.SyntaxLexers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
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
            Title = title;

            IniTxt.CurrentHighlighter = new IniParser();
            JsonTxt.CurrentHighlighter = new JsonParser();

            if (package.Exportable)
            {
                try
                {
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
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            foreach(var kv in package.Extensions)
            {
                switch (kv.Key.ToLowerInvariant())
                {
                    case "ini":
                        using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry))
                        using (var r = new StreamReader(s))
                            IniTxt.Text = r.ReadToEnd();
                        Exports.Add(new ExportItem("Ini File", ExportItem.IniImage, ExportType.INI));
                        break;
                    case "uproject":
                        using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry))
                        using (var r = new StreamReader(s))
                            Exports.Add(new ExportItem("Project File", ExportItem.JsonImage, ExportType.JSON) { Data = r.ReadToEnd() });
                        break;
                    case "uplugin":
                        using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry))
                        using (var r = new StreamReader(s))
                            Exports.Add(new ExportItem("Plugin File", ExportItem.JsonImage, ExportType.JSON) { Data = r.ReadToEnd() });
                        break;
                    case "upluginmanifest":
                        using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry))
                        using (var r = new StreamReader(s))
                            Exports.Add(new ExportItem("Plugin Manifest", ExportItem.JsonImage, ExportType.JSON) { Data = r.ReadToEnd() });
                        break;
                    case "png":
                        using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry))
                        {
                            var img = SKImage.FromEncodedData(s);
                            Exports.Add(new ExportItem($"PNG ({img.Width}x{img.Height})", img, ExportType.IMAGE) { Data = img });
                        }
                        break;
                    case "locres":
                        using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry))
                            Exports.Add(new ExportItem($"LocRes File", ExportItem.JsonImage, ExportType.JSON) { Data = JsonConvert.SerializeObject(new LocResFile(s).Entries, Formatting.Indented) });
                        break;
                    case "locmeta":
                        using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry))
                            Exports.Add(new ExportItem($"LocMeta File", ExportItem.JsonImage, ExportType.JSON) { Data = JsonConvert.SerializeObject(new LocMetaFile(s), Formatting.Indented) });
                        break;
                    case "udic":
                        using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry))
                        using (var reader = new BinaryReader(s))
                        {
                            var udic = new UDicFile(reader);
                            Exports.Add(new ExportItem($"Oodle Header", ExportItem.JsonImage, ExportType.JSON) { Data = JsonConvert.SerializeObject(udic.Header, Formatting.Indented) });
                            Exports.Add(new ExportItem($"Dictionary Data", ExportItem.RawImage, ExportType.RAW) { Data = udic.DictionaryData });
                            Exports.Add(new ExportItem($"Compressor State", ExportItem.RawImage, ExportType.RAW) { Data = udic.CompressorState });
                        }
                        break;
                    case "bin":
                        if (!title.Contains("AssetRegistry"))
                            break;
                        using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry))
                        using (var reader = new BinaryReader(s))
                        {
                            Exports.Add(new ExportItem($"AssetRegistry", ExportItem.JsonImage, ExportType.JSON) { Data = JsonConvert.SerializeObject(new AssetRegistryFile(s), Formatting.Indented) });
                        }
                        break;
                }

                using (var s = kv.Value.Reader.GetPackageStream(kv.Value.Entry) as MemoryStream)
                    Exports.Add(new ExportItem($"Raw {kv.Key}", ExportItem.RawImage, ExportType.RAW) { Data = s.ToArray() });
            }

            if (ExtraExports.Count != 0)
            {
                Exports.Insert(0, new ExportItem("Encoded Data", ExportItem.JsonImage, ExportType.JSON) { Data = JsonConvert.SerializeObject(ExtraExports, Formatting.Indented) });
            }
        }
        
        private void ExportAsset(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;
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
                case ExportType.INI:
                    ext = "ini";
                    name = "Ini File";
                    break;
                case ExportType.RAW:
                    Helpers.AskConfirmation(this, "You can't export raw files. Use the \"Save As Raw\" button.", MessageBoxButton.OK);
                    return;
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
                case ExportType.INI:
                    File.WriteAllText(file, IniTxt.Text);
                    break;
                default:
                    // lol what
                    return;
            }
        }

        private void SaveAsset(object sender, RoutedEventArgs e) => SaveAsset(Package);

        internal static void SaveAsset(PakPackage package)
        {
            var file = Helpers.ChooseSaveFile("Asset Files (multiple extensions)", "uasset");
            if (file == null) return;

            if (file.EndsWith("uasset")) file = file.Substring(0, file.Length - 6);
            if (file.EndsWith(".")) file = file.Substring(0, file.Length - 1);

            foreach (var kv in package.Extensions)
                SaveFile(file, kv.Value.Reader.GetPackageStream(kv.Value.Entry), kv.Key);
        }

        static void SaveFile(string path, Stream stream, string ext)
        {
            using (var fStream = File.OpenWrite(path + "." + ext))
            using (stream)
                stream.CopyTo(fStream);
        }

        private void SelectExport(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            SelectExport(btn, btn.DataContext as ExportItem);
        }

        void SelectExport(Button btn, ExportItem ctx)
        {
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
                    case ExportType.INI:
                        IniTxt.Visibility = Visibility.Collapsed;
                        break;
                    case ExportType.RAW:
                        RawTxt.Visibility = Visibility.Collapsed;
                        break;
                }
                SelectedBtn.IsEnabled = true;
            }
            switch (ctx.Type)
            {
                case ExportType.JSON:
                    JsonTxt.Visibility = Visibility.Visible;
                    JsonTxt.Text = FormatJson((string)ctx.Data);
                    break;
                case ExportType.IMAGE:
                    ImagePanelBg.Visibility = Visibility.Visible;
                    ImagePanel.Visibility = Visibility.Visible;
                    ImagePanel.Source = ctx.Thumbnail;
                    break;
                case ExportType.OPENGL:
                    GLPanel.Visibility = Visibility.Visible;
                    break;
                case ExportType.INI:
                    IniTxt.Visibility = Visibility.Visible;
                    break;
                case ExportType.RAW:
                    RawTxt.Visibility = Visibility.Visible;
                    RawTxt.Text = FormatRaw((byte[])ctx.Data);
                    break;
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
                SelectExport(btn, ctx);
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

        const int MAX_VIEWER_SIZE = 64;
        static string FormatRaw(byte[] rawData, int maxLength = 1024 * MAX_VIEWER_SIZE) // max size: 64 kb
        {
            StringBuilder builder = new StringBuilder(Helpers.ToHex(rawData, maxLength));
            int j = 0;
            int iters = builder.Length / 4;
            for (int i = 1; i <= iters; i++)
            {
                builder.Insert(i * 5 - 1, (j = (j + 1) % 8) == 0 ? '\n' : ' ');
            }
            if (rawData.Length > maxLength)
            {
                builder.Append($"\nTruncated at {MAX_VIEWER_SIZE} KB. Actual size is {Helpers.GetReadableSize(rawData.Length)}");
            }
            return builder.ToString();
        }

        static string FormatJson(string json, int maxLength = 1024 * MAX_VIEWER_SIZE) => // max size: 64 kb
            json.Length > maxLength ?
                json.Substring(0, maxLength) + $"\nTruncated at {MAX_VIEWER_SIZE} KB. Actual size is {Helpers.GetReadableSize(json.Length)}" :
                json;

        class ExportItem
        {
            internal readonly static BitmapImage JsonImage;
            internal readonly static BitmapImage ModelImage;
            internal readonly static BitmapImage IniImage;
            internal readonly static BitmapImage RawImage;

            static ExportItem()
            {
                JsonImage = LoadImage("json.png");
                ModelImage = LoadImage("opengl.png");
                IniImage = LoadImage("ini.png");
                RawImage = LoadImage("file.ico");
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
            INI,
            IMAGE,
            OPENGL,
            RAW
        }
    }
}
