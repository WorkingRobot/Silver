using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Silver
{
    /// <summary>
    /// Interaction logic for Imports.xaml
    /// </summary>
    public partial class ProjectProps : Window
    {
        Project Project;
        public bool Dirty { get; private set; }

        ObservableCollection<ImportContext> ImportList = new ObservableCollection<ImportContext>();

        public ProjectProps(Project file)
        {
            Project = file;
            foreach(var pak in file.Files)
            {
                ImportList.Add(new ImportContext
                {
                    Path = pak.Path,
                    Aes = pak.Key
                });
            }
            InitializeComponent();
            ImportGrid.ItemsSource = ImportList;
            ProjectNameTxt.Text = file.Name;
        }

        private void Add_File(object sender, RoutedEventArgs e)
        {
            var files = Helpers.ChooseOpenFiles("Pak File", "pak");
            foreach (var file in files)
            {
                if (!File.Exists(file)) continue;
                ImportList.Add(new ImportContext
                {
                    Path = file
                });
            }
        }

        private void Add_Folder(object sender, RoutedEventArgs e)
        {
            var folder = Helpers.ChooseFolder();
            if (!Directory.Exists(folder)) return;
            foreach(var path in Directory.EnumerateFiles(folder, "*.pak"))
            {
                ImportList.Add(new ImportContext
                {
                    Path = path
                });
            }
        }

        private void Click_OK(object sender, RoutedEventArgs e)
        {
            Click_Apply(sender, e);
            Close();
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Click_Apply(object sender, RoutedEventArgs e)
        {
            Project.Name = ProjectNameTxt.Text;
            Project.Files.Clear();
            foreach(var file in ImportList)
            {
                Project.Files.Add(new ProjectFile(file.Path, file.Aes));
            }
            Dirty = true;
        }

        class ImportContext
        {
            public string Path { get; set; }
            public string Key
            {
                get
                {
                    return key_ == null ? (key_ = Helpers.ToHex(Aes)) : key_;
                }
                set
                {
                    value = value.ToLowerInvariant();
                    if (value.StartsWith("0x"))
                    {
                        value = value.Substring(2);
                    }
                    Aes = Helpers.StringToByteArray(value);
                    key_ = Helpers.ToHex(Aes);
                }
            }

            string key_;
            public byte[] Aes;
        }
    }
}
