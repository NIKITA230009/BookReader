using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Dapper;
using GTranslate.Translators;
using Npgsql;

namespace GUI_FOR_BOOK
{
    public interface ITextSelectionObserver
    {
        void OnTextSelected(string selectedText, int startPage, int startLine);
    }

    public interface ITextSelectionSubject
    {
        void RegisterSelectionObserver(ITextSelectionObserver observer);
        void RemoveSelectionObserver(ITextSelectionObserver observer);
        void NotifySelectionObservers(string selectedText, int startPage, int startLine);
    }

    public partial class MainWindow : Window, ITextSelectionSubject
    {
        private Book _currentBook;
        private string[] _currentBookLines;

        private ListBox BooksListBox;

        private Button LoadBookButton;
        private readonly DatabaseService _dbService;

        private string[] _lines;
        private int _currentPage = 0;
        private TextBox _textBox;
        private TextBlock _pagination;
        private int totalPages;
        private Popup _selectionPopup;

        private List<ITextSelectionObserver> _selectionObservers =
            new List<ITextSelectionObserver>();

        public MainWindow()
        {
            var connectionString =
                "Host=localhost;Port=5432;Database=BOOKS;Username=postgres;Password=your_password";
            _dbService = new DatabaseService(connectionString);
            InitializeComponent();
            // InitializeBook();
            InitializeUI();
            _ = LoadBooksList();
            // CreateControls();
        }

        private async Task LoadBooksList()
        {
            var books = await _dbService.GetBooksAsync();

            // Очищаем список
            BooksListBox.Items.Clear();

            // Добавляем книги
            foreach (var book in books)
            {
                BooksListBox.Items.Add(book);
            }

            // Чтобы показывалось название книги, нужно переопределить ToString()
        }

        // private void InitializeBook()
        // {
        //     _lines = File.ReadAllLines("Sample1.txt");
        //     const int LINES_PER_PAGE = 100;
        //     totalPages = (int)Math.Ceiling((double)_lines.Length / LINES_PER_PAGE);
        // }

        // Базовый репозиторий
        public interface IBookRepository
        {
            Task<List<Book>> GetAllBooksAsync();
            Task<Book> GetBookByIdAsync(int id);
            Task<int> AddBookAsync(Book book);
            Task<bool> DeleteBookAsync(int id);
        }

        // Модель книги
        public class Book
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string FilePath { get; set; }
            public DateTime CreatedAt { get; set; }
            public int TotalPages { get; set; }

            public override string ToString()
            {
                return $"{Title} - {Author}";
            }
        }

        public interface IBookmarkRepository
        {
            Task<List<Bookmark>> GetBookmarksAsync(int bookId);
            Task AddBookmarkAsync(Bookmark bookmark);
            Task RemoveBookmarkAsync(int bookmarkId);
        }

        // Модель закладки
        public class Bookmark
        {
            public int Id { get; set; }
            public int BookId { get; set; }
            public int PageNumber { get; set; }
            public int LineNumber { get; set; }
            public string SelectedText { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class DatabaseService
        {
            private readonly string _connectionString;

            public DatabaseService(string connectionString)
            {
                _connectionString = connectionString;
            }

            // Только работа с метаданными книг
            public async Task<List<Book>> GetBooksAsync()
            {
                using var connection = new NpgsqlConnection(_connectionString);

                // Явно укажи маппинг
                var books = await connection.QueryAsync<Book>(
                    "SELECT id, title, author, file_path as FilePath, created_at as CreatedAt FROM books ORDER BY title"
                );

                return books.ToList();
            }

            public async Task<int> AddBookAsync(string filePath, string title, string author = null)
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql =
                    @"INSERT INTO books (title, author, file_path) 
                    VALUES (@title, @author, @filePath) RETURNING id";
                return await connection.ExecuteScalarAsync<int>(
                    sql,
                    new
                    {
                        title,
                        author,
                        filePath,
                    }
                );
            }

            // public async Task UpdateCurrentPageAsync(int bookId, int pageNumber)
            // {
            //     using var connection = new NpgsqlConnection(_connectionString);
            //     var sql = "UPDATE books SET current_page = @pageNumber WHERE id = @bookId";
            //     await connection.ExecuteAsync(sql, new { bookId, pageNumber });
            // }

            // Закладки и заметки
            public async Task AddBookmarkAsync(
                int bookId,
                int pageNumber,
                string selectedText = null
            )
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql =
                    @"INSERT INTO bookmarks (book_id, page_number, selected_text) 
                    VALUES (@bookId, @pageNumber, @selectedText)";
                await connection.ExecuteAsync(
                    sql,
                    new
                    {
                        bookId,
                        pageNumber,
                        selectedText,
                    }
                );
            }

            public async Task<List<Bookmark>> GetBookmarksAsync(int bookId)
            {
                using var connection = new NpgsqlConnection(_connectionString);
                return (
                    await connection.QueryAsync<Bookmark>(
                        "SELECT * FROM bookmarks WHERE book_id = @bookId ORDER BY page_number",
                        new { bookId }
                    )
                ).ToList();
            }
        }

        // private void CreateControls()
        // {
        //     // Создаем ListBox
        //     BooksListBox = new ListBox();
        //     BooksListBox.Size = new Size(10, 10); // X, Y
        //     BooksListBox.Size = new Size(200, 300); // Width, Height
        //     BooksListBox.SelectedIndexChanged += BooksListBox_SelectedIndexChanged;
        //     this.Controls.Add(BooksListBox);

        //     // Создаем TextBox для текста книги
        //     TextContentTextBox = new TextBox();
        //     TextContentTextBox.Location = new Point(220, 10);
        //     TextContentTextBox.Size = new Size(400, 300);
        //     TextContentTextBox.Multiline = true;
        //     TextContentTextBox.ScrollBars = ScrollBars.Vertical;
        //     this.Controls.Add(TextContentTextBox);

        //     // Создаем кнопку загрузки
        //     LoadBookButton = new Button();
        //     LoadBookButton.Location = new Point(10, 320);
        //     LoadBookButton.Size = new Size(200, 30);
        //     LoadBookButton.Text = "Загрузить книгу";
        //     LoadBookButton.Click += LoadBookButton_Click;
        //     this.Controls.Add(LoadBookButton);
        // }

        private void InitializeUI()
        {
            var grid = new Grid();

            // Определяем колонки: список книг слева, текст справа
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(220) }); // Для списка книг
            grid.ColumnDefinitions.Add(
                new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } // Правая колонка: занимает всё оставшееся пространство (Star означает "подели оставшееся место")
            ); // Для текста

            // Определяем строки
            grid.RowDefinitions.Add(
                new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }
            ); // Основной контент
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Пагинация
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Навигация

            // Левая панель - список книг
            var leftPanel = new StackPanel();
            leftPanel.SetValue(Grid.ColumnProperty, 0);
            leftPanel.SetValue(Grid.RowProperty, 0);
            leftPanel.Margin = new Thickness(10);

            BooksListBox = new ListBox();
            BooksListBox.Width = 200;
            BooksListBox.Height = 100;
            BooksListBox.SelectionChanged += BooksListBox_SelectionChanged;

            var addBookButton = new Button();
            addBookButton.Content = "Добавить книгу";
            addBookButton.Width = 200;
            addBookButton.Height = 30;
            addBookButton.Margin = new Thickness(0, 10, 0, 0);
            addBookButton.Click += AddBookButton_Click;

            leftPanel.Children.Add(BooksListBox);
            leftPanel.Children.Add(addBookButton);

            // Правая панель - текст книги
            var rightPanel = new StackPanel();
            rightPanel.SetValue(Grid.ColumnProperty, 1);
            rightPanel.SetValue(Grid.RowProperty, 0);
            rightPanel.Margin = new Thickness(10);

            _textBox = new TextBox();
            _textBox.TextWrapping = TextWrapping.Wrap;
            _textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _textBox.FontSize = 20;
            _textBox.Padding = new Thickness(10);
            _textBox.IsReadOnly = true;
            _textBox.BorderThickness = new Thickness(0);
            _textBox.Background = Brushes.White;
            _textBox.SelectionChanged += TextBox_SelectionChanged;

            rightPanel.Children.Add(_textBox);

            // Пагинация (под текстом)
            _pagination = new TextBlock()
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10),
            };
            _pagination.SetValue(Grid.ColumnProperty, 1);
            _pagination.SetValue(Grid.RowProperty, 1);

            // Навигация (в самом низу)
            var navigationPanel = CreateNavigationPanel();
            navigationPanel.SetValue(Grid.ColumnProperty, 1);
            navigationPanel.SetValue(Grid.RowProperty, 2);

            // Добавляем все элементы в Grid
            grid.Children.Add(leftPanel);
            grid.Children.Add(rightPanel);
            grid.Children.Add(_pagination);
            grid.Children.Add(navigationPanel);

            this.Content = grid;

            // Регистрируем наблюдателей
            RegisterSelectionObserver(new HighlightObserver());
            RegisterSelectionObserver(new TranslationObserver(_textBox));
            RegisterSelectionObserver(new BookmarkObserver());
        }

        private async void BooksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // try
            // {
            //     MessageBox.Show($"Выбрано элементов: {BooksListBox.SelectedItems.Count}");

            //     {
            //         MessageBox.Show(
            //             $"Выбрана книга: {selectedBook.Title}\nПуть: {selectedBook.FilePath}"
            //         );

            // АВТОМАТИЧЕСКИ загружаем выбранную книгу
            if (BooksListBox.SelectedItem is Book selectedBook)
            {
                await LoadBookAsync(selectedBook);
            }
            //     }
            //     else
            //     {
            //         MessageBox.Show("Выбранный элемент не является книгой!");
            //     }
            // }
            // catch (Exception ex)
            // {
            //     MessageBox.Show($"Ошибка при выборе книги: {ex.Message}", "Ошибка");
            // }
        }

        private async void AddBookButton_Click(object sender, RoutedEventArgs e)
        {
            // Диалог выбора файла
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            dialog.Title = "Выберите книгу для добавления";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = dialog.FileName;
                    string title = System.IO.Path.GetFileNameWithoutExtension(filePath);

                    // Добавляем книгу в БД
                    int bookId = await _dbService.AddBookAsync(
                        filePath,
                        title,
                        "Неизвестный автор"
                    );

                    // Обновляем список книг
                    await LoadBooksList();

                    MessageBox.Show(
                        $"Книга '{title}' успешно добавлена!",
                        "",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при добавлении книги: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        private async Task LoadBookAsync(Book book)
        {
            try
            {
                // Проверяем что book и FilePath не null
                if (book == null)
                {
                    MessageBox.Show("Ошибка: книга не выбрана!");
                    return;
                }

                if (string.IsNullOrEmpty(book.FilePath))
                {
                    MessageBox.Show($"Ошибка: у книги '{book.Title}' не указан путь к файлу!");
                    return;
                }

                _currentBook = book;
                _currentBookLines = File.ReadAllLines(book.FilePath);
                _currentPage = 0;
                ShowPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книги: {ex.Message}", "Ошибка");
            }
        }

        #region Реализация ITextSelectionSubject
        public void RegisterSelectionObserver(ITextSelectionObserver observer)
        {
            _selectionObservers.Add(observer);
        }

        public void RemoveSelectionObserver(ITextSelectionObserver observer)
        {
            _selectionObservers.Remove(observer);
        }

        public void NotifySelectionObservers(string selectedText, int startPage, int startLine)
        {
            foreach (var observer in _selectionObservers)
            {
                observer.OnTextSelected(selectedText, startPage, startLine);
            }
        }
        #endregion

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_textBox.SelectionLength > 0)
            {
                string selectedText = _textBox.SelectedText.Trim();
                if (!string.IsNullOrEmpty(selectedText) && selectedText.Length > 0)
                {
                    int startLine = FindLineNumberForSelection();
                    // ShowPopupAboveSelection(selectedText);
                    NotifySelectionObservers(selectedText, _currentPage, startLine);
                }
            }
        }

        private int FindLineNumberForSelection()
        {
            return _currentPage * 29
                + _textBox.GetLineIndexFromCharacterIndex(_textBox.SelectionStart);
        }

        private void ShowPage()
        {
            if (_currentBookLines == null)
            {
                _textBox.Text = "Выберите книгу из списка";
                return;
            }

            const int LINES_PER_PAGE = 29;

            // Вычисляем totalPages для текущей книги
            int totalPagesForCurrentBook =
                (_currentBookLines.Length + LINES_PER_PAGE - 1) / LINES_PER_PAGE;

            // Корректируем текущую страницу
            _currentPage = Math.Max(0, Math.Min(_currentPage, totalPagesForCurrentBook - 1));

            int start_index = _currentPage * LINES_PER_PAGE;
            int end_index = Math.Min(start_index + LINES_PER_PAGE, _currentBookLines.Length);

            StringBuilder stringBuilder = new StringBuilder();
            for (int i = start_index; i < end_index; i++)
            {
                stringBuilder.AppendLine(_currentBookLines[i]);
            }

            _textBox.Text = stringBuilder.ToString();
            _pagination.Text = $"Страница {_currentPage + 1} из {totalPagesForCurrentBook}";
        }

        // private void ShowPopupAboveSelection(string text)
        // {
        //     if (_selectionPopup == null)
        //     {
        //         _selectionPopup = new Popup();
        //         _selectionPopup.Placement = PlacementMode.Relative;
        //         _selectionPopup.PlacementTarget = _textBox;

        //         var new_border = new Border()
        //         {
        //             Background = Brushes.LightYellow,
        //             BorderBrush = Brushes.Gray,
        //             BorderThickness = new Thickness(1),
        //             CornerRadius = new CornerRadius(4),
        //             Padding = new Thickness(8),
        //             MaxWidth = 300,
        //         };

        //         var textBlock = new TextBlock()
        //         {
        //             Text = text,
        //             FontWeight = FontWeights.Bold,
        //             Foreground = Brushes.Black,
        //             TextWrapping = TextWrapping.Wrap,
        //         };

        //         new_border.Child = textBlock;
        //         _selectionPopup.Child = new_border;
        //     }

        //     if (_selectionPopup.Child is Border border && border.Child is TextBlock popupText)
        //     {
        //         popupText.Text = $"{text}";
        //     }

        //     _selectionPopup.HorizontalOffset = 50;
        //     _selectionPopup.VerticalOffset = -60;
        //     _selectionPopup.IsOpen = true;
        // }

        private StackPanel CreateNavigationPanel()
        {
            var nextBtn = new Button()
            {
                Content = "Next",
                Width = 100,
                Margin = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            nextBtn.Click += (s, e) =>
            {
                _currentPage++;
                ShowPage();
            };

            var prevBtn = new Button()
            {
                Content = "Back",
                Width = 100,
                Margin = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            prevBtn.Click += (s, e) =>
            {
                _currentPage--;
                ShowPage();
            };

            var panel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            panel.Children.Add(prevBtn);
            panel.Children.Add(nextBtn);

            return panel;
        }

        #region Конкретные наблюдатели

        public class HighlightObserver : ITextSelectionObserver
        {
            public void OnTextSelected(string selectedText, int startPage, int startLine)
            {
                Console.WriteLine(
                    $"Выделен текст: '{selectedText}' на странице {startPage}, строке {startLine}"
                );
            }
        }

        public class TranslationObserver : ITextSelectionObserver
        {
            private Popup _translationPopup;
            private TextBox _parentTextBox;

            public TranslationObserver(TextBox parentTextBox)
            {
                _parentTextBox = parentTextBox;
                InitializeTranslationPopup();
            }

            public async void OnTextSelected(string selectedText, int startPage, int startLine)
            {
                string translation = await GetTranslation(selectedText);
                ShowTranslationPopup(selectedText, translation);
            }

            private async Task<string> GetTranslation(string selectedText)
            {
                var translator = new GoogleTranslator();
                string originalText = selectedText;

                var resultEs = await translator.TranslateAsync(originalText, "es", "ru");
                return resultEs.Translation;

                // var dictionary = new Dictionary<string, string>()
                // {
                //     ["hello"] = "привет",
                //     ["world"] = "мир",
                //     ["book"] = "книга",
                //     ["text"] = "текст",
                //     ["page"] = "страница",
                //     ["read"] = "читать",
                // };

                // string lowerWord = word.ToLower();
                // return dictionary.ContainsKey(lowerWord)
                //     ? dictionary[lowerWord]
                //     : $"Перевод для '{word}' не найден";
            }

            private void ShowTranslationPopup(string original, string translation)
            {
                if (_translationPopup == null)
                    return;

                if (
                    _translationPopup.Child is Border border
                    && border.Child is StackPanel stackPanel
                )
                {
                    var originalText = (TextBlock)stackPanel.Children[0];
                    var translationText = (TextBlock)stackPanel.Children[1];

                    translationText.Text = $"Перевод: {translation}";

                    _translationPopup.IsOpen = true;

                    // Подписываемся на клик по TextBox
                    _parentTextBox.PreviewMouseLeftButtonDown += CloseTranslationPopupOnClick;
                }
            }

            private void CloseTranslationPopupOnClick(object sender, MouseButtonEventArgs e)
            {
                if (_translationPopup != null && _translationPopup.IsOpen)
                {
                    _translationPopup.IsOpen = false;
                    _parentTextBox.PreviewMouseLeftButtonDown -= CloseTranslationPopupOnClick; // Отписываемся
                }
            }

            private void InitializeTranslationPopup()
            {
                _translationPopup = new Popup();

                var border = new Border()
                {
                    Background = Brushes.LightBlue,
                    BorderBrush = Brushes.DarkBlue,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10),
                    MaxWidth = 300,
                };

                var stackPanel = new StackPanel();

                var originalText = new TextBlock()
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkBlue,
                };

                var translationText = new TextBlock()
                {
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.Black,
                    TextWrapping = TextWrapping.Wrap,
                };

                stackPanel.Children.Add(originalText);
                stackPanel.Children.Add(translationText);
                border.Child = stackPanel;
                _translationPopup.Child = border;

                _translationPopup.PlacementTarget = _parentTextBox;
                _translationPopup.Placement = PlacementMode.Relative;
                _translationPopup.StaysOpen = false;
                _translationPopup.HorizontalOffset = 100;
                _translationPopup.VerticalOffset = -80;
            }
        }

        public class BookmarkObserver : ITextSelectionObserver
        {
            public void OnTextSelected(string selectedText, int startPage, int startLine)
            {
                if (selectedText.Length > 10)
                {
                    SaveBookmark(selectedText, startPage, startLine);
                }
            }

            private void SaveBookmark(string text, int page, int line)
            {
                Console.WriteLine(
                    $"Создана закладка: '{text.Substring(0, Math.Min(50, text.Length))}...'"
                );
            }
        }
        #endregion
    }
}

// namespace GUI_FOR_BOOK
// {
//     /// <summary>
//     /// Interaction logic for MainWindow.xaml
//     /// </summary>
//     public partial class MainWindow : Window
//     {
//         private BookViewModel _viewModel;
//         private string[] _lines;
//         private int _currentPage = 0;
//         private TextBox _textBox;
//         private TextBlock _pagination;
//         private int totalPages;
//         private Popup _selectionPopup;

//         public MainWindow()
//         {
//             InitializeComponent();

//             _lines = File.ReadAllLines("Sample1.txt");
//             const int LINES_PER_PAGE = 100;
//             totalPages = (int)Math.Ceiling((double)_lines.Length / (LINES_PER_PAGE));

//             // Создание UI
//             var stack = new StackPanel();

//             _textBox = new TextBox();
//             _textBox.TextWrapping = TextWrapping.Wrap;
//             _textBox.Height = 900;
//             _textBox.FontSize = 20;
//             _textBox.Padding = new Thickness(10); // Отступы внутри
//             _textBox.IsReadOnly = true; // Запрещаем редактирование
//             _textBox.BorderThickness = new Thickness(0); // Убираем границу
//             _textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; // Прокрутк
//             _textBox.Background = Brushes.White; // Белый фон как у TextBlock

//             _textBox.SelectionChanged += TextBox_SelectionChanged;

//             _pagination = new TextBlock()
//             {
//                 // Text = $"{_currentPage} из {totalPages}",
//                 FontSize = 20,
//                 FontWeight = FontWeights.Bold,
//                 HorizontalAlignment = HorizontalAlignment.Center,
//                 Margin = new Thickness(0, 0, 0, 20),
//             };

//             var nextBtn = new Button()
//             {
//                 Content = "Next",
//                 Width = 100,
//                 Margin = new Thickness(10, 5, 10, 5), // Отступы: слева, сверху, справа, снизу
//                 HorizontalAlignment = HorizontalAlignment.Right, // Выравнивание по правому краю
//                 VerticalAlignment = VerticalAlignment.Bottom,
//             };
//             nextBtn.Click += (s, e) =>
//             {
//                 _currentPage++;
//                 ShowPage();
//             };

//             var prevBtn = new Button()
//             {
//                 Content = "Back",
//                 Width = 100,
//                 Margin = new Thickness(10, 5, 10, 5),
//                 HorizontalAlignment = HorizontalAlignment.Left, // Выравнивание по левому краю
//                 VerticalAlignment = VerticalAlignment.Bottom,
//             };
//             prevBtn.Click += (s, e) =>
//             {
//                 _currentPage--;
//                 ShowPage();
//             };
//             // panel to unite buttons
//             var panel = new StackPanel()
//             {
//                 Orientation = Orientation.Horizontal,
//                 HorizontalAlignment = HorizontalAlignment.Center,
//             };
//             panel.Children.Add(prevBtn);
//             panel.Children.Add(nextBtn);

//             stack.Children.Add(_textBox);
//             stack.Children.Add(_pagination);
//             stack.Children.Add(panel);

//             this.Content = stack;
//             ShowPage();
//         }

//         // StringBuilder pageBuilder = new StringBuilder();
//         // private void ShowPage()
//         // {
//         //     int start = _currentPage * 29;
//         //     int end = Math.Min(start + 29, _lines.Length);
//         //     _currentPage = Math.Max(0, Math.Min(_currentPage, (_lines.Length - 1) / 29));

//         //     var text = string.Join("\n", _lines.Skip(start).Take(29));
//         //     _textBlock.Text = $"Страница {_currentPage + 1}\n{text}";
//         // }
//         private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
//         {
//             if (_textBox.SelectionLength > 0)
//             {
//                 string selectedText = _textBox.SelectedText.Trim();

//                 if (!string.IsNullOrEmpty(selectedText) && selectedText.Length > 0)
//                 {
//                     ShowPopupAboveSelection(selectedText);
//                 }
//             }
//             else
//             {
//                 // Скрываем popup когда выделение снято
//                 if (_selectionPopup != null)
//                     _selectionPopup.IsOpen = false;
//             }
//         }

//         private void ShowPopupAboveSelection(string text)
//         {
//             // Создаем popup если его нет
//             if (_selectionPopup == null)
//             {
//                 _selectionPopup = new Popup();
//                 _selectionPopup.Placement = PlacementMode.Relative;
//                 _selectionPopup.PlacementTarget = _textBox;

//                 var new_border = new Border()
//                 {
//                     Background = Brushes.LightYellow,
//                     BorderBrush = Brushes.Gray,
//                     BorderThickness = new Thickness(1),
//                     CornerRadius = new CornerRadius(4),
//                     Padding = new Thickness(8),
//                     MaxWidth = 300,
//                 };

//                 var textBlock = new TextBlock()
//                 {
//                     Text = text,
//                     FontWeight = FontWeights.Bold,
//                     Foreground = Brushes.Black,
//                     TextWrapping = TextWrapping.Wrap,
//                 };

//                 new_border.Child = textBlock;
//                 _selectionPopup.Child = new_border;
//             }

//             // Обновляем текст
//             if (_selectionPopup.Child is Border border && border.Child is TextBlock popupText)
//             {
//                 popupText.Text = $"{text}";
//             }

//             // ПРОСТАЯ ПОЗИЦИЯ - в центре над TextBox
//             _selectionPopup.HorizontalOffset = 50;
//             _selectionPopup.VerticalOffset = -60;
//             _selectionPopup.IsOpen = true;
//         }

//         private void ShowPage()
//         {
//             // const int INFO_LINES = 16; // 2 отступа + 2 строки информации
//             const int LINES_PER_PAGE = 29;

//             // КОРРЕКТИРУЕМ текущую страницу (на случай если вышли за границы)
//             _currentPage = Math.Max(0, Math.Min(_currentPage, totalPages - 1));

//             int start_index = _currentPage * LINES_PER_PAGE;
//             int end_index = Math.Min(start_index + LINES_PER_PAGE, _lines.Length);

//             // Проверяем, не выходим ли мы за пределы с учетом информации о странице
//             if (end_index - start_index > LINES_PER_PAGE)
//             {
//                 end_index = start_index + (LINES_PER_PAGE);
//             }

//             StringBuilder stringBuilder = new StringBuilder();

//             for (int i = start_index; i < end_index; i++)
//             {
//                 stringBuilder.AppendLine(_lines[i]);
//             }

//             // Гарантированно добавляем информацию о странице
//             // stringBuilder.AppendLine();
//             // stringBuilder.AppendLine();
//             // stringBuilder.AppendLine(
//             //     $"{new string(' ', 150)}Страница {_currentPage + 1} из {totalPages}"
//             // );
//             // stringBuilder.AppendLine(new string('=', 40));

//             _textBox.Text = stringBuilder.ToString();

//             _pagination.Text = $"Страница {_currentPage + 1} из {totalPages}";
//         }
//     }
// }
// // КНИГА: [_lines] = 87 строк
// // [0-28] [29-57] [58-86]  ← Страницы
// //   0       1       2      ← _currentPage

// // ПОЛЬЗОВАТЕЛЬ НАЖИМАЕТ "ВПЕРЕД" → _currentPage = 1

// // ShowPage():
// // 1. start_index = 1 * 29 = 29  ← "Палец №1 на строке 29"
// // 2. end_index = min(29+29, 87) = 58  ← "Палец №2 на строке 58"
// // 3. totalPages = ceil(87/29) = 3
// // 4. Пишем: "Страница 2 из 3"
// // 5. Копируем строки: 29, 30, 31, ..., 57
// // 6. Показываем пользователю

// // // Загружаем текст из файла прямо в TextBox
// // try
// // {
// //     string filePath = "Sample1.txt"; // Файл рядом с .exe
// //     if (File.Exists(filePath))
// //     {
// //         string fileContent = File.ReadAllText(filePath);
// //         string[] lines = fileContent.Split('\n'); // Разделяем по переносам строк
// //         for (int i = 0; i < 29; i++)
// //         {
// //             pageBuilder.AppendLine(lines[i]);
// //         }
// //         _textBlock.Text = pageBuilder.ToString();
// //     }
// //     else
// //     {
// //         _textBlock.Text = "Файл Sample.txt не найден!";
// //     }
// // }
// // catch (Exception ex)
// // {
// //     _textBlock.Text = $"Ошибка: {ex.Message}";
// // }

// // this.Content = textBlock;

// // // Создаем ViewModel и устанавливаем DataContext
// // _viewModel = new BookViewModel();
// // this.DataContext = _viewModel;

// // // Загружаем книгу при старте
// // _viewModel.LoadBook("Sample.txt");

// // Создаем WPF элементы
// // TextBox textBox1 = new TextBox();
// // Label label1 = new Label();

// // // Настраиваем layout
// // var stackPanel = new StackPanel();
// // stackPanel.Margin = new Thickness(30);

// // textBox1.Height = 40;
// // textBox1.Margin = new Thickness(10);

// // // Привязка данных в WPF
// // Binding binding = new Binding("Text");
// // binding.Source = textBox1;
// // label1.SetBinding(Label.ContentProperty, binding);
// // label1.FontWeight = FontWeights.Bold;

// // // Добавляем элементы
// // stackPanel.Children.Add(textBox1);
// // stackPanel.Children.Add(label1);

// // // Устанавливаем содержимое окна
// // this.Content = stackPanel;

// // Создаем TextBox
// // TextBox textBox1 = new TextBox();
// // textBox1.IsReadOnly = true; // Запрет редактирования
// // textBox1.Height = 800;
// // textBox1.Width = 1200;
// // textBox1.TextWrapping = TextWrapping.Wrap;
// // // textBox1.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
// // textBox1.Margin = new Thickness(20);
// // textBox1.FontSize = 18;
// // StringBuilder pageBuilder = new StringBuilder();

// // // Загружаем текст из файла прямо в TextBox
// // try
// // {
// //     string filePath = "Sample1.txt"; // Файл рядом с .exe
// //     if (File.Exists(filePath))
// //     {
// //         string fileContent = File.ReadAllText(filePath);
// //         string[] lines = fileContent.Split('\n'); // Разделяем по переносам строк
// //         for (int i = 0; i < 29; i++)
// //         {
// //             pageBuilder.AppendLine(lines[i]);
// //         }
// //         textBox1.Text = pageBuilder.ToString();
// //     }
// //     else
// //     {
// //         textBox1.Text = "Файл Sample.txt не найден!";
// //     }
// // }
// // catch (Exception ex)
// // {
// //     textBox1.Text = $"Ошибка: {ex.Message}";
// // }

// // // Устанавливаем TextBox как содержимое окна
// // this.Content = textBox1;

// // Создаем TextBlock (текст нельзя прокручивать)
