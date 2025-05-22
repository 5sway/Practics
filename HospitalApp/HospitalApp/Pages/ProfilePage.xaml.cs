using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using HospitalApp.Properties;

namespace HospitalApp
{
    public partial class ProfilePage : Page, INotifyPropertyChanged
    {
        private int _userId;
        private DateTime _sessionStartTime;
        private DispatcherTimer _timer;

        // Конструктор страницы профиля
        // Принимает ID пользователя и инициализирует компоненты
        public ProfilePage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            DataContext = this;
            LoadUserData();
            SetupTimer();
        }

        public string FullName { get; set; }
        public string Role { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Login { get; set; }
        public string UserProfileIcon { get; set; }

        private string _currentTime;

        // Свойство для отображения текущего времени
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged(nameof(CurrentTime));
            }
        }

        private string _uptime;

        // Свойство для отображения времени сессии (uptime)
        public string Uptime
        {
            get => _uptime;
            set
            {
                _uptime = value;
                OnPropertyChanged(nameof(Uptime));
            }
        }

        // Событие для уведомления об изменении свойств (реализация INotifyPropertyChanged)
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для вызова события PropertyChanged
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Загружает данные пользователя из базы данных
        private void LoadUserData()
        {
            using (var context = new HospitalBaseEntities())
            {
                var user = context.User
                    .Include("Role")
                    .FirstOrDefault(u => u.User_Id == _userId);
                if (user != null)
                {
                    FullName = user.Full_Name;
                    Role = user.Role?.Name ?? "Не указана";
                    LastLogin = user.Last_Login_Date;
                    Login = user.Login;
                    PswrdBox.Text = user.Password;
                    switch (user.Role?.Name ?? "Не указана")
                    {
                        case "Лаборант":
                            UserProfileIcon = "/Resources/laborant_2.png";
                            break;
                        case "Лаборант-Исследователь":
                            UserProfileIcon = "/Resources/laborant_1.jpeg";
                            break;
                        case "Лаборант-Администратор":
                            UserProfileIcon = "/Resources/Администратор.png";
                            break;
                        case "Бухгалтер":
                            UserProfileIcon = "/Resources/Бухгалтер.jpeg";
                            break;
                        default:
                            break;
                    }
                    DateTime lastLoginTime = Settings.Default.LastLoginTime == DateTime.MinValue ? DateTime.Now : Settings.Default.LastLoginTime;
                    DateTime? lastLoginDate = user.Last_Login_Date;
                    if (lastLoginDate.HasValue && lastLoginDate.Value < lastLoginTime)
                    {
                        _sessionStartTime = lastLoginDate.Value;
                    }
                    else
                    {
                        _sessionStartTime = lastLoginTime;
                    }

                    if (Settings.Default.LastLoginTime == DateTime.MinValue)
                    {
                        Settings.Default.LastLoginTime = _sessionStartTime;
                    }
                    Settings.Default.LastUserId = _userId;
                    Settings.Default.Save();
                }
                else
                {
                    MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService?.Navigate(new AuthorizePage());
                }
            }
        }

        // Настраивает таймер для обновления текущего времени и продолжительности сессии
        private void SetupTimer()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                TimeSpan sessionDuration = DateTime.Now - _sessionStartTime;
                Uptime = sessionDuration.ToString(@"hh\:mm\:ss");
            };
            _timer.IsEnabled = true;
            _timer.Start();
        }


        // Обработчик кнопки сохранения изменений профиля
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new HospitalBaseEntities())
            {
                var user = context.User.FirstOrDefault(u => u.User_Id == _userId);
                if (user != null)
                {
                    user.Full_Name = FullName;
                    user.Login = Login;
                    if (!string.IsNullOrWhiteSpace(PswrdBox.Text))
                    {
                        user.Password = PswrdBox.Text;
                    }
                    context.SaveChanges();
                    MessageBox.Show("Изменения сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Обработчик кнопки выхода из системы
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            Settings.Default.LastUserId = _userId;
            Settings.Default.Save();
            UserData.CurrentUserId = 0;
            NavigationService?.Navigate(new AuthorizePage());
        }

        // Обработчик кнопки возврата назад
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            Settings.Default.LastUserId = _userId;
            Settings.Default.Save();
            NavigationService.GoBack();
        }
    }
}