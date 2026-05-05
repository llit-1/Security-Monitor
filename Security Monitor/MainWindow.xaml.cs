using Security_Monitor.Services;
using Security_Monitor.ViewModels;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Security_Monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly HttpServer _server;
        private bool _isFullScreen = false;
        private EmployeeEdit _employeeEditWindow;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;
            this.Focusable = true;
            this.Focus();
            _server = new HttpServer();
            _server.OnIdReceived += async (id, reader, time, status, code) =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    if (_employeeEditWindow != null && reader == "СХЗ Проходная - Вход")
                    {
                        _employeeEditWindow.InitializePerson(code, time);
                    }
                    _vm.LoadPerson(id, reader, time);
                    await ShowAndHide(AccessBorder, 1, status);                    
                });
            };
            _server.Start();
        }



        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.WindowState = WindowState.Minimized;
                return;
            }

            if (e.Key == Key.Enter)
            {
                OpenEmployeeEditWindow();
            }
        }


        private void OpenEmployeeEditWindow()
        {
            // Если окно уже открыто – ничего не делаем
            if (_employeeEditWindow != null)
                return;

            _employeeEditWindow = new EmployeeEdit();
            _employeeEditWindow.Owner = this;
            _employeeEditWindow.Closed += (s, e) =>
            {
                _employeeEditWindow = null;
                this.Focus(); // Возвращаем фокус на главное окно
            };

            _employeeEditWindow.Show();
            _employeeEditWindow.Focus();
        }


        public async Task ShowAndHide(Border border, int miliseconds, int status)
        {
            if (border.Child is TextBlock tb)
            {
                switch (status)
                {
                    case 1:

                        tb.Text = "Проход разрешен";
                        border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8F, 0xAE, 0x29));
                        break;
                    case 2:
                            tb.Text = "Проход запрещен";
                        border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE3, 0x26, 0x36));
                        break;
                    case 3:
                        tb.Text = "Карта не зарегестрирована";
                        border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE3, 0x54, 0x26));
                        break;
                }
            }
            // Меняем цвет
            

            // Получаем сториборды
            var show = (Storyboard)this.FindResource("ShowStoryboard");
            var hide = (Storyboard)this.FindResource("HideStoryboard");

            Storyboard.SetTarget(show, border);
            Storyboard.SetTarget(hide, border);

            // Показ
            show.Begin();
            await Task.Delay(1000);

            // Пауза
            await Task.Delay(miliseconds);

            // Скрытие
            hide.Begin();
            await Task.Delay(1000);
        }
    }

}