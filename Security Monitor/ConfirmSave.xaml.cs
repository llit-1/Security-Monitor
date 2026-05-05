using System.Windows;
using System.Windows.Input;

namespace Security_Monitor
{
    public partial class ConfirmSave : Window
    {
        public bool Result { get; private set; } = false;

        public ConfirmSave()
        {
            InitializeComponent();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Result = false;
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                Result = true;
                Close();
            }
        }
    }
}