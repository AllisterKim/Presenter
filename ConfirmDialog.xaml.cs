using System.Windows;

namespace Presenter
{
    // 표준 MessageBox 대신 앱의 다른 창들과 같은 어두운 라운드 스타일로 통일한 확인 팝업.
    public partial class ConfirmDialog : Window
    {
        public bool Result { get; private set; }

        private ConfirmDialog(string message, string confirmText, string cancelText)
        {
            InitializeComponent();
            MessageText.Text = message;
            ConfirmButton.Content = confirmText;
            CancelButton.Content = cancelText;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        public static bool Show(string message, string confirmText = "확인", string cancelText = "취소")
        {
            var dialog = new ConfirmDialog(message, confirmText, cancelText);
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}
