using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace HospitalApp
{
    public partial class ProfilePage : Page
    {
        private int _userId;
        private DateTime _startTime;
        private DispatcherTimer _timer;

        public ProfilePage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _startTime = DateTime.Now;
            DataContext = this;
            LoadUserData();
            SetupTimer();
        }

        public string FullName { get; set; }
        public string Role { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Login { get; set; }
        public string CurrentTime => DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        public string Uptime => (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss");

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
                }
                else
                {
                    MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService?.Navigate(new AuthorizePage());
                }
            }
        }

        private void SetupTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) =>
            {
                CurrentTimeText.Text = CurrentTime;
                UptimeText.Text = Uptime;
            };
            _timer.Start();
        }

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

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            UserData.CurrentUserId = 0;
            NavigationService?.Navigate(new AuthorizePage());
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}