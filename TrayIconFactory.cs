using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Presenter
{
    // 트레이 아이콘을 마스코트 얼굴 모양으로 보이게 하기 위해, WPF로 그린 얼굴 Visual을
    // 오프스크린으로 렌더링해 System.Drawing.Icon(트레이가 요구하는 타입)으로 변환한다.
    internal static class TrayIconFactory
    {
        public static System.Drawing.Icon CreateMascotFaceIcon(int pixelSize = 64)
        {
            UIElement visual = MascotWindow.BuildTrayIconFaceVisual(pixelSize);
            visual.Measure(new Size(pixelSize, pixelSize));
            visual.Arrange(new Rect(0, 0, pixelSize, pixelSize));
            visual.UpdateLayout();

            var renderBitmap = new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using var pngStream = new MemoryStream();
            encoder.Save(pngStream);
            pngStream.Position = 0;

            using var bitmap = new System.Drawing.Bitmap(pngStream);
            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }

        // 일회성 유틸리티: 앱 실행 파일에 임베드할 다중 해상도 .ico 자산을 생성한다.
        // (Presenter.csproj의 ApplicationIcon 및 설치 프로그램 바로가기 아이콘용)
        //
        // System.Drawing.Icon.Save()는 내부적으로 16색 레거시 비트맵으로 낮춰버려 알파(투명)가
        // 깨지므로 쓰지 않는다. 대신 각 크기를 32bpp BGRA 원본 픽셀 그대로 표준 ICO의
        // "BMP-in-ICO" 프레임(BITMAPINFOHEADER + XOR + AND 마스크)으로 직접 조립한다.
        public static void GenerateAppIconFile(string path, int[] sizes)
        {
            var images = new System.Collections.Generic.List<byte[]>();
            foreach (int size in sizes)
            {
                images.Add(CreateRawBmpFrame(size));
            }

            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fileStream);

            writer.Write((short)0); // reserved
            writer.Write((short)1); // type: icon
            writer.Write((short)sizes.Length);

            int offset = 6 + sizes.Length * 16;
            for (int i = 0; i < sizes.Length; i++)
            {
                int size = sizes[i];
                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)0); // color count (0 = 256+ colors)
                writer.Write((byte)0); // reserved
                writer.Write((short)1); // planes
                writer.Write((short)32); // bits per pixel
                writer.Write(images[i].Length);
                writer.Write(offset);
                offset += images[i].Length;
            }

            foreach (byte[] image in images)
            {
                writer.Write(image);
            }
        }

        // 지정한 크기로 얼굴을 렌더링해, ICO 안에 들어가는 고전 방식의 32bpp BMP 프레임
        // (BITMAPINFOHEADER + 위->아래가 아니라 아래->위 순서의 BGRA 픽셀 + AND 마스크)을 만든다.
        private static byte[] CreateRawBmpFrame(int size)
        {
            UIElement visual = MascotWindow.BuildTrayIconFaceVisual(size);
            visual.Measure(new Size(size, size));
            visual.Arrange(new Rect(0, 0, size, size));
            visual.UpdateLayout();

            var renderBitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);

            int stride = size * 4;
            byte[] pixels = new byte[stride * size];
            renderBitmap.CopyPixels(pixels, stride, 0);

            int andMaskStride = ((size + 31) / 32) * 4; // AND 마스크는 32비트 경계로 행 패딩
            int pixelDataSize = stride * size;
            int andMaskSize = andMaskStride * size;

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(40);           // biSize
            writer.Write(size);         // biWidth
            writer.Write(size * 2);     // biHeight (XOR+AND 두 장 몫이라 실제 높이의 2배)
            writer.Write((short)1);     // biPlanes
            writer.Write((short)32);    // biBitCount
            writer.Write(0);            // biCompression = BI_RGB
            writer.Write(pixelDataSize + andMaskSize); // biSizeImage
            writer.Write(0);            // biXPelsPerMeter
            writer.Write(0);            // biYPelsPerMeter
            writer.Write(0);            // biClrUsed
            writer.Write(0);            // biClrImportant

            // XOR(색상) 데이터: WPF는 위->아래로 주지만 BMP는 아래->위 순서로 저장해야 한다.
            // Pbgra32는 이미 straight BGRA와 픽셀 바이트 순서가 같아 그대로 복사하면 된다.
            for (int row = size - 1; row >= 0; row--)
            {
                writer.Write(pixels, row * stride, stride);
            }

            // AND 마스크: 32bpp는 알파 채널이 투명도를 담당하므로 전부 0(불투명 취급)으로 채운다.
            writer.Write(new byte[andMaskSize]);

            return ms.ToArray();
        }
    }
}
