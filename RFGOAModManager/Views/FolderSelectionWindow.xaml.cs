using MahApps.Metro.Controls;
using System.Windows;
using WinForms = System.Windows.Forms;
using WpfMessageBox = System.Windows.MessageBox;

namespace RFGOAModManager.Views
{
    public partial class FolderSelectionWindow : MetroWindow
    {
        public string SelectedFolder { get; private set; }

        public FolderSelectionWindow(string initialFolder)
        {
            InitializeComponent();

            if (!string.IsNullOrWhiteSpace(initialFolder))
                FolderPathTextBox.Text = initialFolder;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            if (!string.IsNullOrWhiteSpace(FolderPathTextBox.Text) && System.IO.Directory.Exists(FolderPathTextBox.Text))
            {
                dialog.SelectedPath = FolderPathTextBox.Text;
            }

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                FolderPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.Directory.Exists(FolderPathTextBox.Text))
            {
                SelectedFolder = FolderPathTextBox.Text;
                DialogResult = true;
            }
            else
            {
                WpfMessageBox.Show("Selected folder does not exist.", "Invalid Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
