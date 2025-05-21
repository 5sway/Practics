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
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
            Manager.MainFrame = MainFrame;

            // Проверка автоматической авторизации
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
                        System.Diagnostics.Debug.WriteLine($"Auto-login: UserId={user.User_Id}, Last_Login_Date={user.Last_Login_Date}");
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
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (MainFrame.Content is AuthorizePage authorizePage)
            {
                try
                {
                    var togglePasswordIcon = (Image)authorizePage.FindName("TogglePasswordIcon");
                    if (togglePasswordIcon != null)
                    {
                        // Получаем текущую GIF или null (если статическое изображение)
                        string gifPath = (string)authorizePage.GetType().GetField("_currentGifPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(authorizePage);
                        if (string.IsNullOrEmpty(gifPath))
                        {
                            // Статическое изображение, ничего не делаем
                            System.Diagnostics.Debug.WriteLine("Window activated, static image: /Resources/eye_open.png");
                            return;
                        }

                        // Перезапускаем анимацию
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
                            ImageBehavior.SetAnimationSpeedRatio(togglePasswordIcon, 2.0); // 2x скорость
                            // Попытка установить начальный кадр
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"Set GIF start frame to 1: {gifPath}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Ошибка установки начального кадра: {ex.Message}. Модифицируйте GIF, удалив первый кадр.");
                            }
                            ImageBehavior.SetAutoStart(togglePasswordIcon, true);
                            togglePasswordIcon.InvalidateVisual();
                            System.Diagnostics.Debug.WriteLine($"GIF animation restarted with path: {gifPath}, Speed: 2.0");
                        }, DispatcherPriority.Render);

                        // Запускаем таймер для повторного старта
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

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is AuthorizePage)
            {
                _viewModel.HeaderVisibility = Visibility.Hidden;
                _viewModel.MenuVisibility = Visibility.Hidden;
            }
            else
            {
                _viewModel.HeaderVisibility = Visibility.Visible;
                _viewModel.MenuVisibility = Visibility.Visible;
            }
        }

        private void ProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProfilePage());
        }

        public void AuthorizeUser(User user)
        {
            _viewModel.CurrentUser = user;
            UserData.CurrentUserId = user.User_Id;
            UserData.CurrentUserRole = user.Role.Name;
            UserData.CurrentUserName = user.Full_Name;

            // Используем новый контекст для проверки и навигации
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

            // Сохраняем данные для автоматической авторизации
            Properties.Settings.Default.LastUserId = user.User_Id;
            Properties.Settings.Default.LastLoginTime = DateTime.Now;
            Properties.Settings.Default.Save();
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ExitBtn_Click triggered.");
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Подтверждение выхода",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Сброс сохраненных данных авторизации
                Properties.Settings.Default.LastUserId = 0;
                Properties.Settings.Default.LastLoginTime = DateTime.MinValue;
                Properties.Settings.Default.Save();

                // Сброс текущего пользователя
                _viewModel.CurrentUser = null;

                // Переход на страницу авторизации
                MainFrame.Navigate(new AuthorizePage());

                System.Diagnostics.Debug.WriteLine("User logged out, credentials reset");
            }
        }
    }
}