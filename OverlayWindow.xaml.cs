using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace Presenter
{
    public enum OverlayMode
    {
        None,
        Pointer, // 레이저 포인터 (Day-1)
        Draw     // 그리기 (Day-1)
    }

    public enum DrawTool
    {
        Freehand,
        Line,
        Rectangle,
        Square,
        Ellipse
    }

    public partial class OverlayWindow : Window
    {
        private OverlayMode _mode = OverlayMode.None;
        private Polyline? _currentStroke;
        private Shape? _currentShape;
        private Point _shapeStartPoint;

        private Brush _drawBrush = Brushes.Red;
        private double _drawThickness = 3;
        private DrawTool _drawTool = DrawTool.Freehand;

        // AllowsTransparency 창은 픽셀 알파값이 0인 영역은 마우스 히트테스트 자체가 되지 않는다
        // (WS_EX_TRANSPARENT와는 별개의 동작). 완전 투명(Transparent, alpha=0)이면 그리기 모드에서도
        // 클릭이 하위 창으로 그대로 통과해버리므로, 사실상 안 보이는 수준(alpha=1)의 브러시로
        // 캔버스 전체를 "클릭 가능"하게 만든다.
        private static readonly Brush InteractiveBackground = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

        // 포인터 모드에서는 클릭 통과(WS_EX_TRANSPARENT)로 인해 창이 마우스 이벤트를
        // 전혀 받지 못하므로, WPF MouseMove 대신 전역 커서 위치를 직접 폴링한다.
        private readonly DispatcherTimer _pointerTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(15)
        };

        public OverlayWindow()
        {
            InitializeComponent();
            _pointerTimer.Tick += PointerTimer_Tick;

            // WindowState=Maximized는 창이 시작된 모니터 하나만 덮으므로, 듀얼 모니터에서도
            // 포인터/그리기가 모두 동작하도록 가상 데스크톱(모든 모니터를 합친 영역) 전체를 덮는다.
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        public void EnterPointerMode()
        {
            _mode = OverlayMode.Pointer;

            LaserDot.Visibility = Visibility.Visible;
            Cursor = Cursors.Arrow;
            DrawCanvas.Background = Brushes.Transparent;

            // 클릭은 하위 애플리케이션(PPT 등)으로 그대로 전달되어야 하므로 클릭 통과 ON
            SetClickThrough(true);

            DetachDrawHandlers();
            UpdateLaserPosition();
            _pointerTimer.Start();
        }

        // 색상/두께 프리셋의 알파값(채우기 30%, 테두리 70%)은 그대로 유지하고
        // 색상(RGB)과 지름만 바꾼다.
        public void ApplyPointerAppearance(Color baseColor, double diameter)
        {
            LaserDot.Width = diameter;
            LaserDot.Height = diameter;
            LaserDot.Fill = new SolidColorBrush(Color.FromArgb(0x4D, baseColor.R, baseColor.G, baseColor.B));
            LaserDot.Stroke = new SolidColorBrush(Color.FromArgb(0xB3, baseColor.R, baseColor.G, baseColor.B));

            if (LaserDot.Effect is DropShadowEffect glow)
            {
                glow.Color = baseColor;
            }
        }

        public void EnterDrawMode()
        {
            _mode = OverlayMode.Draw;

            LaserDot.Visibility = Visibility.Collapsed;
            Cursor = Cursors.Pen;
            DrawCanvas.Background = InteractiveBackground;

            // 그리기는 오버레이가 클릭을 직접 받아야 하므로 클릭 통과 OFF
            SetClickThrough(false);

            _pointerTimer.Stop();
            AttachDrawHandlers();
        }

        // 이미 그려진 도형에는 소급 적용되지 않고, 이후에 새로 그리는 도형부터 적용된다.
        public void SetDrawColor(Color color)
        {
            _drawBrush = new SolidColorBrush(color);
        }

        public void SetDrawThickness(double thickness)
        {
            _drawThickness = thickness;
        }

        public void SetDrawTool(DrawTool tool)
        {
            _drawTool = tool;
        }

        private void ExitMode()
        {
            _mode = OverlayMode.None;

            LaserDot.Visibility = Visibility.Collapsed;
            Cursor = Cursors.Arrow;
            DrawCanvas.Background = Brushes.Transparent;

            _pointerTimer.Stop();
            DetachDrawHandlers();

            SetClickThrough(true);
        }

        // ---------- 레이저 포인터 ----------

        private void PointerTimer_Tick(object? sender, EventArgs e)
        {
            UpdateLaserPosition();

            // 클릭 통과 상태라 우클릭도 창에 전달되지 않으므로, 전역 키 상태를 폴링해
            // 우클릭 시 포인터 모드를 해제한다.
            if (NativeMethods.IsRightMouseButtonDown())
            {
                ExitMode();
            }
        }

        private void UpdateLaserPosition()
        {
            // PointFromScreen은 Win32 화면 픽셀 좌표를 해당 Visual의 DPI 보정된
            // 로컬 좌표로 변환해주므로, 모니터 배율(125%/150% 등)이 달라도 정확하다.
            System.Drawing.Point screenPos = WinForms.Cursor.Position;
            Point localPos = DrawCanvas.PointFromScreen(new Point(screenPos.X, screenPos.Y));
            Canvas_SetCenter(LaserDot, localPos);
        }

        private static void Canvas_SetCenter(FrameworkElement element, Point center)
        {
            System.Windows.Controls.Canvas.SetLeft(element, center.X - element.Width / 2);
            System.Windows.Controls.Canvas.SetTop(element, center.Y - element.Height / 2);
        }

        // ---------- 그리기 (자유곡선/직선/사각형/정사각형/원형) ----------

        private void AttachDrawHandlers()
        {
            // 그리기 도구/색상/굵기를 바꿀 때마다 EnterDrawMode()가 다시 호출될 수 있으므로,
            // 먼저 떼어낸 뒤 다시 붙여서 중복 구독(스트로크 중복 생성)을 막는다.
            DetachDrawHandlers();
            DrawCanvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            DrawCanvas.MouseMove += Canvas_MouseMove;
            DrawCanvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
        }

        private void DetachDrawHandlers()
        {
            DrawCanvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
            DrawCanvas.MouseMove -= Canvas_MouseMove;
            DrawCanvas.MouseLeftButtonUp -= Canvas_MouseLeftButtonUp;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(DrawCanvas);
            _shapeStartPoint = pos;

            if (_drawTool == DrawTool.Freehand)
            {
                _currentStroke = new Polyline
                {
                    Stroke = _drawBrush,
                    StrokeThickness = _drawThickness,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                _currentStroke.Points.Add(pos);
                DrawCanvas.Children.Add(_currentStroke);
            }
            else if (_drawTool == DrawTool.Line)
            {
                // Line은 X1/Y1/X2/Y2를 캔버스 절대좌표로 직접 사용하므로,
                // Rectangle/Ellipse처럼 Canvas.Left/Top을 추가로 주면 시작점만큼 이중으로 밀려버린다.
                _currentShape = CreateShapeForTool(_drawTool);
                if (_currentShape is Line line)
                {
                    line.X1 = pos.X;
                    line.Y1 = pos.Y;
                    line.X2 = pos.X;
                    line.Y2 = pos.Y;
                }
                DrawCanvas.Children.Add(_currentShape);
            }
            else
            {
                _currentShape = CreateShapeForTool(_drawTool);
                Canvas.SetLeft(_currentShape, pos.X);
                Canvas.SetTop(_currentShape, pos.Y);
                DrawCanvas.Children.Add(_currentShape);
            }

            DrawCanvas.CaptureMouse();
        }

        private Shape CreateShapeForTool(DrawTool tool)
        {
            if (tool == DrawTool.Line)
            {
                return new Line
                {
                    Stroke = _drawBrush,
                    StrokeThickness = _drawThickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
            }

            if (tool == DrawTool.Ellipse)
            {
                return new Ellipse
                {
                    Stroke = _drawBrush,
                    StrokeThickness = _drawThickness,
                    Fill = Brushes.Transparent
                };
            }

            // Rectangle과 Square 모두 Rectangle Shape를 사용하고, 드래그 중 폭/높이 계산만 다르게 처리한다.
            return new Rectangle
            {
                Stroke = _drawBrush,
                StrokeThickness = _drawThickness,
                Fill = Brushes.Transparent
            };
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point pos = e.GetPosition(DrawCanvas);

            if (_drawTool == DrawTool.Freehand)
            {
                _currentStroke?.Points.Add(pos);
                return;
            }

            if (_currentShape == null)
            {
                return;
            }

            if (_currentShape is Line line)
            {
                line.X1 = _shapeStartPoint.X;
                line.Y1 = _shapeStartPoint.Y;
                line.X2 = pos.X;
                line.Y2 = pos.Y;
                return;
            }

            UpdateBoundingBoxShape(_currentShape, _shapeStartPoint, pos, keepSquare: _drawTool == DrawTool.Square);
        }

        private static void UpdateBoundingBoxShape(Shape shape, Point start, Point current, bool keepSquare)
        {
            double dx = current.X - start.X;
            double dy = current.Y - start.Y;

            if (keepSquare)
            {
                double side = Math.Max(Math.Abs(dx), Math.Abs(dy));
                dx = side * (dx < 0 ? -1 : 1);
                dy = side * (dy < 0 ? -1 : 1);
            }

            double left = Math.Min(start.X, start.X + dx);
            double top = Math.Min(start.Y, start.Y + dy);

            shape.Width = Math.Abs(dx);
            shape.Height = Math.Abs(dy);
            Canvas.SetLeft(shape, left);
            Canvas.SetTop(shape, top);
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _currentStroke = null;
            _currentShape = null;
            DrawCanvas.ReleaseMouseCapture();

            // TODO(v1.1): Undo/Redo 스택에 스트로크 추가
        }

        private void ClearDrawing()
        {
            for (int i = DrawCanvas.Children.Count - 1; i >= 0; i--)
            {
                // LaserDot도 Ellipse(Shape)이므로 참조로 명시적으로 제외하고, 그려진 도형만 지운다.
                if (DrawCanvas.Children[i] is Shape shape && !ReferenceEquals(shape, LaserDot))
                {
                    DrawCanvas.Children.RemoveAt(i);
                }
            }
        }

        // ---------- 타이머 표시 ----------

        // 오버레이는 모든 모니터를 합친 가상 데스크톱 전체를 덮으므로, 타이머 관련 표시는
        // 항상 이 오버레이의 ActualWidth/Height(전체 영역) 기준이 아니라 타이머 창이 실제로
        // 놓여 있는 모니터 하나의 영역 기준으로 위치를 잡아야 한다.
        private Rect GetAnchorMonitorLocalBounds(Window anchorWindow)
        {
            Point centerScreen = anchorWindow.PointToScreen(
                new Point(anchorWindow.ActualWidth / 2, anchorWindow.ActualHeight / 2));

            var screen = WinForms.Screen.FromPoint(new System.Drawing.Point((int)centerScreen.X, (int)centerScreen.Y));

            // PointFromScreen은 이 오버레이 창 자체의 DPI 컨텍스트를 기준으로 변환해주므로,
            // 오버레이가 실제로 렌더링되는 좌표계와 항상 일치한다.
            Point topLeft = DrawCanvas.PointFromScreen(new Point(screen.Bounds.Left, screen.Bounds.Top));
            Point bottomRight = DrawCanvas.PointFromScreen(new Point(screen.Bounds.Right, screen.Bounds.Bottom));
            return new Rect(topLeft, bottomRight);
        }

        // 화면 상단에 배너 형태로 경고 문구를 띄우고 지정한 시간 뒤에 스스로 사라진다.
        public async void ShowTimerWarning(string message, int visibleSeconds, Window anchorWindow)
        {
            Rect bounds = GetAnchorMonitorLocalBounds(anchorWindow);

            var text = new TextBlock
            {
                Text = message,
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            var badge = new Border
            {
                Width = bounds.Width,
                Background = new SolidColorBrush(Color.FromArgb(0x66, 0x20, 0x20, 0x20)),
                Padding = new Thickness(20),
                Child = text
            };

            Canvas.SetLeft(badge, bounds.Left);
            Canvas.SetTop(badge, bounds.Top + bounds.Height * 0.12);
            DrawCanvas.Children.Add(badge);

            await Task.Delay(TimeSpan.FromSeconds(visibleSeconds));

            DrawCanvas.Children.Remove(badge);
        }

        private UIElement? _timeUpContainer;

        // 화면 중앙에 화면 크기의 절반 정도로 마스코트를 띄운다(발표 시간 종료 알림용).
        // 오버레이는 기본적으로 화면 전체가 투명(알파 0)이라 클릭이 하위 창으로 통과하는데,
        // 닫기 버튼을 누를 수 있어야 하므로 떠 있는 동안은 클릭 통과를 잠시 꺼둔다
        // (투명한 나머지 영역은 알파값이 0이라 여전히 클릭이 그대로 통과된다).
        public void ShowTimeUpMascot(UIElement content, Window anchorWindow)
        {
            HideTimeUpMascot();

            Rect bounds = GetAnchorMonitorLocalBounds(anchorWindow);

            // content의 실제 비율에 맞춰 꽉 차게 박스를 잡아야, 닫기 버튼이 마스코트 바로
            // 옆(빈 여백이 아니라)에 붙는다. 50% 제한 상자 안에서 원본 비율을 유지한 채
            // 최대로 키운 크기를 직접 계산한다.
            content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size natural = content.DesiredSize;

            double maxWidth = bounds.Width * 0.5;
            double maxHeight = bounds.Height * 0.5;
            double scale = Math.Min(maxWidth / natural.Width, maxHeight / natural.Height);
            double boxWidth = natural.Width * scale;
            double boxHeight = natural.Height * scale;

            var viewbox = new Viewbox
            {
                Child = content,
                Width = boxWidth,
                Height = boxHeight,
                Stretch = Stretch.Uniform
            };

            var closeButton = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x20, 0x20, 0x20)),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -12, -12, 0),
                Child = new TextBlock
                {
                    Text = "✕",
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            closeButton.MouseLeftButtonUp += (_, _) => HideTimeUpMascot();

            var container = new Grid { Width = boxWidth, Height = boxHeight };
            container.Children.Add(viewbox);
            container.Children.Add(closeButton);

            Canvas.SetLeft(container, bounds.Left + (bounds.Width - boxWidth) / 2);
            Canvas.SetTop(container, bounds.Top + (bounds.Height - boxHeight) / 2);
            DrawCanvas.Children.Add(container);

            _timeUpContainer = container;
            SetClickThrough(false);

            // TimerWindow도 Topmost라 화면 중앙에서 이 오버레이와 겹치는데, TimerWindow가
            // 나중에 활성화되어 위에 있으면 마스코트의 X 클릭이 TimerWindow에 가로채인다.
            // Topmost를 껐다 켜서 오버레이(및 이 닫기 버튼)를 다시 맨 앞으로 올린다.
            Topmost = false;
            Topmost = true;
        }

        // 닫기 버튼 클릭 또는 타이머 창이 닫혔을 때 호출되어 마스코트를 치운다.
        public void HideTimeUpMascot()
        {
            if (_timeUpContainer == null)
            {
                return;
            }

            DrawCanvas.Children.Remove(_timeUpContainer);
            _timeUpContainer = null;

            // 원래 모드(포인터/그리기/없음)에 맞는 클릭 통과 상태로 되돌린다.
            SetClickThrough(_mode != OverlayMode.Draw);
        }

        // ---------- 공통 ----------

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ExitMode();
            }
            else if (e.Key == Key.Delete && _mode == OverlayMode.Draw)
            {
                ClearDrawing();
            }
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 그리기 모드(클릭 통과 OFF)에서는 이 라우티드 이벤트로 우클릭이 정상 전달된다.
            // 포인터 모드(클릭 통과 ON)의 우클릭 해제는 PointerTimer_Tick의 폴링이 담당한다.
            ExitMode();
        }

        private void SetClickThrough(bool clickThrough)
        {
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                NativeMethods.SetClickThrough(hwndSource.Handle, clickThrough);
            }
        }
    }
}
