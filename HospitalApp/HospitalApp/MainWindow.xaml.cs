using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace HospitalApp
{
    public partial class MainWindow : Window
    {
        public readonly MainWindowViewModel _viewModel;
        private DispatcherTimer _sessionTimer;

        // Конструктор главного окна
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
            Manager.MainFrame = MainFrame;
            try
            {
                int lastUserId = Properties.Settings.Default.LastUserId;
                DateTime lastLoginTime = Properties.Settings.Default.LastLoginTime;
                if (lastUserId > 0 && (DateTime.Now - lastLoginTime).TotalSeconds <= 5400)
                {
                    var user = HospitalBaseEntities.GetContext().User
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.User_Id == lastUserId);
                    if (user != null)
                    {
                        AuthorizeUser(user);
                        MainFrame.Navigate(new MainPage(user.Role.Name));
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка автоматической авторизации: {ex.Message}");
            }

            // Показать страницу авторизации
            MainFrame.Navigate(new AuthorizePage());
            Activated += Window_Activated;
            _sessionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _sessionTimer.Tick += SessionTimer_Tick;
            _sessionTimer.Start();
        }

        // Таймер для отслеживания времени сессии
        private void SessionTimer_Tick(object sender, EventArgs e)
        {
            if (_viewModel.CurrentUser == null) return;
            DateTime lastLoginTime = Properties.Settings.Default.LastLoginTime;
            if ((DateTime.Now - lastLoginTime).TotalSeconds > 5400)
            {
                Dispatcher.Invoke(() => ForceLogout());
            }
        }

        // Принудительный выход из системы по истечении сессии
        private void ForceLogout()
        {
            _sessionTimer.Stop();
            Properties.Settings.Default.LastUserId = 0;
            Properties.Settings.Default.LastLoginTime = DateTime.MinValue;
            Properties.Settings.Default.Save();
            _viewModel.CurrentUser = null;
            MainFrame.Navigate(new AuthorizePage());
            MessageBox.Show("Ваша сессия истекла. Пожалуйста, войдите снова.", "Сессия завершена",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Обработчик активации окна (для перезапуска анимаций)
        private void Window_Activated(object sender, EventArgs e)
        {
            if (MainFrame.Content is AuthorizePage authorizePage)
            {
                try
                {
                    var togglePasswordIcon = (Image)authorizePage.FindName("TogglePasswordIcon");
                    if (togglePasswordIcon != null)
                    {
                        string gifPath = (string)authorizePage.GetType().GetField("_currentGifPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(authorizePage);
                        if (string.IsNullOrEmpty(gifPath))
                        {
                            return;
                        }
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(gifPath, UriKind.Relative);
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        Dispatcher.Invoke(() =>
                        {
                            ImageBehavior.SetAutoStart(togglePasswordIcon, false);
                            ImageBehavior.SetAnimatedSource(togglePasswordIcon, bitmap);
                            ImageBehavior.SetAnimationSpeedRatio(togglePasswordIcon, 2.0);
                            ImageBehavior.SetAutoStart(togglePasswordIcon, true);
                            togglePasswordIcon.InvalidateVisual();
                        }, DispatcherPriority.Render);
                        var startTimer = (DispatcherTimer)authorizePage.GetType().GetField("_gifStartTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(authorizePage);
                        startTimer?.Stop();
                        startTimer?.Start();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при перезапуске GIF: {ex.Message}");
                }
            }
        }

        // Обработчик навигации по страницам
        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is AuthorizePage)
            {
                _viewModel.HeaderVisibility = Visibility.Hidden;
                _viewModel.MenuVisibility = Visibility.Hidden;
            }
            else if (e.Content is ProfilePage)
            {
                _viewModel.HeaderVisibility = Visibility.Visible;
                ProfileBorder.Visibility = Visibility.Hidden;
                _viewModel.MenuVisibility = Visibility.Visible;
            }
            else
            {
                _viewModel.HeaderVisibility = Visibility.Visible;
                ProfileBorder.Visibility = Visibility.Visible;
                _viewModel.MenuVisibility = Visibility.Visible;
            }

        }

        // Переход на страницу профиля
        private void ProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProfilePage(UserData.CurrentUserId));
        }

        // Метод авторизации пользователя
        public void AuthorizeUser(User user)
        {
            _viewModel.CurrentUser = user;
            UserData.CurrentUserId = user.User_Id;
            UserData.CurrentUserRole = user.Role.Name;
            UserData.CurrentUserName = user.Full_Name;

            using (var context = new HospitalBaseEntities())
            {
                var dbUser = context.User
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.User_Id == user.User_Id);
                if (dbUser != null)
                {
                    MainFrame.Navigate(new MainPage(dbUser.Role.Name));
                }
                else
                {
                    MessageBox.Show("Пользователь не найден в базе данных!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    MainFrame.Navigate(new AuthorizePage());
                    return;
                }
            }
            Properties.Settings.Default.LastUserId = user.User_Id;
            Properties.Settings.Default.LastLoginTime = DateTime.Now;
            Properties.Settings.Default.Save();
        }

        // Выход из системы
        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Подтверждение выхода",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Properties.Settings.Default.LastUserId = 0;
                Properties.Settings.Default.LastLoginTime = DateTime.MinValue;
                Properties.Settings.Default.Save();
                _viewModel.CurrentUser = null;
                MainFrame.Navigate(new AuthorizePage());
            }
        }
    }
}