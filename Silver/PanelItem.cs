using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Silver
{
    class PanelItem
    {
        static PanelItem()
        {
            DirImage = new BitmapImage();
            DirImage.BeginInit();
            DirImage.UriSource = new Uri(@"/Silver;component/Resources/folder.ico", UriKind.Relative);
            DirImage.EndInit();

            FileImage = new BitmapImage();
            FileImage.BeginInit();
            FileImage.UriSource = new Uri(@"/Silver;component/Resources/file.ico", UriKind.Relative);
            FileImage.EndInit();
        }

        readonly static BitmapImage DirImage;
        readonly static BitmapImage FileImage;

        public bool Checked { get; set; }
        public ImageSource Pic => IsDirectory ? DirImage : FileImage;
        public string ReadableSize => Size == 0 ? null : Helpers.GetReadableSize(Size);

        public bool IsDirectory { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Assets { get; set; }
    }
}
