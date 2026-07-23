using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Presenter
{
    public partial class MascotWindow : Window
    {
        // 드래그와 "클릭"을 구분하기 위한 최소 이동 거리(px).
        // 이보다 적게 움직이면 드래그가 아니라 클릭으로 간주 -> 메뉴 표시.
        private const double DragThreshold = 4.0;

        private static readonly (string Label, Color Color)[] ColorPresets =
        {
            ("빨강", Color.FromRgb(0xFF, 0x3B, 0x30)),
            ("주황", Color.FromRgb(0xFF, 0x9F, 0x0A)),
            ("노랑", Color.FromRgb(0xFF, 0xD6, 0x0A)),
            ("초록", Color.FromRgb(0x30, 0xD1, 0x58)),
            ("파랑", Color.FromRgb(0x0A, 0x84, 0xFF)),
            ("보라", Color.FromRgb(0xBF, 0x5A, 0xF2)),
        };

        // 기존 고정 크기(21.6)를 "보통"으로 두고, 더 작은 것 1개 + 더 큰 것 3개를 제공한다.
        private static readonly (string Label, double Diameter)[] SizePresets =
        {
            ("작게", 15.0),
            ("보통", 21.6),
            ("크게", 30.0),
            ("더 크게", 40.0),
            ("아주 크게", 50.0),
        };

        // 기존 고정 두께(3px)를 "보통"으로 둔다.
        private static readonly (string Label, double Thickness)[] ThicknessPresets =
        {
            ("얇게", 2.0),
            ("보통", 3.0),
            ("굵게", 6.0),
            ("더 굵게", 10.0),
            ("아주 굵게", 16.0),
        };

        private static readonly (string Label, DrawTool Tool)[] ToolPresets =
        {
            ("자유곡선", DrawTool.Freehand),
            ("직선", DrawTool.Line),
            ("정사각형", DrawTool.Square),
            ("직사각형", DrawTool.Rectangle),
            ("원형", DrawTool.Ellipse),
        };

        private Point _mouseDownScreenPos;
        private bool _isDragging;

        private OverlayWindow? _overlay;

        private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private bool _showClock;

        private Color _pointerColor = ColorPresets[0].Color;
        private double _pointerSize = SizePresets[1].Diameter;

        private Color _drawColor = ColorPresets[0].Color;
        private double _drawThickness = ThicknessPresets[1].Thickness;
        private DrawTool _drawTool = DrawTool.Freehand;

        public MascotWindow()
        {
            InitializeComponent();

            // 화면 우측 하단 근처에 기본 배치
            Left = SystemParameters.WorkArea.Width - Width - 40;
            Top = SystemParameters.WorkArea.Height - Height - 40;

            InitializeFace();

            _clockTimer.Tick += (_, _) => UpdateClockText();
        }

        private void UpdateClockText()
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void SetClockVisible(bool visible)
        {
            _showClock = visible;
            ClockText.Visibility = visible ? Visibility.Visible : Visibility.Hidden;

            if (visible)
            {
                UpdateClockText();
                _clockTimer.Start();
            }
            else
            {
                _clockTimer.Stop();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownScreenPos = PointToScreen(e.GetPosition(this));
            _isDragging = false;

            CaptureMouse();
            MouseMove += Window_MouseMove;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point current = PointToScreen(e.GetPosition(this));
            Vector delta = current - _mouseDownScreenPos;

            if (!_isDragging && delta.Length > DragThreshold)
            {
                _isDragging = true;
            }

            if (_isDragging)
            {
                Left += delta.X;
                Top += delta.Y;
                _mouseDownScreenPos = current;

                UpdateFaceDirection(delta.X);
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseMove -= Window_MouseMove;
            ReleaseMouseCapture();

            if (!_isDragging)
            {
                // 이동 없이 눌렀다 뗀 경우 = "클릭" -> 메뉴 표시
                ShowModeMenu();
            }

            _isDragging = false;
        }

        private void ShowModeMenu()
        {
            var menu = new ContextMenu
            {
                PlacementTarget = this
            };

            var presentationItem = new MenuItem { Header = "프레젠테이션 모드 (레이저 포인터)" };
            presentationItem.Click += (_, _) =>
            {
                menu.IsOpen = false;
                StartPresentationMode();
            };
            menu.Items.Add(presentationItem);

            menu.Items.Add(BuildColorSubmenu(menu));
            menu.Items.Add(BuildSizeSubmenu(menu));

            var drawItem = new MenuItem { Header = "그리기 모드" };
            drawItem.Click += (_, _) =>
            {
                menu.IsOpen = false;
                StartDrawingMode();
            };
            menu.Items.Add(drawItem);

            menu.Items.Add(BuildToolSubmenu(menu));
            menu.Items.Add(BuildDrawColorSubmenu(menu));
            menu.Items.Add(BuildThicknessSubmenu(menu));

            var timerItem = new MenuItem { Header = "타이머" };
            timerItem.Click += (_, _) =>
            {
                menu.IsOpen = false;
                OpenTimerWindow();
            };
            menu.Items.Add(timerItem);

            var clockItem = new MenuItem { Header = "현재 시간 표시", IsCheckable = true, IsChecked = _showClock };
            clockItem.Click += (_, _) =>
            {
                SetClockVisible(clockItem.IsChecked);
                menu.IsOpen = false;
            };
            menu.Items.Add(clockItem);

            menu.Items.Add(new Separator());

            var helpItem = new MenuItem { Header = "사용법" };
            helpItem.Click += (_, _) =>
            {
                menu.IsOpen = false;
                new HelpWindow().ShowDialog();
            };
            menu.Items.Add(helpItem);

            menu.Items.Add(new Separator());

            var hideItem = new MenuItem { Header = "숨기기" };
            hideItem.Click += (_, _) =>
            {
                menu.IsOpen = false;
                Hide();
            };
            menu.Items.Add(hideItem);

            var exitItem = new MenuItem { Header = "종료" };
            exitItem.Click += (_, _) =>
            {
                menu.IsOpen = false;
                if (App.ConfirmExit())
                {
                    Application.Current.Shutdown();
                }
            };
            menu.Items.Add(exitItem);

            menu.IsOpen = true;
        }

        // "프레젠테이션 모드"/"그리기 모드" 액션 항목에 딸린 하위 설정처럼 보이도록
        // 들여쓰기하고 글자를 살짝 작고 옅게 만든 서브메뉴 헤더.
        private static MenuItem CreateIndentedSubmenuItem(string header)
        {
            return new MenuItem
            {
                Header = header,
                Padding = new Thickness(30, 6, 14, 6),
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC))
            };
        }

        // 색상 스와치 하나. 선택된 항목은 파란색 링(테두리)으로 뚜렷하게 구분한다.
        private static Border CreateColorSwatch(Color color, string label, bool isSelected)
        {
            var circle = new Ellipse
            {
                Width = 22,
                Height = 22,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            return new Border
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(3),
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x0A, 0x84, 0xFF)),
                BorderThickness = new Thickness(isSelected ? 2.5 : 0),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                ToolTip = label,
                Child = circle
            };
        }

        // 목록형 항목(크기/굵기/도구) 한 줄. 선택된 항목은 왼쪽 체크 표시 + 옅은 파란 배경으로 구분한다.
        private static Border CreateOptionRow(FrameworkElement preview, string label, bool isSelected)
        {
            var check = new TextBlock
            {
                Text = "✓",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0x84, 0xFF)),
                Width = 16,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = isSelected ? Visibility.Visible : Visibility.Hidden
            };

            var labelBlock = new TextBlock
            {
                Text = label,
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(check);
            content.Children.Add(preview);
            content.Children.Add(labelBlock);

            return new Border
            {
                Child = content,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 5, 12, 5),
                Margin = new Thickness(2, 1, 2, 1),
                Background = isSelected
                    ? new SolidColorBrush(Color.FromArgb(0x33, 0x0A, 0x84, 0xFF))
                    : Brushes.Transparent,
                Cursor = Cursors.Hand
            };
        }

        // 미리보기 아이콘마다 폭이 달라도 체크/라벨이 항상 같은 위치에 오도록 고정폭 슬롯에 담는다.
        private static FrameworkElement InPreviewSlot(FrameworkElement preview, double slotSize = 32)
        {
            var slot = new Grid { Width = slotSize, Height = slotSize };
            preview.HorizontalAlignment = HorizontalAlignment.Center;
            preview.VerticalAlignment = VerticalAlignment.Center;
            slot.Children.Add(preview);
            return slot;
        }

        // 마우스를 올리면 실제 화면의 레이저 포인터에 바로 미리보기가 반영되고,
        // 벗어나면 마지막으로 선택했던 값으로 되돌아간다. 클릭하면 그 값으로 확정된다.
        private MenuItem BuildColorSubmenu(ContextMenu menu)
        {
            var colorItem = CreateIndentedSubmenuItem("포인터 색상  ▸");
            var panel = new WrapPanel { Width = 176, Margin = new Thickness(2) };

            foreach (var preset in ColorPresets)
            {
                bool isSelected = preset.Color == _pointerColor;
                var swatch = CreateColorSwatch(preset.Color, preset.Label, isSelected);

                swatch.MouseEnter += (_, _) => _overlay?.ApplyPointerAppearance(preset.Color, _pointerSize);
                swatch.MouseLeave += (_, _) => _overlay?.ApplyPointerAppearance(_pointerColor, _pointerSize);
                swatch.MouseLeftButtonUp += (_, _) =>
                {
                    _pointerColor = preset.Color;
                    _overlay?.ApplyPointerAppearance(_pointerColor, _pointerSize);
                    menu.IsOpen = false;
                };

                panel.Children.Add(swatch);
            }

            colorItem.Items.Add(new MenuItem { Header = panel });
            return colorItem;
        }

        private MenuItem BuildSizeSubmenu(ContextMenu menu)
        {
            var sizeItem = CreateIndentedSubmenuItem("포인터 크기  ▸");
            var panel = new StackPanel { Margin = new Thickness(2) };

            foreach (var preset in SizePresets)
            {
                bool isSelected = preset.Diameter == _pointerSize;
                double previewDiameter = System.Math.Min(preset.Diameter, 28);

                var previewDot = new Ellipse
                {
                    Width = previewDiameter,
                    Height = previewDiameter,
                    Fill = new SolidColorBrush(Color.FromArgb(0x4D, _pointerColor.R, _pointerColor.G, _pointerColor.B)),
                    Stroke = new SolidColorBrush(Color.FromArgb(0xB3, _pointerColor.R, _pointerColor.G, _pointerColor.B)),
                    StrokeThickness = 1.2
                };

                var row = CreateOptionRow(InPreviewSlot(previewDot), preset.Label, isSelected);

                row.MouseEnter += (_, _) => _overlay?.ApplyPointerAppearance(_pointerColor, preset.Diameter);
                row.MouseLeave += (_, _) => _overlay?.ApplyPointerAppearance(_pointerColor, _pointerSize);
                row.MouseLeftButtonUp += (_, _) =>
                {
                    _pointerSize = preset.Diameter;
                    _overlay?.ApplyPointerAppearance(_pointerColor, _pointerSize);
                    menu.IsOpen = false;
                };

                panel.Children.Add(row);
            }

            sizeItem.Items.Add(new MenuItem { Header = panel });
            return sizeItem;
        }

        // 그리기 도구는 레이저 포인터와 달리 화면에 계속 떠 있는 요소가 아니라서
        // (스트로크는 그릴 때만 생기므로) 마우스오버 미리보기 대상이 없다. 그래서 각 항목에
        // 아이콘으로 모양/두께/색을 보여주고, 클릭하면 다음 스트로크부터 바로 적용한다.
        private MenuItem BuildToolSubmenu(ContextMenu menu)
        {
            var toolItem = CreateIndentedSubmenuItem("그리기 도구  ▸");
            var panel = new StackPanel { Margin = new Thickness(2) };

            foreach (var preset in ToolPresets)
            {
                bool isSelected = preset.Tool == _drawTool;
                var row = CreateOptionRow(InPreviewSlot(CreateToolIcon(preset.Tool)), preset.Label, isSelected);

                row.MouseLeftButtonUp += (_, _) =>
                {
                    _drawTool = preset.Tool;
                    menu.IsOpen = false;
                    StartDrawingMode();
                };

                panel.Children.Add(row);
            }

            toolItem.Items.Add(new MenuItem { Header = panel });
            return toolItem;
        }

        private FrameworkElement CreateToolIcon(DrawTool tool)
        {
            const double iconSize = 22;
            var brush = new SolidColorBrush(_drawColor);

            switch (tool)
            {
                case DrawTool.Line:
                    return new Line
                    {
                        X1 = 2,
                        Y1 = iconSize - 2,
                        X2 = iconSize - 2,
                        Y2 = 2,
                        Stroke = brush,
                        StrokeThickness = 2,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        Width = iconSize,
                        Height = iconSize
                    };

                case DrawTool.Square:
                    return new Rectangle
                    {
                        Width = iconSize - 4,
                        Height = iconSize - 4,
                        Stroke = brush,
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent
                    };

                case DrawTool.Rectangle:
                    return new Rectangle
                    {
                        Width = iconSize,
                        Height = (iconSize - 4) * 0.6,
                        Stroke = brush,
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                case DrawTool.Ellipse:
                    return new Ellipse
                    {
                        Width = iconSize - 2,
                        Height = iconSize - 2,
                        Stroke = brush,
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent
                    };

                default: // Freehand
                    var freehand = new Polyline
                    {
                        Stroke = brush,
                        StrokeThickness = 2,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeLineJoin = PenLineJoin.Round,
                        Width = iconSize,
                        Height = iconSize
                    };
                    freehand.Points.Add(new Point(1, iconSize - 4));
                    freehand.Points.Add(new Point(7, 4));
                    freehand.Points.Add(new Point(13, iconSize - 6));
                    freehand.Points.Add(new Point(iconSize - 2, 3));
                    return freehand;
            }
        }

        private MenuItem BuildDrawColorSubmenu(ContextMenu menu)
        {
            var colorItem = CreateIndentedSubmenuItem("선 색상  ▸");
            var panel = new WrapPanel { Width = 176, Margin = new Thickness(2) };

            foreach (var preset in ColorPresets)
            {
                bool isSelected = preset.Color == _drawColor;
                var swatch = CreateColorSwatch(preset.Color, preset.Label, isSelected);

                swatch.MouseLeftButtonUp += (_, _) =>
                {
                    _drawColor = preset.Color;
                    menu.IsOpen = false;
                    StartDrawingMode();
                };

                panel.Children.Add(swatch);
            }

            colorItem.Items.Add(new MenuItem { Header = panel });
            return colorItem;
        }

        private MenuItem BuildThicknessSubmenu(ContextMenu menu)
        {
            var thicknessItem = CreateIndentedSubmenuItem("선 굵기  ▸");
            var panel = new StackPanel { Margin = new Thickness(2) };

            foreach (var preset in ThicknessPresets)
            {
                bool isSelected = preset.Thickness == _drawThickness;

                var previewLine = new Line
                {
                    X1 = 0,
                    Y1 = 0,
                    X2 = 26,
                    Y2 = 0,
                    Stroke = new SolidColorBrush(_drawColor),
                    StrokeThickness = preset.Thickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Width = 26,
                    Height = preset.Thickness
                };

                var row = CreateOptionRow(InPreviewSlot(previewLine), preset.Label, isSelected);

                row.MouseLeftButtonUp += (_, _) =>
                {
                    _drawThickness = preset.Thickness;
                    menu.IsOpen = false;
                    StartDrawingMode();
                };

                panel.Children.Add(row);
            }

            thicknessItem.Items.Add(new MenuItem { Header = panel });
            return thicknessItem;
        }

        private void StartPresentationMode()
        {
            EnsureOverlay();
            _overlay!.ApplyPointerAppearance(_pointerColor, _pointerSize);
            _overlay!.EnterPointerMode();
        }

        private void StartDrawingMode()
        {
            EnsureOverlay();
            _overlay!.SetDrawColor(_drawColor);
            _overlay!.SetDrawThickness(_drawThickness);
            _overlay!.SetDrawTool(_drawTool);
            _overlay!.EnterDrawMode();
        }

        private void OpenTimerWindow()
        {
            var timerWindow = new TimerWindow();

            timerWindow.SecondsRemainingChanged += remainingSeconds =>
            {
                if (remainingSeconds == 60 || remainingSeconds == 30)
                {
                    EnsureOverlay();
                    _overlay!.ShowTimerWarning("발표 시간이 다 되어갑니다", visibleSeconds: 5, timerWindow);
                }
            };

            timerWindow.TimeUp += () =>
            {
                EnsureOverlay();
                _overlay!.ShowTimeUpMascot(BuildTimeUpVisual(), timerWindow);
            };

            // 타이머 창을 닫으면(닫기 버튼이든 다시 열어서든) 떠 있던 종료 마스코트도 같이 치운다.
            timerWindow.Closed += (_, _) => _overlay?.HideTimeUpMascot();

            timerWindow.Show();
        }

        private void EnsureOverlay()
        {
            if (_overlay == null || !_overlay.IsLoaded)
            {
                _overlay = new OverlayWindow();
                _overlay.Show();
            }

            // 오버레이는 전체 화면을 덮는 Topmost 창이라, Show() 이후로는 마스코트보다
            // 위(z-order)에 있게 되어 마스코트 클릭/메뉴 선택이 먹히지 않게 된다.
            // Topmost를 껐다 켜서 마스코트(및 그 컨텍스트 메뉴)를 다시 맨 앞으로 올린다.
            Topmost = false;
            Topmost = true;
        }
    }
}
