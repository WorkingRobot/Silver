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

        private void SelectPaks(object sender, RoutedEventArgs e)
        {
            string aes = AesBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(aes))
            {
                if (Helpers.AskConfirmation(this, "You didn't enter an AES key! Are you sure your paks are unencrypted?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
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
                key = Helpers.StringToByteArray(aes.ToLowerInvariant());
                if (key == null || key.Length != 32)
                    throw new System.ArgumentNullException();
            }
            catch
            {
                Helpers.AskConfirmation(this, "That isn't a valid AES key!", MessageBoxButton.OK);
                return;
            }

            var files = Helpers.ChooseOpenFiles("Pak Files", "pak");
            if (files == null || files.Length == 0)
            {
                return;
            }

            MainWindow.Project.Name = "Startup Project";
            foreach (var file in files)
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
