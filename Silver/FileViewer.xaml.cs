using AurelienRibon.Ui.SyntaxHighlightBox;
using Newtonsoft.Json;
using PakReader;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Silver
{
    /// <summary>
    /// Interaction logic for FileViewer.xaml
    /// </summary>
    public partial class FileViewer : Window
    {
        ObservableCollection<ExportItem> Exports = new ObservableCollection<ExportItem>();

        Button Selected;

        public FileViewer(string title, ExportObject[] exps)
        {
            InitializeComponent();
            ExportPanel.ItemsSource = Exports;
            Exports.Add(new ExportItem("Encoded Data", ExportItem.JsonImage, ExportType.JSON));
            Title = title;

            List<UObject> ExtraExports = new List<UObject>();
            foreach (var exp in exps)
            {
                switch (exp)
                {
                    case Texture2D tex:
                        var image = tex.GetImage();
                        Exports.Add(new ExportItem($"{image.Width}x{image.Height}", image, ExportType.IMAGE));
                        break;
                    case UObject obj:
                        ExtraExports.Add(obj);
                        break;
                    default:
                        break;
                }
            }

            JsonTxt.CurrentHighlighter = HighlighterManager.Instance.Highlighters["JSON"];
            JsonTxt.Text = JsonConvert.SerializeObject(ExtraExports, Formatting.Indented);
        }

        private void SelectExport(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            Selected.IsEnabled = true;
            btn.IsEnabled = false;
            btn.DataContext = SelectExport(btn.DataContext as ExportItem);
            Selected = btn;
        }

        ExportItem SelectExport(ExportItem obj)
        {
            var selectedObj = Selected.DataContext as ExportItem;
            switch (selectedObj.Type)
            {
                case ExportType.JSON:
                    JsonTxt.Visibility = Visibility.Collapsed;
                    break;
                case ExportType.IMAGE:
                    ImagePanelBg.Visibility = Visibility.Collapsed;
                    ImagePanel.Visibility = Visibility.Collapsed;
                    break;
                case ExportType.OPENGL:
                    break;
            }
            switch (obj.Type)
            {
                case ExportType.JSON:
                    JsonTxt.Visibility = Visibility.Visible;
                    break;
                case ExportType.IMAGE:
                    ImagePanelBg.Visibility = Visibility.Visible;
                    ImagePanel.Visibility = Visibility.Visible;
                    ImagePanel.Source = obj.Thumbnail;
                    break;
                case ExportType.OPENGL:
                    break;
            }
            Selected.DataContext = selectedObj;
            return obj;
        }

        private void LoadedExport(object sender, RoutedEventArgs e)
        {
            if (Selected == null)
            {
                Selected = sender as Button;
                Selected.IsEnabled = false;
            }
        }

        class ExportItem
        {
            internal readonly static BitmapImage JsonImage;

            static ExportItem()
            {
                JsonImage = new BitmapImage();
                JsonImage.BeginInit();
                JsonImage.UriSource = new Uri(@"/Silver;component/Resources/json.png", UriKind.Relative);
                JsonImage.EndInit();
            }

            public string Caption { get; set; }
            public ImageSource Thumbnail { get; set; }
            public ExportType Type;

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
            OPENGL // Unused
        }
    }
}
