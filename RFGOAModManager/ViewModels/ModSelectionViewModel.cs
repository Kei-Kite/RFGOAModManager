using RFGOAModManager.Models;
using System.ComponentModel;

namespace RFGOAModManager.ViewModels
{
    public class ModSelectionViewModel : INotifyPropertyChanged
    {
        public Mod Mod { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        private bool _isMissingFromLibrary;
        public bool IsMissingFromLibrary
        {
            get => _isMissingFromLibrary;
            set
            {
                if (_isMissingFromLibrary != value)
                {
                    _isMissingFromLibrary = value;
                    OnPropertyChanged(nameof(IsMissingFromLibrary));
                }
            }
        }

        public string Name => Mod.Name;

        public ModSelectionViewModel(Mod mod)
        {
            Mod = mod;
            _isMissingFromLibrary = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
