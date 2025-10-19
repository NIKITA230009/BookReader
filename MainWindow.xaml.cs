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

        public MainWindow()
        {
            InitializeComponent();
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
            TextBox textBox1 = new TextBox();
            textBox1.IsReadOnly = true; // Запрет редактирования
            textBox1.Height = 800;
            textBox1.Width = 1200;
            textBox1.TextWrapping = TextWrapping.Wrap;
            textBox1.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            textBox1.Margin = new Thickness(20);
            textBox1.FontSize = 18;

            // Загружаем текст из файла прямо в TextBox
            try
            {
                string filePath = "Sample1.txt"; // Файл рядом с .exe
                if (File.Exists(filePath))
                {
                    string fileContent = File.ReadAllText(filePath);
                    textBox1.Text = fileContent;
                }
                else
                {
                    textBox1.Text = "Файл Sample.txt не найден!";
                }
            }
            catch (Exception ex)
            {
                textBox1.Text = $"Ошибка: {ex.Message}";
            }

            // Устанавливаем TextBox как содержимое окна
            this.Content = textBox1;
        }
    }
}
