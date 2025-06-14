using RFGOAModManager.Commands;
using RFGOAModManager.Models;
using RFGOAModManager.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using WinForms = System.Windows.Forms;
using WpfMessageBox = System.Windows.MessageBox;

namespace RFGOAModManager.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ModSelectionViewModel> AvailableMods { get; } = new();
        public ObservableCollection<ModSelectionViewModel> LoadOrderMods { get; } = new();

        private readonly string _modLibraryFolder;
        private readonly string _loadOrdersFolder;

        private const string ConfigFileName = "config.json";
        private readonly string _configFilePath;

        private string _steamModsFolder;

        public ICommand AddModCommand { get; }
        public ICommand RemoveModCommand { get; }
        public ICommand ImportModsCommand { get; }
        public ICommand ExportLoadOrderCommand { get; }
        public ICommand SaveLoadOrderCommand { get; }
        public ICommand LoadLoadOrderCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand OpenModsFolderCommand { get; }
        public ICommand CheckAllAvailableCommand { get; }
        public ICommand UncheckAllAvailableCommand { get; }
        public ICommand CheckAllLoadOrderCommand { get; }
        public ICommand UncheckAllLoadOrderCommand { get; }
        public ICommand InfoButtonCommand { get; }
        public ICommand ReloadModsCommand { get; }
        public ICommand OpenFolderSelectionCommand { get; }
        public ICommand OpenGameCommand { get; }


        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindowViewModel()
        {
            _modLibraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RFGOAModManager", "Library");
            _loadOrdersFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RFGOAModManager", "LoadOrders");
            _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RFGOAModManager", ConfigFileName);

            _steamModsFolder = LoadOrSelectSteamGameFolder();

            AddModCommand = new RelayCommand(AddMods, () => AvailableMods.Any(m => m.IsSelected));
            RemoveModCommand = new RelayCommand(RemoveMods, () => LoadOrderMods.Any(m => m.IsSelected));
            ImportModsCommand = new RelayCommand(ImportMods);
            ExportLoadOrderCommand = new RelayCommand(ExportLoadOrder);
            SaveLoadOrderCommand = new RelayCommand(SaveLoadOrder);
            LoadLoadOrderCommand = new RelayCommand(LoadLoadOrder);
            MoveUpCommand = new RelayCommand(MoveUp, () => LoadOrderMods.Any(m => m.IsSelected && LoadOrderMods.IndexOf(m) > 0));
            MoveDownCommand = new RelayCommand(MoveDown, () => LoadOrderMods.Any(m => m.IsSelected && LoadOrderMods.IndexOf(m) < LoadOrderMods.Count - 1));
            OpenModsFolderCommand = new RelayCommand(OpenModsFolder);
            CheckAllAvailableCommand = new RelayCommand(() => {
                foreach (var mod in AvailableMods)
                    mod.IsSelected = true;
            });

            UncheckAllAvailableCommand = new RelayCommand(() => {
                foreach (var mod in AvailableMods)
                    mod.IsSelected = false;
            });

            CheckAllLoadOrderCommand = new RelayCommand(() => {
                foreach (var mod in LoadOrderMods)
                    mod.IsSelected = true;
            });

            UncheckAllLoadOrderCommand = new RelayCommand(() => {
                foreach (var mod in LoadOrderMods)
                    mod.IsSelected = false;
            });
            InfoButtonCommand = new RelayCommand(InfoButton);
            ReloadModsCommand = new RelayCommand(ReloadMods);
            OpenFolderSelectionCommand = new RelayCommand(OpenFolderSelection);
            OpenGameCommand = new RelayCommand(OpenGame);

            EnsureFoldersExist();
            LoadModsFromLibrary();
            LoadCurrentLoadOrder();

        }

        private bool _hasForcedLoadOrderMods;
        public bool HasForcedLoadOrderMods
        {
            get => _hasForcedLoadOrderMods;
            private set
            {
                if (_hasForcedLoadOrderMods != value)
                {
                    _hasForcedLoadOrderMods = value;
                    OnPropertyChanged(nameof(HasForcedLoadOrderMods));
                }
            }
        }

        private string _forcedLoadOrderWarning = string.Empty;
        public string ForcedLoadOrderWarning
        {
            get => _forcedLoadOrderWarning;
            private set
            {
                if (_forcedLoadOrderWarning != value)
                {
                    _forcedLoadOrderWarning = value;
                    OnPropertyChanged(nameof(ForcedLoadOrderWarning));
                }
            }
        }

        private void EnsureFoldersExist()
        {
            Directory.CreateDirectory(_modLibraryFolder);
            Directory.CreateDirectory(_loadOrdersFolder);
        }

        private void LoadModsFromLibrary()
        {
            if (!Directory.Exists(_modLibraryFolder))
            {
                AvailableMods.Clear();
                return;
            }

            var allFiles = Directory.GetFiles(_modLibraryFolder);

            var groupedByBaseName = allFiles
            .GroupBy(f => Path.Combine(Path.GetDirectoryName(f), Path.GetFileNameWithoutExtension(f)))
            .Where(g =>
            {
                var exts = g.Select(f => Path.GetExtension(f).ToLowerInvariant()).ToList();
                return (exts.Contains(".pak") && exts.Count == 1) || 
                       (exts.Contains(".pak") && exts.Contains(".ucas") && exts.Contains(".utoc")); 
            })
            .ToList();

            var mods = groupedByBaseName
                .Select(g => new Mod(Path.GetFileNameWithoutExtension(g.Key), g.ToList()))
                .ToList();

            AvailableMods.Clear();
            foreach (var mod in mods)
                AvailableMods.Add(new ModSelectionViewModel(mod));

            LoadOrderMods.Clear();
        }

        private void ReloadMods()
        {
            LoadModsFromLibrary();
            LoadCurrentLoadOrder(); 

            CommandManager.InvalidateRequerySuggested();
        }

        private void AddMods()
        {
            var selectedMods = AvailableMods.Where(m => m.IsSelected).ToList();

            foreach (var selModVM in selectedMods)
            {
                if (!LoadOrderMods.Any(m => m.Mod.Name == selModVM.Mod.Name))
                    LoadOrderMods.Add(new ModSelectionViewModel(selModVM.Mod));

                selModVM.IsSelected = false;
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void RemoveMods()
        {
            var selectedMods = LoadOrderMods.Where(m => m.IsSelected).ToList();

            foreach (var selModVM in selectedMods)
                LoadOrderMods.Remove(selModVM);

            CommandManager.InvalidateRequerySuggested();
        }

        private void ImportMods()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Select Mod Files to Import",
                Filter = "Mod files (*.pak;*.ucas;*.utoc)|*.pak;*.ucas;*.utoc",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                var grouped = dlg.FileNames
                .GroupBy(f => Path.Combine(Path.GetDirectoryName(f), Path.GetFileNameWithoutExtension(f)))
                .ToList();

                var importReport = new List<string>();

                foreach (var group in grouped)
                {
                    var files = group.ToList();
                    var exts = files.Select(f => Path.GetExtension(f).ToLowerInvariant()).ToList();
                    var modName = Path.GetFileNameWithoutExtension(group.Key);

                    if (exts.Contains(".pak") && (exts.Contains(".ucas") && exts.Contains(".utoc") || exts.Count == 1))
                    {
                        ImportModFiles(files);
                        importReport.Add($"{modName} - Imported ({string.Join(", ", exts)})");
                    }
                    else
                    {
                        importReport.Add($"{modName} - Skipped (incomplete set: {string.Join(", ", exts)})");
                    }
                }

                ReloadMods();

                string message = string.Join("\n", importReport);
                WpfMessageBox.Show(message, "Import Summary", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportModFiles(List<string> modFiles)
        {
            foreach (var file in modFiles)
            {
                string dest = Path.Combine(_modLibraryFolder, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
            }
        }

        private void ExportLoadOrder()
        {
            try
            {
                ExportLoadOrderToSteamFolder();
                WpfMessageBox.Show("Mods exported to Steam folder successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Error exporting mods: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportLoadOrderToSteamFolder()
        {
            if (!Directory.Exists(_steamModsFolder))
                Directory.CreateDirectory(_steamModsFolder);

            foreach (var file in Directory.GetFiles(_steamModsFolder))
            {
                try { File.Delete(file); }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"Failed to delete file: {file}\n\n{ex.Message}", "Cleanup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            var modsWithPrefixIssues = new List<string>();

            for (int index = 0; index < LoadOrderMods.Count; index++)
            {
                var mod = LoadOrderMods[index].Mod;
                if (HasForcedLoadOrderName(mod.Name))
                    modsWithPrefixIssues.Add(mod.Name);

                foreach (var filePath in mod.Files)
                {
                    string ext = Path.GetExtension(filePath);
                    string newFileName = $"{index:D3}_{mod.Name}{ext}";
                    string destPath = Path.Combine(_steamModsFolder, newFileName);
                    File.Copy(filePath, destPath, overwrite: true);
                }
            }

            /*
            if (modsWithPrefixIssues.Any())
            {
                string message = "Some mods include prefixes that may force load order:\n\n" +
                                 string.Join("\n", modsWithPrefixIssues) +
                                 "\n\nThis manager overrides load order based on your list.";

                WpfMessageBox.Show(message, "Load Order Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            */
        }

        private void SaveLoadOrder()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "Save Load Order",
                Filter = "JSON Files (*.json)|*.json",
                FileName = "new_loadorder.json",
                InitialDirectory = _loadOrdersFolder
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    Directory.CreateDirectory(_loadOrdersFolder);
                    var modNames = LoadOrderMods.Select(m => m.Mod.Name).ToList();
                    string json = JsonSerializer.Serialize(modNames, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dlg.FileName, json);
                    WpfMessageBox.Show("Load order saved successfully.", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"Failed to save load order:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadLoadOrder()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Load Load Order",
                Filter = "JSON Files (*.json)|*.json",
                InitialDirectory = _loadOrdersFolder
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dlg.FileName);
                    var modNames = JsonSerializer.Deserialize<List<string>>(json);

                    if (modNames == null)
                    {
                        WpfMessageBox.Show("Failed to load load order: empty or invalid file.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    LoadOrderMods.Clear();

                    foreach (var name in modNames)
                    {
                        var mod = AvailableMods.Select(vm => vm.Mod).FirstOrDefault(m => m.Name == name);
                        if (mod != null)
                        {
                            LoadOrderMods.Add(new ModSelectionViewModel(mod));
                        }
                        else
                        {
                            WpfMessageBox.Show($"Mod '{name}' from load order not found in library.", "Load Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"Failed to load load order:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadCurrentLoadOrder()
        {
            LoadOrderMods.Clear();

            if (!Directory.Exists(_steamModsFolder))
                return;

            var files = Directory.GetFiles(_steamModsFolder);
            var groupedMods = files
                .GroupBy(f =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(f);

                    if (fileName.Length > 4 && char.IsDigit(fileName[0]) && fileName[3] == '_')
                        return fileName[4..]; 
                    else
                        return fileName;
                })
                .Where(g => g.Any(f => Path.GetExtension(f).Equals(".pak", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var missingMods = new List<string>();

            foreach (var group in groupedMods)
            {
                string modName = group.Key;
                List<string> modFiles = group.ToList();

                var mod = AvailableMods.Select(vm => vm.Mod).FirstOrDefault(m => m.Name == modName);

                if (mod != null)
                {
                    LoadOrderMods.Add(new ModSelectionViewModel(mod));
                }
                else
                {
                    var dummyMod = new Mod(modName, modFiles);
                    var dummyVM = new ModSelectionViewModel(dummyMod)
                    {
                        IsMissingFromLibrary = true
                    };
                    LoadOrderMods.Add(dummyVM);
                    missingMods.Add(modName);
                }
            }

            if (missingMods.Any())
            {
                string message = "The following mods are active in the Steam folder but not found in the mod library:\n\n" +
                                 string.Join("\n", missingMods) +
                                 "\n\nThey are not imported properly and will be overwritten if you try to export the load order. Please import them first to use them correctly.";
                WpfMessageBox.Show(message, "Missing Mods Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        /*
        private bool HasForcedLoadOrderName(string modName)
        {
            return modName.StartsWith("001_") || modName.StartsWith("002_") || modName.StartsWith("003_");
        }
        */

        private bool HasForcedLoadOrderName(string modName)
        {
            if (string.IsNullOrEmpty(modName)) return false;

            int underscoreIndex = modName.IndexOf('_');
            if (underscoreIndex <= 0) return false; 

            string prefix = modName.Substring(0, underscoreIndex + 1);

            if (prefix.Length == 4)
            {

                if (char.IsDigit(prefix[0]) && char.IsDigit(prefix[1]) && char.IsDigit(prefix[2]))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        private string LoadOrSelectSteamGameFolder()
        {
            string? gameFolderRoot = null;

            if (File.Exists(_configFilePath))
            {
                try
                {
                    var configJson = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<Config>(configJson);
                    if (config != null && Directory.Exists(config.SteamGameFolder))
                        return config.SteamGameFolder;
                }
                catch { }
            }

            if (gameFolderRoot == null)
            {
                var possibleRoots = new[]
                {
                    @"C:\Program Files (x86)\Steam\steamapps\common\Rune Factory Guardians of Azuma",
                    @"D:\SteamLibrary\steamapps\common\Rune Factory Guardians of Azuma"
                };

                foreach (var root in possibleRoots)
                {
                    if (Directory.Exists(root))
                    {
                        gameFolderRoot = root;
                        break;
                    }
                }
            }

            if (gameFolderRoot == null)
            {
                var dialog = new WinForms.FolderBrowserDialog
                {
                    Description = "Select Rune Factory Guardians of Azuma game folder root"
                };

                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                    gameFolderRoot = dialog.SelectedPath;
            }

            if (gameFolderRoot == null)
                throw new InvalidOperationException("Game folder root must be selected.");

            var modsFolder = Path.Combine(gameFolderRoot, "Game", "Content", "Paks", "~mods");

            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
            }

            SaveSteamFolderToConfig(modsFolder);

            return modsFolder;
        }

        private void SaveSteamFolderToConfig(string rootPath)
        {
            var config = new Config
            {
                SteamGameFolder = rootPath
            };

            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath)!);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, json);
        }

        private void MoveUp()
        {
            var selected = LoadOrderMods.Where(m => m.IsSelected).ToList();

            foreach (var mod in selected)
            {
                int index = LoadOrderMods.IndexOf(mod);
                if (index > 0)
                {
                    LoadOrderMods.Move(index, index - 1);
                }
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void MoveDown()
        {
            var selected = LoadOrderMods.Where(m => m.IsSelected).Reverse().ToList();

            foreach (var mod in selected)
            {
                int index = LoadOrderMods.IndexOf(mod);
                if (index < LoadOrderMods.Count - 1)
                {
                    LoadOrderMods.Move(index, index + 1);
                }
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void OpenModsFolder()
        {
            if (Directory.Exists(_modLibraryFolder))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = _modLibraryFolder,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"Failed to open mods folder:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                WpfMessageBox.Show("Mods folder does not exist.", "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void InfoButton()
        {
            var infoWindow = new InfoWindow
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            infoWindow.ShowDialog();
        }

        private void OpenFolderSelection()
        {
            var folderWindow = new RFGOAModManager.Views.FolderSelectionWindow(_steamModsFolder);

            folderWindow.Owner = System.Windows.Application.Current.MainWindow;

            if (folderWindow.ShowDialog() == true)
            {
                var selectedFolder = folderWindow.SelectedFolder;
                if (selectedFolder != null && Path.GetFileName(selectedFolder) == "~mods")
                {
                    if (!Directory.Exists(selectedFolder))
                        Directory.CreateDirectory(selectedFolder);

                    _steamModsFolder = selectedFolder;

                    SaveSteamFolderToConfig(selectedFolder);

                    LoadModsFromLibrary();
                    LoadCurrentLoadOrder();

                    WpfMessageBox.Show($"Steam game folder updated and saved:\n{selectedFolder}", "Folder Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    WpfMessageBox.Show("Invalid folder. Please select a folder named '~mods'.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OpenGame()
        {
            try
            {
                const string steamAppId = "2864560";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"steam://run/{steamAppId}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Failed to launch the game via Steam:\n{ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}