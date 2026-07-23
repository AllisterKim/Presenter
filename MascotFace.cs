using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Presenter
{
    // 오랑우탄을 모티브로 한 자체 창작 마스코트를 셀 단위 픽셀아트로 그린다(라이선스 소스 미사용).
    // 매끈한 곡선 대신 각진 정사각형 셀을 그대로 두어 90년대 후반 저해상도 스프라이트 느낌을 낸다.
    public partial class MascotWindow
    {
        private const int FaceGridColumns = 22;
        private const int FaceGridRows = 38;
        private const double FaceCellSize = 4.0; // 22x38 * 4 = 88x152, 창 크기와 정확히 맞는다.

        private static readonly Color FurColor = Color.FromRgb(0xC9, 0x7B, 0x3D);
        private static readonly Color FurEdgeColor = Color.FromRgb(0x8B, 0x4A, 0x1E);
        private static readonly Color FaceColor = Color.FromRgb(0xE8, 0xC7, 0x9A);
        private static readonly Color DarkColor = Color.FromRgb(0x2A, 0x1A, 0x10);

        // 발 아래 두는 뭉게구름. 특정 캐릭터의 구름을 그대로 베끼지 않고,
        // "흰 구름 + 회색 테두리 + 잔뭉치가 흩어진 느낌"이라는 일반적인 형태만 참고한 자체 창작 디자인.
        private static readonly Color CloudColor = Colors.White;
        private static readonly Color CloudEdgeColor = Color.FromRgb(0xB0, 0xB0, 0xB0);

        // 변신 오라 색상. 특정 캐릭터의 변신 모습(머리카락 형태 등)을 그대로 그리지 않고,
        // "노란색 → 파란색으로 바뀌는 은은한 발광 효과"만 일반화해서 표현한다.
        private static readonly Color SaiyanAuraColor = Color.FromRgb(0xFF, 0xD6, 0x0A);
        private static readonly Color SuperSaiyanAuraColor = Color.FromRgb(0x29, 0x79, 0xFF);

        private readonly Random _idleRandom = new();
        private readonly DispatcherTimer _idleTimer = new();
        private readonly DispatcherTimer _transformTimer = new() { Interval = TimeSpan.FromSeconds(7) };
        private bool _isPlayingIdleAnimation;
        private int _transformPhase; // 0=평상시, 1=초사이언(노랑), 2=슈퍼 초사이언(파랑)

        private void InitializeFace()
        {
            SetFace(blink: false, mouthOpen: false);

            _idleTimer.Tick += IdleTimer_Tick;
            ScheduleNextIdleAnimation();

            StartBreathingBounce();
            StartTransformCycle();
        }

        // 7초 주기로 평상시 -> 초사이언(노랑 오라) -> 슈퍼 초사이언(파랑 오라) -> 평상시 순으로 반복한다.
        private void StartTransformCycle()
        {
            FaceHost.Effect = new DropShadowEffect
            {
                ShadowDepth = 0,
                BlurRadius = 26,
                Opacity = 0,
                Color = SaiyanAuraColor
            };

            _transformTimer.Tick += (_, _) =>
            {
                _transformPhase = (_transformPhase + 1) % 3;
                ApplyTransformPhase();
            };
            _transformTimer.Start();
        }

        private void ApplyTransformPhase()
        {
            if (FaceHost.Effect is not DropShadowEffect glow)
            {
                return;
            }

            Color targetColor = _transformPhase switch
            {
                1 => SaiyanAuraColor,
                2 => SuperSaiyanAuraColor,
                _ => glow.Color
            };
            double targetOpacity = _transformPhase == 0 ? 0 : 0.85;

            glow.BeginAnimation(DropShadowEffect.ColorProperty, new ColorAnimation(targetColor, TimeSpan.FromSeconds(0.6)));
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(targetOpacity, TimeSpan.FromSeconds(0.6)));

            PlayPowerUpPulse();
        }

        // 변신 순간에 살짝 커졌다가 돌아오는 파워업 느낌의 펄스.
        private void PlayPowerUpPulse()
        {
            var pulse = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(0.6)
            };
            pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(0)));
            pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1.18, KeyTime.FromPercent(0.4)));
            pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1)));

            FacePulse.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            FacePulse.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        // 숨쉬듯 살짝 위아래로 계속 움직이는 애니메이션. 귀여운 느낌을 위해 항상 재생한다.
        private void StartBreathingBounce()
        {
            var bounce = new DoubleAnimation
            {
                From = 0,
                To = -3,
                Duration = TimeSpan.FromSeconds(0.8),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            FaceBounce.BeginAnimation(TranslateTransform.YProperty, bounce);
        }

        // 드래그 방향(왼쪽/오른쪽)에 맞춰 마스코트를 좌우로 반전시킨다.
        private void UpdateFaceDirection(double deltaX)
        {
            if (deltaX < -0.5)
            {
                FaceFlip.ScaleX = -1;
            }
            else if (deltaX > 0.5)
            {
                FaceFlip.ScaleX = 1;
            }
        }

        private void ScheduleNextIdleAnimation()
        {
            _idleTimer.Stop();
            _idleTimer.Interval = TimeSpan.FromSeconds(_idleRandom.Next(3, 7));
            _idleTimer.Start();
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            // 드래그 중이 아니라("고정" 상태) 눈을 깜빡이거나 입을 움직인다.
            if (!_isDragging && !_isPlayingIdleAnimation)
            {
                _ = PlayRandomIdleAnimationAsync();
            }

            ScheduleNextIdleAnimation();
        }

        private async Task PlayRandomIdleAnimationAsync()
        {
            _isPlayingIdleAnimation = true;

            int choice = _idleRandom.Next(3);
            if (choice == 0)
            {
                SetFace(blink: true, mouthOpen: false);
                await Task.Delay(150);
                SetFace(blink: false, mouthOpen: false);
            }
            else if (choice == 1)
            {
                SetFace(blink: false, mouthOpen: true);
                await Task.Delay(400);
                SetFace(blink: false, mouthOpen: false);
            }
            else
            {
                await PlayWiggleAsync();
            }

            _isPlayingIdleAnimation = false;
        }

        // 살짝 좌우로 몸을 흔드는 귀여운 모션.
        private async Task PlayWiggleAsync()
        {
            const int durationMs = 500;
            var keyFrames = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };
            keyFrames.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            keyFrames.KeyFrames.Add(new EasingDoubleKeyFrame(-10, KeyTime.FromPercent(0.25)));
            keyFrames.KeyFrames.Add(new EasingDoubleKeyFrame(10, KeyTime.FromPercent(0.5)));
            keyFrames.KeyFrames.Add(new EasingDoubleKeyFrame(-6, KeyTime.FromPercent(0.75)));
            keyFrames.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));

            FaceWiggle.BeginAnimation(RotateTransform.AngleProperty, keyFrames);
            await Task.Delay(durationMs);
        }

        private void SetFace(bool blink, bool mouthOpen)
        {
            FaceHost.Children.Clear();
            FaceHost.Children.Add(BuildFaceVisual(blink, mouthOpen));
        }

        // 발표 시간 종료 시 화면 중앙에 띄우는 "- 끝 -" 마스코트. 지금 쓰는 마스코트 그림을
        // 그대로 재사용하고 문구만 아래에 덧붙인다.
        private static UIElement BuildTimeUpVisual()
        {
            var face = BuildFaceVisual(blink: false, mouthOpen: false);

            var text = new TextBlock
            {
                Text = "- 끝 -",
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 14, 0, 0)
            };

            var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            panel.Children.Add(face);
            panel.Children.Add(text);
            return panel;
        }

        // 트레이 아이콘용으로 몸통/팔다리/구름 없이 머리(귀/눈/코/입)만 그린 미니 얼굴.
        // 헤드 부분 좌표(headCx/headCy/headRadius 등)는 BuildFaceVisual과 동일하게 맞춰
        // 같은 비율의 얼굴로 보이게 한다.
        internal static UIElement BuildTrayIconFaceVisual(double pixelSize)
        {
            const int cols = FaceGridColumns; // 22
            const int rows = 16; // 머리 전체(귀 포함)가 들어가는 최소 높이

            var grid = new UniformGrid
            {
                Rows = rows,
                Columns = cols,
                Width = cols * FaceCellSize,
                Height = rows * FaceCellSize
            };
            RenderOptions.SetEdgeMode(grid, EdgeMode.Aliased);

            const double headCx = 10.5, headCy = 8.0, headRadius = 7.3;
            const double faceCx = 10.5, faceCy = 9.5, faceRadius = 5.2;
            var ears = new (double X, double Y, double R)[]
            {
                (headCx - 5.2, 3.0, 2.0),
                (headCx + 5.2, 3.0, 2.0)
            };

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    double x = col + 0.5;
                    double y = row + 0.5;

                    Color? color = null;

                    foreach (var ear in ears)
                    {
                        if (Distance(x, y, ear.X, ear.Y) <= ear.R)
                        {
                            color = FurColor;
                        }
                    }

                    double headDist = Distance(x, y, headCx, headCy);
                    if (headDist <= headRadius - 1.2)
                    {
                        color = FurColor;
                    }
                    else if (headDist <= headRadius)
                    {
                        color = FurEdgeColor;
                    }

                    if (color != null && Distance(x, y, faceCx, faceCy) <= faceRadius)
                    {
                        color = FaceColor;
                    }

                    bool isLeftEye = (col == 7 || col == 8) && (row == 7 || row == 8);
                    bool isRightEye = (col == 13 || col == 14) && (row == 7 || row == 8);
                    if (isLeftEye || isRightEye)
                    {
                        color = DarkColor;
                    }

                    bool isNose = row == 10 && (col == 10 || col == 11);
                    bool isMouth = row == 12 && col >= 9 && col <= 12;
                    if (isNose || isMouth)
                    {
                        color = DarkColor;
                    }

                    grid.Children.Add(new Rectangle
                    {
                        Width = FaceCellSize,
                        Height = FaceCellSize,
                        Fill = color.HasValue ? new SolidColorBrush(color.Value) : Brushes.Transparent
                    });
                }
            }

            return new Viewbox
            {
                Width = pixelSize,
                Height = pixelSize,
                Stretch = Stretch.Uniform,
                Child = grid
            };
        }

        private static UIElement BuildFaceVisual(bool blink, bool mouthOpen)
        {
            var grid = new UniformGrid
            {
                Rows = FaceGridRows,
                Columns = FaceGridColumns,
                Width = FaceGridColumns * FaceCellSize,
                Height = FaceGridRows * FaceCellSize
            };
            RenderOptions.SetEdgeMode(grid, EdgeMode.Aliased);

            const double headCx = 10.5, headCy = 8.0, headRadius = 7.3;
            const double faceCx = 10.5, faceCy = 9.5, faceRadius = 5.2;
            var ears = new (double X, double Y, double R)[]
            {
                (headCx - 5.2, 3.0, 2.0),
                (headCx + 5.2, 3.0, 2.0)
            };

            const double torsoCx = headCx, torsoCy = 19.0, torsoRx = 6.5, torsoRy = 6.0;
            const double bellyCx = headCx, bellyCy = 20.0, bellyRadius = 3.3;
            const double armRadius = 1.8;
            var arms = new (double ShoulderX, double ShoulderY, double HandX, double HandY)[]
            {
                (headCx - 6.5, 16.5, headCx - 9.5, 24.5),
                (headCx + 6.5, 16.5, headCx + 9.5, 24.5)
            };

            const double legRadius = 2.1;
            var legs = new (double HipX, double HipY, double FootX, double FootY)[]
            {
                (headCx - 2.8, 24.0, headCx - 3.5, 29.5),
                (headCx + 2.8, 24.0, headCx + 3.5, 29.5)
            };

            // 여러 개의 타원을 겹쳐서 뭉게뭉게한 구름 실루엣을 만든다.
            var cloudPuffs = new (double Cx, double Cy, double Rx, double Ry)[]
            {
                (headCx, 34.0, 9.5, 3.2),
                (headCx - 6.3, 32.6, 3.2, 3.2),
                (headCx - 2.2, 31.1, 3.5, 3.5),
                (headCx + 2.2, 31.1, 3.5, 3.5),
                (headCx + 6.3, 32.6, 3.2, 3.2)
            };

            // 구름 몸통에서 떨어져 나온 듯한 작은 잔뭉치들 (잔여 느낌).
            var cloudWisps = new (double Cx, double Cy, double R)[]
            {
                (headCx - 10.2, 34.8, 1.2),
                (headCx + 10.2, 34.8, 1.2),
                (headCx - 8.2, 36.4, 0.9),
                (headCx + 8.2, 36.4, 0.9),
                (headCx, 36.8, 1.0)
            };

            for (int row = 0; row < FaceGridRows; row++)
            {
                for (int col = 0; col < FaceGridColumns; col++)
                {
                    double x = col + 0.5;
                    double y = row + 0.5;

                    Color? color = null;

                    // 구름 (가장 뒤에 깔리고, 다리/발이 그 위에 겹쳐 그려진다)
                    foreach (var puff in cloudPuffs)
                    {
                        double norm = Math.Pow((x - puff.Cx) / puff.Rx, 2) + Math.Pow((y - puff.Cy) / puff.Ry, 2);
                        if (norm <= 0.7)
                        {
                            color = CloudColor;
                        }
                        else if (norm <= 1.0 && color == null)
                        {
                            color = CloudEdgeColor;
                        }
                    }

                    foreach (var wisp in cloudWisps)
                    {
                        if (Distance(x, y, wisp.Cx, wisp.Cy) <= wisp.R)
                        {
                            color = CloudColor;
                        }
                    }

                    // 다리 (엉덩이->발 캡슐 모양)
                    foreach (var leg in legs)
                    {
                        double legDist = DistanceToSegment(x, y, leg.HipX, leg.HipY, leg.FootX, leg.FootY);
                        if (legDist <= legRadius)
                        {
                            color = FurColor;
                        }

                        if (Distance(x, y, leg.FootX, leg.FootY) <= legRadius + 0.4)
                        {
                            color = FurEdgeColor; // 발
                        }
                    }

                    // 팔 (어깨->손 캡슐 모양)
                    foreach (var arm in arms)
                    {
                        double armDist = DistanceToSegment(x, y, arm.ShoulderX, arm.ShoulderY, arm.HandX, arm.HandY);
                        if (armDist <= armRadius)
                        {
                            color = FurColor;
                        }

                        if (Distance(x, y, arm.HandX, arm.HandY) <= armRadius + 0.3)
                        {
                            color = FurEdgeColor; // 손
                        }
                    }

                    // 몸통(타원)
                    double torsoNorm = Math.Pow((x - torsoCx) / torsoRx, 2) + Math.Pow((y - torsoCy) / torsoRy, 2);
                    if (torsoNorm <= 1.0)
                    {
                        color = FurColor;
                    }

                    if (Distance(x, y, bellyCx, bellyCy) <= bellyRadius)
                    {
                        color = FaceColor;
                    }

                    // 귀
                    foreach (var ear in ears)
                    {
                        if (Distance(x, y, ear.X, ear.Y) <= ear.R)
                        {
                            color = FurColor;
                        }
                    }

                    // 머리
                    double headDist = Distance(x, y, headCx, headCy);
                    if (headDist <= headRadius - 1.2)
                    {
                        color = FurColor;
                    }
                    else if (headDist <= headRadius)
                    {
                        color = FurEdgeColor;
                    }

                    if (color != null && Distance(x, y, faceCx, faceCy) <= faceRadius)
                    {
                        color = FaceColor;
                    }

                    // 눈
                    bool isLeftEye = (col == 7 || col == 8) && (row == 7 || row == 8);
                    bool isRightEye = (col == 13 || col == 14) && (row == 7 || row == 8);

                    if (isLeftEye || isRightEye)
                    {
                        bool isUpperEyeRow = row == 7;
                        color = blink && isUpperEyeRow ? FaceColor : DarkColor;
                    }

                    // 코/입
                    bool isNose = row == 10 && (col == 10 || col == 11);
                    bool isMouth = mouthOpen
                        ? (row == 11 || row == 12) && col >= 9 && col <= 12
                        : row == 12 && col >= 9 && col <= 12;

                    if (isNose || isMouth)
                    {
                        color = DarkColor;
                    }

                    grid.Children.Add(new Rectangle
                    {
                        Width = FaceCellSize,
                        Height = FaceCellSize,
                        Fill = color.HasValue ? new SolidColorBrush(color.Value) : Brushes.Transparent
                    });
                }
            }

            return grid;
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // 점(px,py)에서 선분(ax,ay)-(bx,by)까지의 최단 거리. 팔처럼 길쭉한 캡슐 모양을 그릴 때 사용한다.
        private static double DistanceToSegment(double px, double py, double ax, double ay, double bx, double by)
        {
            double abx = bx - ax;
            double aby = by - ay;
            double abLenSq = abx * abx + aby * aby;

            double t = abLenSq > 0 ? ((px - ax) * abx + (py - ay) * aby) / abLenSq : 0;
            t = Math.Max(0, Math.Min(1, t));

            double closestX = ax + t * abx;
            double closestY = ay + t * aby;
            return Distance(px, py, closestX, closestY);
        }
    }
}
