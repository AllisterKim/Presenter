using System.Windows;
using WinForms = System.Windows.Forms;

namespace Presenter
{
    public partial class App : Application
    {
        private WinForms.NotifyIcon? _trayIcon;
        private System.Drawing.Icon? _trayIconImage;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var showItem = new WinForms.ToolStripMenuItem("보이기");
            showItem.Click += (_, _) => ShowMainWindow();

            var exitItem = new WinForms.ToolStripMenuItem("종료");
            exitItem.Click += (_, _) => TryExit();

            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(new WinForms.ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            // 마스코트 얼굴을 오프스크린으로 렌더링해 트레이 아이콘으로 사용한다.
            // 렌더링에 실패하는 예외적인 환경(그래픽 드라이버 문제 등)에서는 기본 아이콘으로 대체한다.
            try
            {
                _trayIconImage = TrayIconFactory.CreateMascotFaceIcon();
            }
            catch
            {
                _trayIconImage = null;
            }

            _trayIcon = new WinForms.NotifyIcon
            {
                Icon = _trayIconImage ?? System.Drawing.SystemIcons.Application,
                Text = "Presenter",
                Visible = true,
                ContextMenuStrip = contextMenu
            };

            // 마스코트를 "숨기기"로 감춰도 트레이 아이콘은 남아있는데, 더블클릭으로도
            // 다시 꺼낼 수 있게 한다(우클릭 메뉴의 "보이기"와 동일 동작).
            _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
        }

        // 마스코트 메뉴의 "숨기기"로 감춰진 창을 다시 화면에 띄운다.
        private static void ShowMainWindow()
        {
            if (Current.MainWindow is Window mainWindow)
            {
                mainWindow.Show();
                mainWindow.Activate();
            }
        }

        private void TryExit()
        {
            if (ConfirmExit())
            {
                Shutdown();
            }
        }

        // 마스코트 메뉴/트레이 메뉴 양쪽에서 공유하는 종료 확인 팝업.
        public static bool ConfirmExit()
        {
            return MessageBox.Show(
                "Presenter를 종료하시겠습니까?",
                "종료 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _trayIcon = null;

            _trayIconImage?.Dispose();
            _trayIconImage = null;

            base.OnExit(e);
        }

        // TODO(v1.1): 전역 예외 핸들러, 설정 로드 등을 여기에 추가.
    }
}
