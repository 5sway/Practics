using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HospitalApp
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private Visibility _headerVisibility = Visibility.Hidden;
        private Visibility _menuVisibility = Visibility.Hidden;
        private string _userFullName;
        private string _userProfileIcon;
        private User _currentUser;

        public Visibility HeaderVisibility
        {
            get => _headerVisibility;
            set
            {
                _headerVisibility = value;
                OnPropertyChanged(nameof(HeaderVisibility));
            }
        }

        public Visibility MenuVisibility
        {
            get => _menuVisibility;
            set
            {
                _menuVisibility = value;
                OnPropertyChanged(nameof(MenuVisibility));
            }
        }

        public string UserFullName
        {
            get => _userFullName;
            set
            {
                _userFullName = value;
                OnPropertyChanged(nameof(UserFullName));
            }
        }

        public string UserProfileIcon
        {
            get => _userProfileIcon;
            set
            {
                _userProfileIcon = value;
                OnPropertyChanged(nameof(UserProfileIcon));
            }
        }

        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                UpdateUserProfile();
            }
        }

        private void UpdateUserProfile()
        {
            if (_currentUser == null)
            {
                UserFullName = string.Empty;
                UserProfileIcon = null;
                return;
            }

            UserFullName = _currentUser.Full_Name;
            switch (_currentUser.Role?.Name ?? "Default")
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
                case "Admin":
                    UserProfileIcon = "/Resources/AdminIcon.png";
                    break;
                case "Doctor":
                    UserProfileIcon = "/Resources/DoctorIcon.png";
                    break;
                case "Patient":
                    UserProfileIcon = "/Resources/PatientIcon.png";
                    break;
                default:
                    UserProfileIcon = "/Resources/DefaultIcon.png";
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
