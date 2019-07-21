using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Silver
{
    /// <summary>
    /// Interaction logic for LoadingProgress.xaml
    /// </summary>
    public partial class LoadingProgress : Window
    {
        public double Progress { get => ProgBar.Value; set => ProgBar.Value = value; }
        public string Text { get => ProgTxt.Text; set => ProgTxt.Text = value; }

        public LoadingProgress()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        [DllImport("user32.dll")]
        private static extern IntPtr DestroyMenu(IntPtr hWnd);

        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint SC_CLOSE = 0xF060;

        IntPtr menuHandle;
        void DisableCloseButton()
        {
            menuHandle = GetSystemMenu(new WindowInteropHelper(this).Handle, false);
            if (menuHandle != IntPtr.Zero)
            {
                EnableMenuItem(menuHandle, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
            }
        }

        private void SourceInitializedHandler(object sender, EventArgs e)
        {
            DisableCloseButton();
        }
    }
}
