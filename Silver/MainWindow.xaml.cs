using PakReader;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Silver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Project Project = new Project();
        public string ProjectPath;
        public bool ProjectDirty = false;
        public bool Searching;
        string cwd_;
        public string WorkingDir { get => cwd_; set { cwd_ = value; WorkingDirTxt.Text = value; } }
        public PathHistory History = new PathHistory(50);
        FSCollection<PakPackage> Index = new FSCollection<PakPackage>();

        ObservableCollection<PanelItem> Items = new ObservableCollection<PanelItem>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new Context(this);
            Title = "Silver - Untitled Project";
            new Startup(this).ShowDialog();
        }

        private void Click_New(object sender, RoutedEventArgs e)
        {
            if (ProjectDirty)
            {
                switch (this.SaveFileCheck())
                {
                    case MessageBoxResult.Yes:
                        string file = Helpers.ChooseSaveFile("Silver Project File", "slv");
                        if (file != null)
                        {
                            Project.Save(file);
                        }
                        else // Cancel or X
                        {
                            return;
                        }
                        break;
                    case MessageBoxResult.No:
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }
            ProjectPath = null;
            Project = new Project();
            ProjectDirty = false;
        }

        private void Click_Open(object sender, RoutedEventArgs e)
        {
            if (ProjectDirty)
            {
                switch (this.SaveFileCheck())
                {
                    case MessageBoxResult.Yes:
                        string file = Helpers.ChooseSaveFile("Silver Project File", "slv");
                        if (file != null)
                        {
                            Project.Save(file);
                        }
                        else // Cancel or X
                        {
                            return;
                        }
                        break;
                    case MessageBoxResult.No:
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }
            string fileName = Helpers.ChooseOpenFile("Silver Project File", "slv");
            if (fileName != null)
            {
                ProjectPath = fileName;
                Project = new Project(fileName);
                LoadProject();
                Refresh();
            }
            else
            {
                ProjectPath = null;
                Project = new Project();
            }
            ProjectDirty = false;
        }

        private void Click_Save(object sender, RoutedEventArgs e)
        {
            if (ProjectPath == null)
            {
                ProjectPath = Helpers.ChooseSaveFile("Silver Project File", "slv");
                if (ProjectPath == null)
                {
                    return;
                }
            }
            Project.Save(ProjectPath);
            ProjectDirty = false;
        }

        private void Click_SaveAs(object sender, RoutedEventArgs e)
        {
            string file = Helpers.ChooseSaveFile("Silver Project File", "slv");
            if (file == null)
            {
                return;
            }
            ProjectPath = file;
            Project.Save(ProjectPath);
            ProjectDirty = false;
        }

        private void Click_Exit(object sender, RoutedEventArgs e)
        {
            if (ProjectDirty)
            {
                switch (this.SaveFileCheck())
                {
                    case MessageBoxResult.Yes:
                        string file = Helpers.ChooseSaveFile("Silver Project File", "slv");
                        if (file != null)
                        {
                            Project.Save(file);
                        }
                        else // Cancel or X
                        {
                            return;
                        }
                        break;
                    case MessageBoxResult.No:
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (ProjectDirty)
            {
                switch (this.SaveFileCheck())
                {
                    case MessageBoxResult.Yes:
                        string file = Helpers.ChooseSaveFile("Silver Project File", "slv");
                        if (file != null)
                        {
                            Project.Save(file);
                        }
                        else // Cancel or X
                        {
                            e.Cancel = true;
                            break;
                        }
                        break;
                    case MessageBoxResult.No:
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void Click_Find(object sender, RoutedEventArgs e)
        {
            FilterTxt.Focus();
        }

        private void Click_EditProject(object sender, RoutedEventArgs e)
        {
            var window = new ProjectProps(Project);
            window.ShowDialog();
            if (window.Dirty)
            {
                LoadProject();
                Refresh();
            }
        }

        private void Click_Settings(object sender, RoutedEventArgs e)
        {
            // Settings window not added yet
        }

        private void Click_Exports(object sender, RoutedEventArgs e)
        {
            // Export window not added yet
        }

        private void Click_Search(object sender, RoutedEventArgs e)
        {
            if (Searching)
            {
                Items.Clear();
                foreach (var entry in Index.GetDirectory(History.Current).Search(FilterTxt.Text))
                {
                    PakPackage file = entry.Value as PakPackage;
                    Items.Add(new PanelItem()
                    {
                        IsDirectory = file == null,
                        Name = History.Current + (string.IsNullOrEmpty(entry.Key.Path) ? "" : entry.Key.Path.Substring(1) + "/") + entry.Key.Name,
                        Size = file != null ? file.uasset?.UncompressedSize ?? 0 + file.uexp?.UncompressedSize ?? 0 + file.ubulk?.UncompressedSize ?? 0 : 0,
                        Assets = file?.ubulk != null ? "Bulk" : null,
                        Openable = file == null || (file.uasset != null && file.uexp != null)
                    });
                }
                WorkingDir = "Search Results for " + FilterTxt.Text;
            }
        }

        private void FilterTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                Searching = !string.IsNullOrWhiteSpace(FilterTxt.Text);
                Click_Search(null, null);
            }
        }

        private void Click_Up(object sender, RoutedEventArgs e)
        {
            if (Searching)
            {
                Searching = false;
                WorkingDir = History.MoveBack();
                Refresh();
                return;
            }
            if (!string.IsNullOrEmpty(WorkingDir) && WorkingDir != "/")
            {
                WorkingDir = History.Navigate(WorkingDir.Substring(0, WorkingDir.LastIndexOf('/', WorkingDir.Length - 2)) + '/');
                Refresh();
            }
        }

        private void Click_Back(object sender, RoutedEventArgs e)
        {
            if (WorkingDir == null)
                return;
            if (!Searching)
                WorkingDir = History.MoveBack();
            Refresh();
        }

        private void Click_Forward(object sender, RoutedEventArgs e)
        {
            if (WorkingDir == null)
                return;
            WorkingDir = History.MoveForward();
            Refresh();
        }

        private void GotFocusPlaceholder(object sender, RoutedEventArgs e)
        {
            var textbox = sender as TextBox;
            if (textbox.Text == "Search" && textbox.Foreground == Brushes.Gray)
            {
                textbox.Text = "";
                textbox.Foreground = Brushes.Black;
            }
        }

        private void LostFocusPlaceholder(object sender, RoutedEventArgs e)
        {
            var textbox = sender as TextBox;
            if (textbox.Text == "")
            {
                textbox.Foreground = Brushes.Gray;
                textbox.Text = "Search";
            }
        }

        internal void Refresh()
        {
            Items.Clear();
            var dir = Index.GetDirectory(History.Current);
            if (dir != null)
            {
                foreach(var entry in dir)
                {
                    PakPackage file = entry.Value as PakPackage;
                    Items.Add(new PanelItem()
                    {
                        IsDirectory = file == null,
                        Name = entry.Key,
                        Size = file != null ? file.uasset?.UncompressedSize ?? 0 + file.uexp?.UncompressedSize ?? 0 + file.ubulk?.UncompressedSize ?? 0 : 0,
                        Assets = file?.ubulk != null ? "Bulk" : null,
                        Openable = file == null || (file.uasset != null && file.uexp != null)
                    });
                }
                WorkingDir = History.Current;
            }
        }

        internal void LoadProject()
        {
            Title = "Silver - " + Project.Name;
            History.Clear();
            WorkingDir = History.Navigate("/");
            FilePanel.ItemsSource = Items;
            foreach (var file in Project.Files)
            {
                PakReader.PakReader reader;
                if (file.Index != null)
                {
                    if (file.Index.Type == ProjectFileIndex.IndexType.FILE_INFO)
                    {
                        reader = new PakReader.PakReader(file.Path, file.Key, false);
                        foreach (var entry in file.Index.Index)
                        {
                            var (Path, Extension) = Helpers.GetPath(entry.Name);
                            var package = Index[file.MountPoint + Path] ?? new PakPackage();
                            switch (Extension)
                            {
                                case "uasset":
                                    package.uasset = entry.Info;
                                    package.AssetReader = reader;
                                    break;
                                case "uexp":
                                    package.uexp = entry.Info;
                                    package.ExpReader = reader;
                                    break;
                                case "ubulk":
                                    package.ubulk = entry.Info;
                                    package.BulkReader = reader;
                                    break;
                                default:
                                    //Console.WriteLine($"Unknown extension: {Extension} in {Path}");
                                    break;
                            }
                            Index[file.MountPoint + Path] = package;
                        }
                    }/*
                    else if (file.Index.Type == ProjectFileIndex.IndexType.FILE_NAME)
                    {
                        reader = new PakReader.PakReader(file.Path, file.Key, false);
                        foreach (var entry in file.Index.Index)
                        {
                            var path = Helpers.GetPath(entry.Name);
                            var package = Index[file.MountPoint + path.Path] ?? new PakPackage();
                            switch (path.Extension)
                            {
                                case "uasset":
                                    package.AssetReader = reader;
                                    break;
                                case "uexp":
                                    package.ExpReader = reader;
                                    break;
                                case "ubulk":
                                    package.BulkReader = reader;
                                    break;
                                default:
                                    Console.WriteLine($"Unknown extension: {path.Extension} in {path.Path}");
                                    break;
                            }
                            Index[file.MountPoint + path.Path] = package;
                        }
                    }*/
                }
                else
                {
                    try
                    {
                        reader = new PakReader.PakReader(file.Path, file.Key);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    foreach (var entry in reader.FileInfos)
                    {
                        var (Path, Extension) = Helpers.GetPath(entry.Name);
                        var package = Index[file.MountPoint + Path] ?? new PakPackage();
                        switch (Extension)
                        {
                            case "uasset":
                                package.uasset = entry;
                                package.AssetReader = reader;
                                break;
                            case "uexp":
                                package.uexp = entry;
                                package.ExpReader = reader;
                                break;
                            case "ubulk":
                                package.ubulk = entry;
                                package.BulkReader = reader;
                                break;
                            case "umap":
                                break;
                            default:
                                //Console.WriteLine($"Unknown extension: {Extension} in {Path}");
                                break;
                        }
                        Index[file.MountPoint + Path] = package;
                    }
                }
            }
        }

        private void Entry_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            PanelItem Context = (PanelItem)((DataGridRow)sender).DataContext;
            if (!Context.Openable) return;
            if (Searching)
            {
                if (Context.IsDirectory)
                {
                    WorkingDir = History.Navigate(Context.Name + "/");
                    Searching = false;
                    Refresh();
                }
                else
                {
                    OpenViewer(Context.Name, Index[Context.Name]);
                }
                return;
            }
            if (Context.IsDirectory)
            {
                WorkingDir += Context.Name + "/";
                History.Navigate(WorkingDir);
                Refresh();
            }
            else
            {
                OpenViewer(Context.Name, Index[History.Current + Context.Name]);
            }
        }

        public void OpenViewer(string fileName, PakPackage package)
        {
            foreach (var exp in package.Exports ?? new ExportObject[0])
            {
                if (exp is Texture2D)
                {
                    var tex = exp as Texture2D;
                    tex.GetImage();
                }
            }
            AssetReader r = new AssetReader(
                    package.AssetReader.GetPackageStream(package.uasset),
                    package.ExpReader.GetPackageStream(package.uexp),
                    package.ubulk == null ? null : package.BulkReader.GetPackageStream(package.ubulk)
                );
            new FileViewer(fileName, package, p => Index[p]).ShowDialog();
        }

        class Context
        {
            MainWindow Window;
            public Context(MainWindow window)
            {
                Window = window;
            }
            public ICommand New => new CommandHandler(() => Window.Click_New(null, null));
            public ICommand Open => new CommandHandler(() => Window.Click_Open(null, null));
            public ICommand Save => new CommandHandler(() => Window.Click_Save(null, null));
            public ICommand SaveAs => new CommandHandler(() => Window.Click_SaveAs(null, null));
            public ICommand Find => new CommandHandler(() => Window.Click_Find(null, null));
            public ICommand EditProject => new CommandHandler(() => Window.Click_EditProject(null, null));
        }

        class CommandHandler : ICommand
        {
            private readonly Action _action;
            public CommandHandler(Action action)
            {
                _action = action;
            }

            public bool CanExecute(object parameter) => true;

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                _action();
            }
        }
    }
}
