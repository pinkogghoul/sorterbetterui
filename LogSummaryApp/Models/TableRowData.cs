using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace LogSummaryApp.Models
{
    public class TableRowData : INotifyPropertyChanged
    {
        private int _index;
        private int _count;
        private string _category = "";
        private int _active;
        private int _inactive;
        private BitmapImage? _icon;

        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(); }
        }

        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public int Active
        {
            get => _active;
            set { _active = value; OnPropertyChanged(); }
        }

        public int Inactive
        {
            get => _inactive;
            set { _inactive = value; OnPropertyChanged(); }
        }

        public BitmapImage? Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
