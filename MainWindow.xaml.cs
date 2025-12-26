using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GreenhouseLightingSystem
{
    public partial class MainWindow : Window
    {
        // Класс для записей в журнале
        public class LogEntry
        {
            public string Time { get; set; }
            public string Message { get; set; }
        }

        // Для журнала событий
        public ObservableCollection<LogEntry> EventLog { get; set; }

        private DispatcherTimer dataTimer;
        private DispatcherTimer clockTimer;
        private Random random = new Random();

        // Текущее состояние системы
        private bool systemEnabled = true;
        private bool isLightOn = false;
        private string currentMode = "Авто"; // "Авто", "Ручной ВКЛ", "Ручной ВЫКЛ"

        // Данные освещения
        private double indoorLux = 0;
        private double outdoorLux = 0;
        private DateTime lastDataUpdate = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация 
            EventLog = new ObservableCollection<LogEntry>();
            EventLogListView.ItemsSource = EventLog;

            // Начальные записи в журнал
            AddToLog("Система управления освещением теплицы запущена");
            AddToLog("Режим работы: Автоматический");
            AddToLog("Дневное время: 07:00 - 21:00, пороги: днем 2500 лк, ночью 500 лк");
            AddToLog("ОРС-сервер: подключено");

            // Таймер для часов обновляется каждую секунду
            clockTimer = new DispatcherTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();

            // Таймер для обновления данных каждые 3 секунды
            dataTimer = new DispatcherTimer();
            dataTimer.Interval = TimeSpan.FromSeconds(3);
            dataTimer.Tick += DataTimer_Tick;
            dataTimer.Start();

            UpdateUI();
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void DataTimer_Tick(object sender, EventArgs e)
        {
            if (!systemEnabled)
            {
                ConnectionStatus.Text = "СИСТЕМА ОТКЛЮЧЕНА";
                return;
            }

            // Обновляем данные освещения
            UpdateLightData();

            // Применяем логику управления в зависимости от режима
            ApplyControlLogic();

            // Обновляем интерфейс
            UpdateUI();
        }

        private void UpdateLightData()
        {
            int hour = DateTime.Now.Hour;
            bool isDayTime = hour >= 7 && hour < 21;

            // Более реалистичная генерация данных
            if (isDayTime)
            {
                // Дневные значения с учетом времени суток
                if (hour >= 11 && hour <= 15)
                {
                    indoorLux = random.Next(2000, 4000); // Пик дня
                }
                else if (hour >= 7 && hour <= 10)
                {
                    indoorLux = random.Next(800, 2500); // Утро
                }
                else
                {
                    indoorLux = random.Next(500, 2000); // Вечер
                }
                outdoorLux = random.Next(10000, 100000);
            }
            else
            {
                // Ночные значения
                if (hour >= 21 && hour <= 23)
                {
                    indoorLux = random.Next(100, 600); // Ранняя ночь
                }
                else
                {
                    indoorLux = random.Next(50, 400); // Поздняя ночь/раннее утро
                }
                outdoorLux = random.Next(0, 100);
            }

            // Небольшие случайные колебания
            indoorLux += random.Next(-50, 50);
            outdoorLux += random.Next(-1000, 1000);
            indoorLux = Math.Max(0, indoorLux);
            outdoorLux = Math.Max(0, outdoorLux);

            lastDataUpdate = DateTime.Now;
        }

        private void ApplyControlLogic()
        {
            int hour = DateTime.Now.Hour;
            bool isDayTime = hour >= 7 && hour < 21;
            double threshold = isDayTime ? 2500 : 500;

            switch (currentMode)
            {
                case "Авто":
                    // Автоматический режим 
                    if (indoorLux > threshold)
                    {
                        if (isLightOn)
                        {
                            isLightOn = false;
                            AddToLog($"Авто: Освещение выключено ({indoorLux:F0} лк > порога {threshold} лк)");
                        }
                    }
                    else
                    {
                        if (!isLightOn)
                        {
                            isLightOn = true;
                            AddToLog($"Авто: Освещение включено ({indoorLux:F0} лк < порога {threshold} лк)");
                        }
                    }
                    break;

                case "Ручной ВКЛ":
                    // Ручной режим ВКЛ
                    if (!isLightOn)
                    {
                        isLightOn = true;
                        AddToLog("Ручное управление: освещение включено");
                    }
                    break;

                case "Ручной ВЫКЛ":
                    // Ручной режим ВЫКЛ
                    if (isLightOn)
                    {
                        isLightOn = false;
                        AddToLog("Ручное управление: освещение выключено");
                    }
                    break;
            }
        }

        private void UpdateUI()
        {
            // Обновляем текстовые поля
            IndoorLuxText.Text = $"{indoorLux:F0} лк";
            OutdoorLuxText.Text = $"{outdoorLux:F0} лк";

            // Прогресс бар
            LightProgress.Maximum = 4000;
            LightProgress.Value = Math.Min(indoorLux, 4000);

            // Статус системы
            string lightStatus = isLightOn ? "ВКЛ" : "ВЫКЛ";
            string timeInfo = DateTime.Now.Hour >= 7 && DateTime.Now.Hour < 21 ? "ДЕНЬ" : "НОЧЬ";
            StatusText.Text = $"Режим: {currentMode}\n" +
                             $"Освещение: {lightStatus}\n" +
                             $"Время суток: {timeInfo}\n" +
                             $"Обновлено: {lastDataUpdate:HH:mm:ss}";

            // Цветовая индикация освещенности
            int hour = DateTime.Now.Hour;
            bool isDayTime = hour >= 7 && hour < 21;
            double threshold = isDayTime ? 2500 : 500;

            if (indoorLux > threshold)
                IndoorLuxText.Foreground = Brushes.Red;
            else if (indoorLux < threshold * 0.5)
                IndoorLuxText.Foreground = Brushes.Orange;
            else
                IndoorLuxText.Foreground = Brushes.Green;

            // Цвет прогресс бара
            if (isLightOn)
                LightProgress.Foreground = Brushes.LimeGreen;
            else
                LightProgress.Foreground = Brushes.Red;

            // Статус подключения в footer
            ConnectionStatus.Text = $"ОРС: OK | Режим: {currentMode} | Освещение: {lightStatus} | {lastDataUpdate:HH:mm}";
        }

        private void AddToLog(string message)
        {
            // Проверяем не дублируется ли сообщение
            if (EventLog.Count > 0 &&
                EventLog[0].Message.Contains(message.Substring(0, Math.Min(20, message.Length))))
                return;

            // Добавляем запись в начало списка
            EventLog.Insert(0, new LogEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Message = message
            });

            // Ограничиваем количество записей
            if (EventLog.Count > 150)
            {
                EventLog.RemoveAt(EventLog.Count - 1);
            }
        }

        // ОБРАБОТЧИКИ КНОПОК 

        private void TurnOnButton_Click(object sender, RoutedEventArgs e)
        {
            if (!systemEnabled) return;

            currentMode = "Ручной ВКЛ";
            isLightOn = true;

            AddToLog("ОПЕРАТОР: Принудительное включение освещения");
            AddToLog("ВНИМАНИЕ: Автоматическое управление отключено");

            UpdateUI();
        }

        private void TurnOffButton_Click(object sender, RoutedEventArgs e)
        {
            if (!systemEnabled) return;

            currentMode = "Ручной ВЫКЛ";
            isLightOn = false;

            AddToLog("ОПЕРАТОР: Принудительное выключение освещения");
            AddToLog("ВНИМАНИЕ: Автоматическое управление отключено");

            UpdateUI();
        }

        private void AutoModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!systemEnabled) return;

            currentMode = "Авто";

            AddToLog("ОПЕРАТОР: Включен автоматический режим");
            AddToLog("Система перешла на управление по ТЗ");

            UpdateUI();
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            EventLog.Clear();
            AddToLog("Журнал событий очищен оператором");
        }

        protected override void OnClosed(EventArgs e)
        {
            dataTimer?.Stop();
            clockTimer?.Stop();
            AddToLog("Система управления освещением остановлена");
            base.OnClosed(e);
        }
    }
}