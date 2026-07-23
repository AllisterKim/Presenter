using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Presenter
{
    public partial class TimerWindow : Window
    {
        // 남은 초가 바뀔 때마다 알려준다(10초 남았을 때 경고 표시 등에 사용).
        public event Action<int>? SecondsRemainingChanged;

        // 0초가 되는 순간 한 번 호출된다.
        public event Action? TimeUp;

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private int _remainingSeconds;

        public TimerWindow()
        {
            InitializeComponent();
            _timer.Tick += Timer_Tick;
            Closed += (_, _) => _timer.Stop();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Opacity = e.NewValue;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(MinutesTextBox.Text, out int minutes) || minutes <= 0)
            {
                return;
            }

            _remainingSeconds = minutes * 60;
            UpdateCountdownText();

            InputPanel.Visibility = Visibility.Collapsed;
            CountdownPanel.Visibility = Visibility.Visible;

            _timer.Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            InputPanel.Visibility = Visibility.Visible;
            CountdownPanel.Visibility = Visibility.Collapsed;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _remainingSeconds--;

            if (_remainingSeconds <= 0)
            {
                _timer.Stop();
                _remainingSeconds = 0;
                UpdateCountdownText();
                SecondsRemainingChanged?.Invoke(0);
                TimeUp?.Invoke();
                return;
            }

            UpdateCountdownText();
            SecondsRemainingChanged?.Invoke(_remainingSeconds);
        }

        private void UpdateCountdownText()
        {
            int minutes = _remainingSeconds / 60;
            int seconds = _remainingSeconds % 60;
            CountdownText.Text = $"{minutes:00}:{seconds:00}";
        }
    }
}
