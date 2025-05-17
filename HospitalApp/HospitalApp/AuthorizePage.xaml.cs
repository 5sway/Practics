using System;
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
    public class AuthorizePageViewModel : INotifyPropertyChanged
    {
        private Visibility _inputVisibility = Visibility.Visible;
        private Visibility _captchaVisibility = Visibility.Hidden;
        private Visibility _errorVisibility = Visibility.Hidden;
        private string _errorMessage = "";
        private bool _isInputEnabled = true;
        private int _failedAttempts = 0;
        private DateTime? _captchaGraceUntil = null;
        private readonly TimeSpan _captchaGracePeriod = TimeSpan.FromMinutes(1);
        private DateTime? _inputLockUntil = null;
        private readonly TimeSpan _inputLockPeriod = TimeSpan.FromMinutes(10);

        public Visibility InputVisibility
        {
            get => _inputVisibility;
            set
            {
                _inputVisibility = value;
                OnPropertyChanged(nameof(InputVisibility));
            }
        }

        public Visibility CaptchaVisibility
        {
            get => _captchaVisibility;
            set
            {
                _captchaVisibility = value;
                OnPropertyChanged(nameof(CaptchaVisibility));
            }
        }

        public Visibility ErrorVisibility
        {
            get => _errorVisibility;
            set
            {
                _errorVisibility = value;
                OnPropertyChanged(nameof(ErrorVisibility));
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        public bool IsInputEnabled
        {
            get => _isInputEnabled;
            set
            {
                _isInputEnabled = value;
                OnPropertyChanged(nameof(IsInputEnabled));
            }
        }

        public int FailedAttempts
        {
            get => _failedAttempts;
            set => _failedAttempts = value;
        }

        public DateTime? CaptchaGraceUntil
        {
            get => _captchaGraceUntil;
            set => _captchaGraceUntil = value;
        }

        public TimeSpan CaptchaGracePeriod => _captchaGracePeriod;

        public DateTime? InputLockUntil
        {
            get => _inputLockUntil;
            set => _inputLockUntil = value;
        }

        public TimeSpan InputLockPeriod => _inputLockPeriod;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class AuthorizePage : Page
    {
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

        public AuthorizePage()
        {
            InitializeComponent();
            _viewModel = new AuthorizePageViewModel();
            DataContext = _viewModel;
            SetupInitialState();
            ResetLoginUI();
            timeBeginPeriod(1);

            PasswordTextBoxVisible.TextChanged += (s, e) => UpdatePlaceholderVisibility();

            try
            {
                ImageBehavior.SetAutoStart(TogglePasswordIcon, false);
                ImageBehavior.SetAnimatedSource(TogglePasswordIcon, new BitmapImage(new Uri("/Resources/eye_closed_open.gif", UriKind.Relative)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации GIF: {ex.Message}");
            }
        }

        ~AuthorizePage()
        {
            timeEndPeriod(1);
        }

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
                Interval = TimeSpan.FromSeconds(1) // Предполагаемая длительность GIF
            };
            _gifStopTimer.Tick += (s, e) =>
            {
                if (!_isPasswordVisible)
                    ImageBehavior.SetAutoStart(TogglePasswordIcon, false);
                _gifStopTimer.Stop();
            };
        }

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

        private void TogglePasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            try
            {
                if (_isPasswordVisible)
                {
                    PasswordTextBoxVisible.Text = PasswordTextBox.Password;
                    PasswordTextBox.Visibility = Visibility.Collapsed;
                    PasswordTextBoxVisible.Visibility = Visibility.Visible;
                    // Запуск GIF с начала
                    ImageBehavior.SetAutoStart(TogglePasswordIcon, false);
                    ImageBehavior.SetAnimatedSource(TogglePasswordIcon, null);
                    ImageBehavior.SetAnimatedSource(TogglePasswordIcon, new BitmapImage(new Uri("/Resources/eye_closed_open.gif", UriKind.Relative)));
                    ImageBehavior.SetAutoStart(TogglePasswordIcon, true);
                    _gifStopTimer.Stop(); // Остановить таймер, если был запущен
                }
                else
                {
                    PasswordTextBox.Password = PasswordTextBoxVisible.Text;
                    PasswordTextBoxVisible.Visibility = Visibility.Collapsed;
                    PasswordTextBox.Visibility = Visibility.Visible;
                    // Запуск GIF и остановка через таймер
                    ImageBehavior.SetAutoStart(TogglePasswordIcon, false);
                    ImageBehavior.SetAnimatedSource(TogglePasswordIcon, null);
                    ImageBehavior.SetAnimatedSource(TogglePasswordIcon, new BitmapImage(new Uri("/Resources/eye_closed_open.gif", UriKind.Relative)));
                    ImageBehavior.SetAutoStart(TogglePasswordIcon, true);
                    _gifStopTimer.Start(); // Запустить таймер для остановки
                }
                UpdatePlaceholderVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переключении GIF: {ex.Message}");
            }
        }

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
                ErrorText.Margin = new Thickness(0, 110, 140, 0);
            }
            else
            {
                ErrorText.Margin = new Thickness(0, 200, 140, 0);
            }

            _errorTimer.Start();
        }

        private void HideError()
        {
            _viewModel.ErrorVisibility = Visibility.Collapsed;
            _viewModel.ErrorMessage = "";
            ErrorText.FontSize = 14;
            ErrorText.Margin = new Thickness(0, 0, 0, 0);
        }

        private void GenerateNewCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText();
            _captchaImage = CaptchaGenerator.GenerateCaptchaImage(_captchaText);
            CaptchaImage.Source = _captchaImage;

            ImageBehavior.SetAnimatedSource(RefreshGif, null);
            ImageBehavior.SetAnimatedSource(RefreshGif, new BitmapImage(new Uri("/Resources/loader (1).gif", UriKind.Relative)));
            ImageBehavior.SetAutoStart(RefreshGif, false);
        }

        private void HideCaptchaUI()
        {
            _viewModel.CaptchaVisibility = Visibility.Collapsed;
            CaptchaTextBox.Clear();
            CaptchaText.Visibility = Visibility.Visible;
            _viewModel.InputVisibility = Visibility.Visible;
            HideError();
            UpdatePlaceholderVisibility();
        }

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

        private void RequestCaptcha()
        {
            GenerateNewCaptcha();
            _viewModel.InputVisibility = Visibility.Collapsed;
            _viewModel.CaptchaVisibility = Visibility.Visible;
            _viewModel.ErrorVisibility = Visibility.Collapsed;
        }

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

            UpdatePlaceholderVisibility();
        }

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

            UserData.CurrentUserId = user.User_Id;
            UserData.CurrentUserRole = user.Role.Name;
            UserData.CurrentUserName = user.Full_Name;

            ((MainWindow)Application.Current.MainWindow).AuthorizeUser(new AppUser
            {
                RoleName = user.Role.Name,
                FullName = user.Full_Name
            });

            ResetLoginUI();
        }

        private void UpdatePlaceholderVisibility()
        {
            LoginTxt.Visibility = string.IsNullOrWhiteSpace(LoginTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            PasswordTxt.Visibility = _isPasswordVisible
                ? string.IsNullOrWhiteSpace(PasswordTextBoxVisible.Text) ? Visibility.Visible : Visibility.Collapsed
                : string.IsNullOrWhiteSpace(PasswordTextBox.Password) ? Visibility.Visible : Visibility.Collapsed;
            CaptchaText.Visibility = string.IsNullOrWhiteSpace(CaptchaTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

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