using System.Windows;
using System.Windows.Input;

namespace Presenter
{
    public partial class MagnifierWindow : Window
    {
        public MagnifierWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
