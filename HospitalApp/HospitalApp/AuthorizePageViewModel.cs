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

        public bool IsPasswordNotEmpty
        {
            get => _isPasswordNotEmpty;
            set
            {
                _isPasswordNotEmpty = value;
                OnPropertyChanged(nameof(IsPasswordNotEmpty));
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
}
