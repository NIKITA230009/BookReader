using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GUI_FOR_BOOK
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BookViewModel _viewModel;
        private string[] _lines;
        private int _currentPage = 0;
        private TextBlock _textBlock;

        public MainWindow()
        {
            InitializeComponent();

            _lines = File.ReadAllLines("Sample1.txt");

            // Создание UI
            var stack = new StackPanel();

            // _textBlock = new TextBlock();
            // _textBlock.Text = "Этот текст неподвижен";
            // _textBlock.FontSize = 20;
            // _textBlock.Foreground = Brushes.Black;
            // _textBlock.TextWrapping = TextWrapping.Wrap;
            // _textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            // _textBlock.VerticalAlignment = VerticalAlignment.Center;
            // _textBlock.Margin = new Thickness(40);

            _textBlock = new TextBlock();
            _textBlock.TextWrapping = TextWrapping.Wrap;
            _textBlock.Height = 800;
            _textBlock.FontSize = 20;

            var nextBtn = new Button() { Content = "Next", Width = 100 };
            nextBtn.Click += (s, e) =>
            {
                _currentPage++;
                ShowPage();
            };

            var prevBtn = new Button() { Content = "Back", Width = 100 };
            prevBtn.Click += (s, e) =>
            {
                _currentPage--;
                ShowPage();
            };
            // panel to unite buttons
            var panel = new StackPanel() { Orientation = Orientation.Horizontal };
            panel.Children.Add(prevBtn);
            panel.Children.Add(nextBtn);

            stack.Children.Add(_textBlock);
            stack.Children.Add(panel);

            this.Content = stack;
            ShowPage();
        }

        // StringBuilder pageBuilder = new StringBuilder();
        // private void ShowPage()
        // {
        //     int start = _currentPage * 29;
        //     int end = Math.Min(start + 29, _lines.Length);
        //     _currentPage = Math.Max(0, Math.Min(_currentPage, (_lines.Length - 1) / 29));

        //     var text = string.Join("\n", _lines.Skip(start).Take(29));
        //     _textBlock.Text = $"Страница {_currentPage + 1}\n{text}";
        // }

        private void ShowPage()
        {
            const int LINES_PER_PAGE = 29; // Сколько строк на одной странице

            int start_index = _currentPage * LINES_PER_PAGE;
            int end_index = Math.Min(start_index + LINES_PER_PAGE, _lines.Length);

            // 2. Вычисляем общее количество страниц
            int totalPages = (int)Math.Ceiling((double)_lines.Length / LINES_PER_PAGE);

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Страница {_currentPage + 1} из {totalPages}");
            stringBuilder.AppendLine(new string('=', 40));

            // 3. Берем только строки для ТЕКУЩЕЙ страницы
            for (int i = start_index; i < end_index; i++)
            {
                stringBuilder.AppendLine(_lines[i]);
            }

            // 4. Обновляем интерфейс
            _textBlock.Text = stringBuilder.ToString();
        }
    }
}
// КНИГА: [_lines] = 87 строк
// [0-28] [29-57] [58-86]  ← Страницы
//   0       1       2      ← _currentPage

// ПОЛЬЗОВАТЕЛЬ НАЖИМАЕТ "ВПЕРЕД" → _currentPage = 1

// ShowPage():
// 1. start_index = 1 * 29 = 29  ← "Палец №1 на строке 29"
// 2. end_index = min(29+29, 87) = 58  ← "Палец №2 на строке 58"
// 3. totalPages = ceil(87/29) = 3
// 4. Пишем: "Страница 2 из 3"
// 5. Копируем строки: 29, 30, 31, ..., 57
// 6. Показываем пользователю

// // Загружаем текст из файла прямо в TextBox
// try
// {
//     string filePath = "Sample1.txt"; // Файл рядом с .exe
//     if (File.Exists(filePath))
//     {
//         string fileContent = File.ReadAllText(filePath);
//         string[] lines = fileContent.Split('\n'); // Разделяем по переносам строк
//         for (int i = 0; i < 29; i++)
//         {
//             pageBuilder.AppendLine(lines[i]);
//         }
//         _textBlock.Text = pageBuilder.ToString();
//     }
//     else
//     {
//         _textBlock.Text = "Файл Sample.txt не найден!";
//     }
// }
// catch (Exception ex)
// {
//     _textBlock.Text = $"Ошибка: {ex.Message}";
// }

// this.Content = textBlock;

// // Создаем ViewModel и устанавливаем DataContext
// _viewModel = new BookViewModel();
// this.DataContext = _viewModel;

// // Загружаем книгу при старте
// _viewModel.LoadBook("Sample.txt");

// Создаем WPF элементы
// TextBox textBox1 = new TextBox();
// Label label1 = new Label();

// // Настраиваем layout
// var stackPanel = new StackPanel();
// stackPanel.Margin = new Thickness(30);

// textBox1.Height = 40;
// textBox1.Margin = new Thickness(10);

// // Привязка данных в WPF
// Binding binding = new Binding("Text");
// binding.Source = textBox1;
// label1.SetBinding(Label.ContentProperty, binding);
// label1.FontWeight = FontWeights.Bold;

// // Добавляем элементы
// stackPanel.Children.Add(textBox1);
// stackPanel.Children.Add(label1);

// // Устанавливаем содержимое окна
// this.Content = stackPanel;

// Создаем TextBox
// TextBox textBox1 = new TextBox();
// textBox1.IsReadOnly = true; // Запрет редактирования
// textBox1.Height = 800;
// textBox1.Width = 1200;
// textBox1.TextWrapping = TextWrapping.Wrap;
// // textBox1.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
// textBox1.Margin = new Thickness(20);
// textBox1.FontSize = 18;
// StringBuilder pageBuilder = new StringBuilder();

// // Загружаем текст из файла прямо в TextBox
// try
// {
//     string filePath = "Sample1.txt"; // Файл рядом с .exe
//     if (File.Exists(filePath))
//     {
//         string fileContent = File.ReadAllText(filePath);
//         string[] lines = fileContent.Split('\n'); // Разделяем по переносам строк
//         for (int i = 0; i < 29; i++)
//         {
//             pageBuilder.AppendLine(lines[i]);
//         }
//         textBox1.Text = pageBuilder.ToString();
//     }
//     else
//     {
//         textBox1.Text = "Файл Sample.txt не найден!";
//     }
// }
// catch (Exception ex)
// {
//     textBox1.Text = $"Ошибка: {ex.Message}";
// }

// // Устанавливаем TextBox как содержимое окна
// this.Content = textBox1;

// Создаем TextBlock (текст нельзя прокручивать)
