using System;
using System.Windows;
using System.Windows.Threading;

namespace CL_CLegendary_Launcher_.Class
{
    public class GameSessionManager
    {
        private DispatcherTimer _gameTimer;
        private string _currentMode;

        public void StartGameSession(string mode)
        {
            _currentMode = mode;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_gameTimer != null) return; 

                _gameTimer = new DispatcherTimer();
                _gameTimer.Interval = TimeSpan.FromMinutes(1);
                _gameTimer.Tick += GameTimer_Tick;
                _gameTimer.Start();
            });
        }

        public void StopGameSession()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_gameTimer != null)
                {
                    _gameTimer.Stop();
                    _gameTimer.Tick -= GameTimer_Tick;
                    _gameTimer = null;

                    Settings1.Default.Save();
                }
            });
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            double oneMinuteInHours = 1.0 / 60.0;

            switch (_currentMode)
            {
                case "vanilla":
                    Settings1.Default.StatsGameVanila += oneMinuteInHours;
                    break;
                case "mod":
                    Settings1.Default.StatsGameMod += oneMinuteInHours;
                    break;
                case "server":
                    Settings1.Default.StatsGameServer += oneMinuteInHours;
                    break;
            }
            Settings1.Default.Save();
        }

        public string GetFormattedStats()
        {
            string vanillaTime = FormatTime(Settings1.Default.StatsGameVanila);
            string modTime = FormatTime(Settings1.Default.StatsGameMod);
            string serverTime = FormatTime(Settings1.Default.StatsGameServer);

            return $"Статистика:\n" +
                   $"Ваніла: {vanillaTime};\n" +
                   $"Модові: {modTime};\n" +
                   $"Сервер: {serverTime}";
        }

        private string FormatTime(double hours)
        {
            int totalMinutes = (int)Math.Round(hours * 60);

            TimeSpan ts = TimeSpan.FromMinutes(totalMinutes);

            if (ts.TotalHours >= 1)
            {
                if (ts.Minutes > 0)
                    return $"{(int)ts.TotalHours} год {ts.Minutes} хв";
                else
                    return $"{(int)ts.TotalHours} год";
            }
            else
            {
                return $"{ts.Minutes} хв";
            }
        }
    }
}