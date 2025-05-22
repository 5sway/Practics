using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
        private bool _isPasswordNotEmpty = false;

        // Видимость полей ввода
        public Visibility InputVisibility
        {
            get => _inputVisibility;
            set
            {
                _inputVisibility = value;
                OnPropertyChanged(nameof(InputVisibility));
            }
        }

        // Видимость капчи
        public Visibility CaptchaVisibility
        {
            get => _captchaVisibility;
            set
            {
                _captchaVisibility = value;
                OnPropertyChanged(nameof(CaptchaVisibility));
            }
        }

        // Видимость сообщения об ошибке
        public Visibility ErrorVisibility
        {
            get => _errorVisibility;
            set
            {
                _errorVisibility = value;
                OnPropertyChanged(nameof(ErrorVisibility));
            }
        }

        // Текст сообщения об ошибке
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        // Доступность полей ввода
        public bool IsInputEnabled
        {
            get => _isInputEnabled;
            set
            {
                _isInputEnabled = value;
                OnPropertyChanged(nameof(IsInputEnabled));
            }
        }

        // Флаг наличия текста в поле пароля
        public bool IsPasswordNotEmpty
        {
            get => _isPasswordNotEmpty;
            set
            {
                _isPasswordNotEmpty = value;
                OnPropertyChanged(nameof(IsPasswordNotEmpty));
            }
        }

        // Количество неудачных попыток входа (без уведомления)
        public int FailedAttempts
        {
            get => _failedAttempts;
            set => _failedAttempts = value;
        }

        // Время до окончания "периода спокойствия" капчи (без уведомления)
        public DateTime? CaptchaGraceUntil
        {
            get => _captchaGraceUntil;
            set => _captchaGraceUntil = value;
        }

        // Длительность "периода спокойствия" капчи (только чтение)
        public TimeSpan CaptchaGracePeriod => _captchaGracePeriod;

        // Время до разблокировки ввода (без уведомления)
        public DateTime? InputLockUntil
        {
            get => _inputLockUntil;
            set => _inputLockUntil = value;
        }

        // Длительность блокировки ввода (только чтение)
        public TimeSpan InputLockPeriod => _inputLockPeriod;

        // Событие изменения свойства
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для вызова события PropertyChanged
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
