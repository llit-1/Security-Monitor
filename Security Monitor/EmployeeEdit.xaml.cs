using Security_Monitor.Models;
using Security_Monitor.Services;
using Security_Monitor.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Security_Monitor
{
    public partial class EmployeeEdit : Window
    {
        private List<string> _allPassports = new();
        private bool _isButtonClick; // Флаг, что клик был по кнопке
        private bool _isBackspacePressed; // Флаг для отслеживания нажатия Backspace

        public EmployeeEdit()
        {
            InitializeComponent();

            this.PreviewMouseDown += (s, e) =>
            {
                if (!popupResults.IsOpen)
                    return;

                // Если клик по кнопке - не закрываем (дадим кнопке обработать)
                if (e.OriginalSource is DependencyObject source)
                {
                    var btn = FindParent<Button>(source);
                    if (btn == btnDropDown)
                        return;
                }

                if (!searchContainer.IsMouseOver && !popupResults.IsMouseOver)
                {
                    popupResults.IsOpen = false;
                }
            };

            Loaded += (s, e) =>
            {
                if (DataContext is EmployeeEditViewModel vm)
                {
                    _allPassports = vm.Passports.ToList();
                }

                // Устанавливаем фокус и курсор в поле ввода
                txtSearch.Focus();
                txtSearch.CaretIndex = txtSearch.Text.Length;
            };

            txtSearch.TextChanged += txtSearch_TextChanged;

            // Отслеживаем нажатие клавиши Backspace
            txtSearch.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Back)
                {
                    _isBackspacePressed = true;
                }
                else
                {
                    _isBackspacePressed = false;
                }
            };

            // Блокируем автозакрытие при клике на кнопку
            btnDropDown.PreviewMouseDown += (s, e) =>
            {
                _isButtonClick = true;
            };
        }

        // Вспомогательный метод для поиска родительского элемента
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }

            if (e.Key == Key.Enter)
            {
                if (popupResults.IsOpen && lstResults.SelectedItem is string selected)
                {
                    txtSearch.Text = selected;
                    if (DataContext is EmployeeEditViewModel vm)
                        vm.SelectedPassport = selected;
                    popupResults.IsOpen = false;
                    return;
                }

                var confirm = new ConfirmSave { Owner = this };
                confirm.ShowDialog();

                if (confirm.Result)
                {
                    await SaveChanges();
                }
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = txtSearch.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                popupResults.IsOpen = false;
                // Сбрасываем выбранный паспорт, если поле пустое
                if (DataContext is EmployeeEditViewModel vm)
                {
                    vm.SelectedPassport = null;
                }
                _isBackspacePressed = false;
                return;
            }

            var results = _allPassports
                .Where(x => x.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            // Если найден ровно один результат И НЕ нажат Backspace
            if (results.Count() == 1 && !_isBackspacePressed)
            {
                if (DataContext is EmployeeEditViewModel vm)
                {
                    // Проверяем, отличается ли текущий текст от найденного
                    if (txtSearch.Text != results[0])
                    {
                        vm.SelectedPassport = results[0];

                        // Сохраняем позицию курсора
                        int oldCaretIndex = txtSearch.CaretIndex;

                        // Обновляем текст
                        txtSearch.Text = results[0];

                        // Восстанавливаем курсор в конец текста
                        txtSearch.CaretIndex = txtSearch.Text.Length;
                    }
                }
                popupResults.IsOpen = false;
                _isBackspacePressed = false;
                return;
            }

            // Если нажат Backspace - открываем popup со всеми подходящими результатами
            if (_isBackspacePressed)
            {
                lstResults.ItemsSource = results;
                if (results.Any())
                {
                    popupResults.IsOpen = true;
                }
                else
                {
                    popupResults.IsOpen = false;
                }
                _isBackspacePressed = false;
                return;
            }

            // Обычная логика для других случаев
            lstResults.ItemsSource = results;

            // Открываем только если есть результаты и не было клика по кнопке
            if (results.Any() && !_isButtonClick)
            {
                popupResults.IsOpen = true;
            }
            else if (!results.Any())
            {
                popupResults.IsOpen = false;
            }

            _isButtonClick = false;
        }

        private void lstResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstResults.SelectedItem is string selected)
            {
                txtSearch.Text = selected;
                if (DataContext is EmployeeEditViewModel vm)
                    vm.SelectedPassport = selected;
                popupResults.IsOpen = false;
            }
        }

        private void btnDropDown_Click(object sender, RoutedEventArgs e)
        {
            if (_allPassports == null || !_allPassports.Any())
                return;

            // Переключаем состояние
            if (popupResults.IsOpen)
            {
                popupResults.IsOpen = false;
            }
            else
            {
                lstResults.ItemsSource = _allPassports;
                searchContainer.UpdateLayout();
                popupResults.PlacementTarget = searchContainer;
                popupResults.Width = searchContainer.ActualWidth;
                popupResults.IsOpen = true;
            }

            _isButtonClick = true;
            e.Handled = true;
        }

        public async void InitializePerson(string code, string time)
        {
            if (DataContext is EmployeeEditViewModel vm)
            {
                vm.PassCardNumber = code;
                DateTime dt = DateTime.Today.Add(TimeSpan.Parse(time));
                var nx = new NxApiService();

                BitmapSource source = await nx.GetImageAsync(dt, "2da4a772-4c24-ed11-a888-9bd9c6760d9a");

                if (source != null)
                {
                    int cropWidth = Math.Min(1080, source.PixelWidth);
                    var cropped = new CroppedBitmap(source, new Int32Rect(0, 0, cropWidth, source.PixelHeight));
                    vm.Photo = cropped;
                }
            }
        }

        private void UpdateImageClip()
        {
            if (EmployeePhoto.ActualWidth <= 0 || EmployeePhoto.ActualHeight <= 0)
                return;

            double W = EmployeePhoto.ActualWidth;
            double H = EmployeePhoto.ActualHeight;
            double targetW = 1233;
            double targetH = 920;
            double x = (W - targetW) / 2;
            double y = (H - targetH) / 2;

            if (ImageClip != null)
                ImageClip.Rect = new Rect(x, y, targetW, targetH);
        }

        private void EmployeePhoto_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateImageClip();
        }

        private void EmployeePhoto_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateImageClip();
        }

        private async Task SaveChanges()
        {
            if (DataContext is not EmployeeEditViewModel vm)
                return;

            using var db = new RKNETContext();

            var person = db.FactoryPerson.FirstOrDefault(x => x.Passport == vm.SelectedPassport);

            if (person == null)
            {
                MessageBox.Show("Ошибка: сотрудник не найден!");
                return;
            }

            person.PassCardNumber = vm.PassCardNumber;

            if (vm.Photo is BitmapSource bmp)
            {
                using (var ms = new MemoryStream())
                {
                    BitmapEncoder enc = new JpegBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bmp));
                    enc.Save(ms);
                    person.Photo = "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray());
                }
            }
            await db.SaveChangesAsync();

            MessageBox.Show("Сохранено успешно!");
        }
    }
}