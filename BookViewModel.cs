using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GUI_FOR_BOOK
{
    public class BookViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> _bookLines;
        private string _statusMessage;

        public ObservableCollection<string> BookLines
        {
            get => _bookLines;
            set
            {
                _bookLines = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public BookViewModel()
        {
            BookLines = new ObservableCollection<string>();
            StatusMessage = "Готов к загрузке книги";
        }

        public void LoadBook(string filePath)
        {
            try
            {
                BookLines.Clear();
                StatusMessage = "Загружаем книгу...";

                using StreamReader sr = new StreamReader(filePath);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    BookLines.Add(line);
                }

                StatusMessage = $"Книга загружена! Строк: {BookLines.Count}";
            }
            catch (Exception e)
            {
                StatusMessage = "Ошибка загрузки книги";
                MessageBox.Show(
                    $"Exception: {e.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
