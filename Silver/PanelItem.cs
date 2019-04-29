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
        public string ReadableSize
        {
            get
            {
                if (Size == 0) return null;
                long absolute_i = Size < 0 ? -Size : Size;
                string suffix;
                double readable;
                if (absolute_i >= 0x40000000)
                {
                    suffix = "GB";
                    readable = Size >> 20;
                }
                else if (absolute_i >= 0x100000)
                {
                    suffix = "MB";
                    readable = Size >> 10;
                }
                else if (absolute_i >= 0x400)
                {
                    suffix = "KB";
                    readable = Size;
                }
                else
                {
                    return Size.ToString("0 B");
                }
                readable = readable / 1024;
                return readable.ToString("0.## ") + suffix;
            }
        }

        public bool IsDirectory { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Assets { get; set; }

        public bool Openable { get; set; }
    }
}
