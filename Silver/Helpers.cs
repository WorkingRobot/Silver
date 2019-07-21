using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DialogResult = System.Windows.Forms.DialogResult;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace Silver
{
    static class Helpers
    {
        public static (string Path, string Extension) GetPath(string inp)
        {
            int extInd = inp.LastIndexOf('.');
            return (inp.Substring(0, extInd), inp.Substring(extInd + 1));
        }

        public static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            return hex - (hex < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        static readonly uint[] _Lookup32 = Enumerable.Range(0, 256).Select(i => {
            string s = i.ToString("x2");
            return s[0] + ((uint)s[1] << 16);
        }).ToArray();
        public static string ToHex(byte[] bytes, int length = -1)
        {
            if (bytes == null) return null;
            length = (length == -1 || length > bytes.Length) ? bytes.Length : length;
            var result = new char[length * 2];
            for (int i = 0; i < length; i++)
            {
                var val = _Lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        public static string GetReadableSize(long size)
        {
            long absolute_i = size < 0 ? -size : size;
            string suffix;
            double readable;
            if (absolute_i >= 0x40000000)
            {
                suffix = "GB";
                readable = size >> 20;
            }
            else if (absolute_i >= 0x100000)
            {
                suffix = "MB";
                readable = size >> 10;
            }
            else if (absolute_i >= 0x400)
            {
                suffix = "KB";
                readable = size;
            }
            else
            {
                return size.ToString("0 B");
            }
            readable = readable / 1024;
            return readable.ToString("0.## ") + suffix;
        }

        const char DirSeparator = '/';
        public static string[] GetKeys(string path)
        {
            if (path.Length == 0 || (path[0] == DirSeparator && path.Length == 1))
            {
                return new string[0];
            }
            if (path[0] == DirSeparator)
            {
                path = path.Substring(1);
            }
            if (path[path.Length - 1] == DirSeparator)
            {
                path = path.Substring(0, path.Length - 1);
            }
            return path.Replace(DirSeparator + DirSeparator.ToString(), DirSeparator.ToString()).Split(DirSeparator);
        }

        public static string ReadFString(this BinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            return Encoding.UTF8.GetString(reader.ReadBytes(length));
        }

        public static void WriteFString(this BinaryWriter writer, string value)
        {
            byte[] toWrite = Encoding.UTF8.GetBytes(value ?? "");
            writer.Write((ushort)toWrite.Length);
            writer.Write(toWrite);
        }

        public static MessageBoxResult AskConfirmation(this Window me, string caption, MessageBoxButton buttons = MessageBoxButton.OKCancel)
        {
            return MessageBox.Show(me, caption, "Silver", buttons);
        }

        public static MessageBoxResult SaveFileCheck(this MainWindow me)
        {
            return me.Project == null
                ? MessageBoxResult.No
                : AskConfirmation(me, $"Do you want to save changes to {me.Project?.Name ?? "Untitled Project"}?", MessageBoxButton.YesNoCancel);
        }

        public static string ChooseSaveFile(string name, string extension)
        {
            var dialog = new SaveFileDialog
            {
                Filter = $"{name} (*.{extension})|*.{extension}",
                DefaultExt = extension,
                AddExtension = true
            };
            return dialog.ShowDialog() ?? false ? dialog.FileName : null;
        }

        public static string ChooseOpenFile(string name, string extension)
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{name} (*.{extension})|*.{extension}",
                DefaultExt = extension,
                AddExtension = true
            };
            return dialog.ShowDialog() ?? false ? dialog.FileName : null;
        }

        public static string[] ChooseOpenFiles(string name, string extension)
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{name} (*.{extension})|*.{extension}",
                DefaultExt = extension,
                AddExtension = true,
                Multiselect = true
            };
            return dialog.ShowDialog() ?? false ? dialog.FileNames : null;
        }

        public static string ChooseFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }
            return null;
        }

        public static ImageSource GetImageSource(Stream stream)
        {
            BitmapImage photo = new BitmapImage();
            using (stream)
            {
                photo.BeginInit();
                photo.CacheOption = BitmapCacheOption.OnLoad;
                photo.StreamSource = stream;
                photo.EndInit();
            }
            return photo;
        }
    }
}
