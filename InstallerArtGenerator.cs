using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Presenter
{
    // 일회성 유틸리티: WiX 설치 마법사 화면(WixUIDialogBmp/WixUIBannerBmp)에 쓸
    // 마스코트 그림을 오프스크린으로 렌더링해 BMP로 저장한다.
    internal static class InstallerArtGenerator
    {
        // WixUI_Minimal의 시작/완료 화면에 쓰이는 큰 그림. 공식 권장 크기는 493x312.
        public static void GenerateDialogBmp(string path)
        {
            UIElement mascot = MascotWindow.BuildFaceVisual(blink: false, mouthOpen: false);
            var viewbox = new Viewbox
            {
                Child = mascot,
                Width = 150,
                Height = 260,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(30, 0, 0, 0)
            };

            var canvas = new Grid
            {
                Width = 493,
                Height = 312,
                Background = Brushes.White
            };
            canvas.Children.Add(viewbox);

            SaveVisualAsBmp(canvas, 493, 312, path);
        }

        // WixUI_Minimal의 진행 화면 등 상단에 쓰이는 배너. 공식 권장 크기는 493x58.
        public static void GenerateBannerBmp(string path)
        {
            UIElement head = MascotWindow.BuildTrayIconFaceVisual(46);
            var headHost = new Grid
            {
                Width = 46,
                Height = 46,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            };
            headHost.Children.Add(head);

            var canvas = new Grid
            {
                Width = 493,
                Height = 58,
                Background = Brushes.White
            };
            canvas.Children.Add(headHost);

            SaveVisualAsBmp(canvas, 493, 58, path);
        }

        private static void SaveVisualAsBmp(UIElement visual, int width, int height, string path)
        {
            visual.Measure(new Size(width, height));
            visual.Arrange(new Rect(0, 0, width, height));
            visual.UpdateLayout();

            var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);

            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            encoder.Save(stream);
        }
    }
}
