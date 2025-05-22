﻿using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace HospitalApp
{
    public partial class AuthorizePage : Page
    {
        // Импорт функций WinAPI для управления таймером
        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint period);

        private readonly AuthorizePageViewModel _viewModel;
        private BitmapImage _captchaImage;
        private string _pendingLogin;
        private string _pendingPassword;
        private string _captchaText;
        private DispatcherTimer _errorTimer;
        private DispatcherTimer _smoothTimer;
        private bool _isPasswordVisible = false;
        private DispatcherTimer _gifStopTimer;
        private DispatcherTimer _gifStartTimer;
        private string _currentGifPath;

        // Конструктор страницы авторизации
        public AuthorizePage()
        {
            InitializeComponent();
            _viewModel = new AuthorizePageViewModel();
            DataContext = _viewModel;
            SetupInitialState();
            ResetLoginUI();
            timeBeginPeriod(1);

            PasswordTextBoxVisible.TextChanged += (s, e) => UpdatePasswordFieldState();
            PasswordTextBox.PasswordChanged += (s, e) => UpdatePasswordFieldState();

            try
            {
                _currentGifPath = "/Resources/eye_animation.gif";
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_currentGifPath, UriKind.Relative);
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ImageBehavior.SetAnimatedSource(TogglePasswordIcon, bitmap);
                ImageBehavior.SetAutoStart(TogglePasswordIcon, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки начальной GIF: {ex.Message}");
            }
        }

        // Деструктор (освобождение ресурсов таймера)
        ~AuthorizePage()
        {
            timeEndPeriod(1);
        }

        // Настройка начального состояния страницы
        private void SetupInitialState()
        {
            _errorTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _errorTimer.Tick += (s, e) =>
            {
                HideError();
                _errorTimer.Stop();
            };

            _smoothTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _smoothTimer.Tick += SmoothTimer_Tick;

            _gifStopTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromSeconds(1.0)
            };
            _gifStopTimer.Tick += (s, e) =>
            {
                ImageBehavior.SetAutoStart(TogglePasswordIcon, false);
                _gifStopTimer.Stop();
            };

            _gifStartTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _gifStartTimer.Tick += (s, e) =>
            {
                ImageBehavior.SetAutoStart(TogglePasswordIcon, true);
                _gifStartTimer.Stop();
            };
        }

        // Обновление состояния поля пароля (видимое/скрытое)
        private void UpdatePasswordFieldState()
        {
            string password = _isPasswordVisible ? PasswordTextBoxVisible.Text : PasswordTextBox.Password;
            _viewModel.IsPasswordNotEmpty = !string.IsNullOrEmpty(password);
            UpdatePlaceholderVisibility();
        }

        // Обработчик плавного таймера (для анимаций и блокировок)
        private void SmoothTimer_Tick(object sender, EventArgs e)
        {
            if (_viewModel.InputLockUntil.HasValue && DateTime.Now < _viewModel.InputLockUntil.Value)
            {
                var remaining = _viewModel.InputLockUntil.Value - DateTime.Now;
                _viewModel.ErrorMessage = $"Вход заблокирован. Разблокировка через {(int)remaining.TotalSeconds} сек.";
                _viewModel.ErrorVisibility = Visibility.Visible;
                return;
            }

            if (_viewModel.CaptchaGraceUntil.HasValue && DateTime.Now < _viewModel.CaptchaGraceUntil.Value)
            {
                var remaining = _viewModel.CaptchaGraceUntil.Value - DateTime.Now;
                if (_viewModel.ErrorVisibility == Visibility.Visible && _viewModel.ErrorMessage.Contains("Капча скрыта на"))
                {
                    var baseMessage = _viewModel.ErrorMessage.Split('(')[0].Trim();
                    var secondsLeft = (int)remaining.TotalSeconds;
                    _viewModel.ErrorMessage = $"{baseMessage} (Капча скрыта на {secondsLeft} сек)";
                }
                return;
            }

            _viewModel.FailedAttempts = 0;
            _viewModel.CaptchaGraceUntil = null;
            _viewModel.InputLockUntil = null;
            _viewModel.IsInputEnabled = true;
            _viewModel.ErrorVisibility = Visibility.Hidden;
            _smoothTimer.Stop();
        }

        // Переключение видимости пароля (кнопка "глаз")
        private void TogglePasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            try
            {
                System.Diagnostics.Debug.WriteLine($"Toggling password visibility: {_isPasswordVisible}");

                if (_isPasswordVisible)
                {
                    PasswordTextBoxVisible.Text = PasswordTextBox.Password;
                    PasswordTextBox.Visibility = Visibility.Collapsed;
                    PasswordTextBoxVisible.Visibility = Visibility.Visible;
                }
                else
                {
                    PasswordTextBox.Password = PasswordTextBoxVisible.Text;
                    PasswordTextBoxVisible.Visibility = Visibility.Collapsed;
                    PasswordTextBox.Visibility = Visibility.Visible;
                }
                string newGifPath = "/Resources/eye_animation.gif";
                var controller = ImageBehavior.GetAnimationController(TogglePasswordIcon);
                if (controller != null && controller.IsPaused == false)
                {
                    ImageBehavior.SetAnimatedSource(TogglePasswordIcon, null);
                }
                else
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(newGifPath, UriKind.Relative);
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImageBehavior.SetAnimatedSource(TogglePasswordIcon, bitmap);
                    ImageBehavior.SetRepeatBehavior(TogglePasswordIcon, new System.Windows.Media.Animation.RepeatBehavior(1));
                    ImageBehavior.SetAutoStart(TogglePasswordIcon, true);
                }

                UpdatePlaceholderVisibility();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при переключении GIF: {ex.Message}");
            }
        }

        // Отображение сообщения об ошибке
        private void ShowError(string message)
        {
            if (_errorTimer.IsEnabled)
                _errorTimer.Stop();

            if (_viewModel.InputLockUntil.HasValue && DateTime.Now < _viewModel.InputLockUntil.Value)
            {
                var remaining = _viewModel.InputLockUntil.Value - DateTime.Now;
                message = $"Вход заблокирован. Разблокировка через {(int)remaining.TotalSeconds} сек.";
            }
            else if (_viewModel.CaptchaGraceUntil.HasValue && DateTime.Now < _viewModel.CaptchaGraceUntil.Value)
            {
                var remaining = _viewModel.CaptchaGraceUntil.Value - DateTime.Now;
                var secondsLeft = (int)remaining.TotalSeconds;
                message += $" (Капча скрыта на {secondsLeft} сек)";
            }

            _viewModel.ErrorMessage = message;
            _viewModel.ErrorVisibility = Visibility.Visible;

            double baseFontSize = 14;
            int lineCount = message.Split('\n').Length;
            double newFontSize = baseFontSize;
            if (message.Length > 30 || lineCount > 1)
                newFontSize = Math.Max(10, baseFontSize - (message.Length / 20));
            ErrorText.FontSize = newFontSize;

            ErrorText.HorizontalAlignment = HorizontalAlignment.Center;
            ErrorText.VerticalAlignment = VerticalAlignment.Center;
            if (_viewModel.CaptchaVisibility == Visibility.Visible)
            {
                ErrorText.Margin = new Thickness(0, 110, 0, 0);
            }
            else
            {
                ErrorText.Margin = new Thickness(0, 180, 0, 0);
            }

            _errorTimer.Start();
        }

        // Скрытие сообщения об ошибке
        private void HideError()
        {
            _viewModel.ErrorVisibility = Visibility.Collapsed;
            _viewModel.ErrorMessage = "";
            ErrorText.FontSize = 14;
            ErrorText.Margin = new Thickness(0, 0, 0, 0);
        }

        // Генерация новой капчи
        private void GenerateNewCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText();
            _captchaImage = CaptchaGenerator.GenerateCaptchaImage(_captchaText);
            CaptchaImage.Source = _captchaImage;

            ImageBehavior.SetAnimatedSource(RefreshGif, null);
            ImageBehavior.SetAnimatedSource(RefreshGif, new BitmapImage(new Uri("/Resources/loader (1).gif", UriKind.Relative)));
            ImageBehavior.SetAutoStart(RefreshGif, false);
        }

        // Скрытие интерфейса капчи
        private void HideCaptchaUI()
        {
            _viewModel.CaptchaVisibility = Visibility.Collapsed;
            CaptchaTextBox.Clear();
            CaptchaText.Visibility = Visibility.Visible;
            _viewModel.InputVisibility = Visibility.Visible;
            HideError();
            UpdatePlaceholderVisibility();
        }

        // Проверка находится ли капча в "периоде спокойствия"
        private bool IsCaptchaInGracePeriod()
        {
            bool isInGracePeriod = _viewModel.CaptchaGraceUntil.HasValue && DateTime.Now < _viewModel.CaptchaGraceUntil.Value;
            if (!isInGracePeriod && _viewModel.CaptchaGraceUntil.HasValue && _viewModel.FailedAttempts >= 3)
            {
                _viewModel.FailedAttempts = 0;
                _viewModel.CaptchaGraceUntil = null;
            }
            return isInGracePeriod;
        }

        // Запрос отображения капчи
        private void RequestCaptcha()
        {
            GenerateNewCaptcha();
            _viewModel.InputVisibility = Visibility.Collapsed;
            _viewModel.CaptchaVisibility = Visibility.Visible;
            _viewModel.ErrorVisibility = Visibility.Collapsed;
        }

        // Проверка учетных данных (логин/пароль)
        private void VerifyCredentials()
        {
            string login = LoginTextBox.Text.Trim();
            string password = _isPasswordVisible ? PasswordTextBoxVisible.Text.Trim() : PasswordTextBox.Password.Trim();

            StringBuilder errorMessage = new StringBuilder();
            if (string.IsNullOrWhiteSpace(login)) errorMessage.AppendLine("Введите логин!");
            if (string.IsNullOrWhiteSpace(password)) errorMessage.AppendLine("Введите пароль!");

            if (errorMessage.Length > 0)
            {
                ShowError(errorMessage.ToString());
                return;
            }

                var user = HospitalBaseEntities.GetContext().User
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Login == login && u.Password == password);

            if (user == null || user.Password != password)
            {
                if (!IsCaptchaInGracePeriod())
                {
                    _viewModel.FailedAttempts++;
                    if (_viewModel.FailedAttempts >= 5)
                    {
                        _viewModel.InputLockUntil = DateTime.Now.Add(_viewModel.InputLockPeriod);
                        _viewModel.IsInputEnabled = false;
                        _smoothTimer.Start();
                        ShowError("Вход заблокирован. Разблокировка через 600 сек.");
                        return;
                    }
                }

                ShowError(user == null ? "Неверный логин!" : "Неверный пароль!");
                _pendingLogin = null;
                _pendingPassword = null;

                if (IsCaptchaInGracePeriod()) return;

                if (_viewModel.FailedAttempts >= 3)
                {
                    _pendingLogin = login;
                    _pendingPassword = password;
                    RequestCaptcha();
                }
                return;
            }

            _pendingLogin = login;
            _pendingPassword = password;
            AuthorizeUser();
        }

        // Авторизация с проверкой капчи
        private void AuthorizeWithCaptcha()
        {
            string enteredCaptcha = CaptchaTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(enteredCaptcha) || enteredCaptcha != _captchaText)
            {
                ShowError("Неверная капча! Попробуйте еще раз.");
                GenerateNewCaptcha();
                return;
            }

            _viewModel.CaptchaGraceUntil = DateTime.Now.Add(_viewModel.CaptchaGracePeriod);
            _smoothTimer.Start();

            if (string.IsNullOrWhiteSpace(_pendingLogin) || string.IsNullOrWhiteSpace(_pendingPassword))
            {
                ShowError("Введите логин и пароль заново!");
                _viewModel.FailedAttempts = 0;
                HideCaptchaUI();
                return;
            }

            var user = HospitalBaseEntities.GetContext().User
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Login == _pendingLogin && u.Password == _pendingPassword);

            if (user == null)
            {
                _viewModel.FailedAttempts++;
                ShowError("Неверный логин или пароль!");
                HideCaptchaUI();
                if (_viewModel.FailedAttempts >= 5)
                {
                    _viewModel.InputLockUntil = DateTime.Now.Add(_viewModel.InputLockPeriod);
                    _viewModel.IsInputEnabled = false;
                    _smoothTimer.Start();
                    ShowError("Вход заблокирован. Разблокировка через 600 сек.");
                }
                return;
            }

            HideCaptchaUI();
            _viewModel.FailedAttempts = 0;
            HideError();
            AuthorizeUser();
        }

        // Сброс UI авторизации
        private void ResetLoginUI(bool clearInputs = true)
        {
            HideCaptchaUI();
            HideError();
            if (_errorTimer.IsEnabled)
                _errorTimer.Stop();

            if (clearInputs)
            {
                LoginTextBox.Clear();
                PasswordTextBox.Clear();
                PasswordTextBoxVisible.Clear();
            }

            try
            {
                Dispatcher.Invoke(() =>
                {
                    ImageBehavior.SetAnimatedSource(TogglePasswordIcon, null);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сбросе GIF: {ex.Message}");
            }

            UpdatePlaceholderVisibility();

        }

        // Основной метод авторизации пользователя
        private void AuthorizeUser()
        {
            if (string.IsNullOrWhiteSpace(_pendingLogin) || string.IsNullOrWhiteSpace(_pendingPassword))
            {
                ShowError("Ошибка авторизации: данные отсутствуют!");
                return;
            }

            var user = HospitalBaseEntities.GetContext().User
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Login == _pendingLogin && u.Password == _pendingPassword);

            if (user == null)
            {
                ShowError("Ошибка авторизации: неверные данные!");
                _pendingLogin = null;
                _pendingPassword = null;
                return;
            }
            try
            {
                user.Last_Login_Date = DateTime.Now;
                HospitalBaseEntities.GetContext().SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления Last_Login_Date: {ex.Message}");
                ShowError("Ошибка сохранения данных входа!");
                return;
            }
            try
            {
                Properties.Settings.Default.LastUserId = user.User_Id;
                Properties.Settings.Default.LastLoginTime = DateTime.Now;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения настроек авторизации: {ex.Message}");
            }

            UserData.CurrentUserId = user.User_Id;
            UserData.CurrentUserRole = user.Role.Name;
            UserData.CurrentUserName = user.Full_Name;

            ((MainWindow)Application.Current.MainWindow).AuthorizeUser(user);

            ResetLoginUI();
        }

        // Обновление видимости плейсхолдеров (подсказок)
        private void UpdatePlaceholderVisibility()
        {
            LoginTxt.Visibility = string.IsNullOrWhiteSpace(LoginTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            PasswordTxt.Visibility = _isPasswordVisible
                ? string.IsNullOrWhiteSpace(PasswordTextBoxVisible.Text) ? Visibility.Visible : Visibility.Collapsed
                : string.IsNullOrWhiteSpace(PasswordTextBox.Password) ? Visibility.Visible : Visibility.Collapsed;
            CaptchaText.Visibility = string.IsNullOrWhiteSpace(CaptchaTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Обработчики событий клавиатуры:
        private void LoginTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                PasswordTextBox.Focus();
        }

        private void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                VerifyCredentials();
        }

        private void CaptchaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AuthorizeWithCaptcha();
        }

        // Обработчики фокуса элементов:
        private void LoginBox_GotFocus(object sender, RoutedEventArgs e)
        {
            LoginTxt.Visibility = Visibility.Collapsed;
        }

        private void LoginBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordTxt.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void CaptchaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CaptchaText.Visibility = Visibility.Collapsed;
        }

        private void CaptchaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }
        
        // Обработчики кликов по плейсхолдерам:
        private void LoginTxt_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoginTxt.Visibility = Visibility.Collapsed;
            LoginTextBox.Focus();
        }

        private void PasswordTxt_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PasswordTxt.Visibility = Visibility.Collapsed;
            if (_isPasswordVisible)
                PasswordTextBoxVisible.Focus();
            else
                PasswordTextBox.Focus();
        }

        private void CaptchaText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CaptchaText.Visibility = Visibility.Collapsed;
            CaptchaTextBox.Focus();
        }
        
        // Обработчики кнопок:
        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            VerifyCredentials();
        }

        private void CaptchaSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            AuthorizeWithCaptcha();
        }

        private void RefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            ImageBehavior.SetAutoStart(RefreshGif, true);
            GenerateNewCaptcha();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetLoginUI(true);
        }
    }
}