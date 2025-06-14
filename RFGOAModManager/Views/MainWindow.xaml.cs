using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;
using RFGOAModManager.ViewModels;

namespace RFGOAModManager.Views
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        public MainWindowViewModel ViewModel => DataContext as MainWindowViewModel;

        private System.Windows.Point _dragStartPoint;

        private void LoadOrderListBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void LoadOrderListBox_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point mousePos = e.GetPosition(null);
            Vector diff = mousePos - _dragStartPoint;

            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                System.Windows.Controls.ListBox listBox = sender as System.Windows.Controls.ListBox;
                var listBoxItem = FindAncestor<System.Windows.Controls.ListBoxItem>((DependencyObject)e.OriginalSource);

                if (listBoxItem == null)
                    return;

                var mod = (ModSelectionViewModel)listBox.ItemContainerGenerator.ItemFromContainer(listBoxItem);
                if (mod == null)
                    return;

                var dragData = new System.Windows.DataObject("myFormat", mod);
                System.Windows.DragDrop.DoDragDrop(listBoxItem, dragData, System.Windows.DragDropEffects.Move);
            }
        }

        private void LoadOrderListBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent("myFormat"))
            {
                var mod = e.Data.GetData("myFormat") as ModSelectionViewModel;
                var listBox = sender as System.Windows.Controls.ListBox;

                System.Windows.Point dropPosition = e.GetPosition(listBox);
                var targetItem = GetItemAtPoint(listBox, dropPosition);

                int oldIndex = ViewModel.LoadOrderMods.IndexOf(mod);
                int newIndex = targetItem == null ? ViewModel.LoadOrderMods.Count - 1 : ViewModel.LoadOrderMods.IndexOf(targetItem);

                if (oldIndex != newIndex)
                {
                    ViewModel.LoadOrderMods.Move(oldIndex, newIndex);
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null && !(current is T))
            {
                current = VisualTreeHelper.GetParent(current);
            }
            return current as T;
        }

        private ModSelectionViewModel GetItemAtPoint(System.Windows.Controls.ListBox listBox, System.Windows.Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(listBox, point);
            DependencyObject obj = result?.VisualHit;

            while (obj != null && !(obj is System.Windows.Controls.ListBoxItem))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            if (obj is System.Windows.Controls.ListBoxItem listBoxItem)
            {
                return (ModSelectionViewModel)listBox.ItemContainerGenerator.ItemFromContainer(listBoxItem);
            }
            return null;
        }

    }
}
