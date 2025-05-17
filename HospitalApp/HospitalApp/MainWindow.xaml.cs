using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace HospitalApp
{
    public class AppUser // Переименован из User для избежания конфликта с EF User
    {
        public string RoleName { get; set; } // Используем строку вместо Role
        public string FullName { get; set; }
        public string ProfileIconPath { get; set; }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private Visibility _headerVisibility = Visibility.Hidden;
        private Visibility _menuVisibility = Visibility.Hidden;
        private string _userFullName;
        private string _userProfileIcon;
        private AppUser _currentUser;

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

        public AppUser CurrentUser
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

            UserFullName = _currentUser.FullName;
            switch (_currentUser.RoleName) // Используем RoleName вместо Role
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

    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
            Manager.MainFrame = MainFrame;
            MainFrame.Navigate(new AuthorizePage());
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
            MainFrame.Navigate(new ProfilePage()); // Обновим позже, когда создадим ProfilePage
        }

        public void AuthorizeUser(AppUser user)
        {
            _viewModel.CurrentUser = user;
            MainFrame.Navigate(new MainPage(UserData.CurrentUserRole)); // Передаем роль из UserData
        }
    }
}