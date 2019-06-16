using System.Diagnostics;
using System.Windows;

namespace Silver
{
    /// <summary>
    /// Interaction logic for Startup.xaml
    /// </summary>
    public partial class Startup : Window
    {
        MainWindow MainWindow;

        public Startup(MainWindow mainWindow)
        {
            InitializeComponent();
            MainWindow = mainWindow;
        }

        string[] paks;

        private void SelectPaks(object sender, RoutedEventArgs e)
        {
            var files = Helpers.ChooseOpenFiles("Pak Files", "pak");
            if (files != null)
            {
                paks = files;
            }
        }

        private void OpenPaks(object sender, RoutedEventArgs e)
        {
            if (paks == null || paks.Length == 0)
            {
                Helpers.AskConfirmation(this, "You didn't select any pak files!", MessageBoxButton.OK);
                return;
            }

            string aes = AesBox.Text;
            if (string.IsNullOrWhiteSpace(aes))
            {
                if (Helpers.AskConfirmation(this, "You didn't enter an AES key! Are you sure these paks are unencrypted?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            if (aes.StartsWith("0x"))
            {
                aes = aes.Substring(2);
            }

            byte[] key;
            try
            {
                key = Helpers.StringToByteArray(aes);
            }
            catch
            {
                Helpers.AskConfirmation(this, "That isn't a valid AES key!", MessageBoxButton.OK);
                return;
            }

            MainWindow.Project.Name = "Startup Project";
            foreach (var file in paks)
            {
                MainWindow.Project.Files.Add(new ProjectFile(file, key));
            }
            MainWindow.ProjectDirty = true;
            MainWindow.LoadProject();
            MainWindow.Refresh();
            Close();
        }

        private void OpenGithub(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/WorkingRobot/Silver");
        }

        private void OpenTwitter(object sender, RoutedEventArgs e)
        {
            Process.Start("https://twitter.com/Asriel_Dev");
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
