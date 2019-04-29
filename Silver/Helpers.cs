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
        public static string ToHex(byte[] bytes)
        {
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = _Lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
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
            if (me.Project == null) return MessageBoxResult.No;
            return AskConfirmation(me, $"Do you want to save changes to {me.Project?.Name ?? "Untitled Project"}?", MessageBoxButton.YesNoCancel);
        }

        public static string ChooseSaveFile(string name, string extension)
        {
            var dialog = new SaveFileDialog
            {
                Filter = $"{name} (*.{extension})|*.{extension}",
                DefaultExt = extension,
                AddExtension = true
            };
            if (dialog.ShowDialog() ?? false)
            {
                return dialog.FileName;
            }
            return null;
        }

        public static string ChooseOpenFile(string name, string extension)
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{name} (*.{extension})|*.{extension}",
                DefaultExt = extension,
                AddExtension = true
            };
            if (dialog.ShowDialog() ?? false)
            {
                return dialog.FileName;
            }
            return null;
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
