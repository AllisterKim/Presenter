using System;
using System.Runtime.InteropServices;

namespace Presenter
{
    /// <summary>
    /// 오버레이 창의 "클릭 통과(Click-through)" 처리와 우클릭 감지를 위한 최소한의 Win32 P/Invoke 래퍼.
    /// - 레이저 포인터 모드: 클릭 통과 ON (마우스 클릭이 하위 앱, 예: PPT로 그대로 전달되어야 함)
    /// - 그리기 모드: 클릭 통과 OFF (오버레이가 클릭을 받아서 그림을 그려야 함)
    /// </summary>
    internal static class NativeMethods
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int VK_RBUTTON = 0x02;

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // 클릭 통과(포인터) 모드에서는 창이 마우스 클릭 이벤트를 전혀 받지 못하므로,
        // 우클릭으로 모드를 해제하려면 전역 키 상태를 직접 폴링해야 한다.
        public static bool IsRightMouseButtonDown()
        {
            return (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
        }

        public static void SetClickThrough(IntPtr hwnd, bool clickThrough)
        {
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            if (clickThrough)
            {
                exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            }
            else
            {
                exStyle &= ~WS_EX_TRANSPARENT;
                // WS_EX_LAYERED는 AllowsTransparency="True" WPF 창에서 이미 설정되어 있으므로 유지.
            }

            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

            // 일부 환경에서는 GWL_EXSTYLE 변경만으로 클릭 통과 여부가 즉시 반영되지 않아,
            // SWP_FRAMECHANGED로 창에 스타일 변경을 명시적으로 다시 알려준다.
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }
    }
}
