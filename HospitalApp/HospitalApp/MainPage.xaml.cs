using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Zen.Barcode;

namespace HospitalApp
{
    public partial class MainPage : Page, INotifyPropertyChanged
    {
        private string _currentRole;
        private Random _random = new Random();
        private bool _isTableFormat = true;
        private string _selectedFormat = "Word";
        private bool _allTablesSelected;
        private bool _patientsTableSelected;
        private bool _ordersTableSelected;
        private bool _servicesTableSelected;
        private bool _usersTableSelected;
        private List<Pacient> _patientRecords;
        private List<Order> _orderRecords;
        private List<Service> _serviceRecords;
        private List<User> _userRecords;
        private Pacient _selectedPatientRecord;
        private Order _selectedOrderRecord;
        private Service _selectedServiceRecord;
        private User _selectedUserRecord;
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        private DateTime _endDate = DateTime.Now;
        private string _suggestedBarcode;
        private List<Analyzer> _analyzers;
        private Visibility _adminButtonsVisibility = Visibility.Visible;
        private Visibility _isTableFormatVisible = Visibility.Visible;

        public bool IsTableFormat
        {
            get => _isTableFormat;
            set { _isTableFormat = value; OnPropertyChanged(nameof(IsTableFormat)); }
        }

        public string SelectedFormat
        {
            get => _selectedFormat;
            set
            {
                _selectedFormat = value;
                IsTableFormatVisible = value == "Excel" ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged(nameof(SelectedFormat));
            }
        }

        public bool AllTablesSelected
        {
            get => _allTablesSelected;
            set
            {
                _allTablesSelected = value;
                if (value)
                {
                    PatientsTableSelected = true;
                    OrdersTableSelected = true;
                    ServicesTableSelected = true;
                    UsersTableSelected = true;
                    // Сбрасываем выбор записей и скрываем ComboBox
                    SelectedPatientRecord = null;
                    SelectedOrderRecord = null;
                    SelectedServiceRecord = null;
                    SelectedUserRecord = null;
                }
                OnPropertyChanged(nameof(AllTablesSelected));
                UpdateComboBoxVisibility();
            }
        }

        public bool PatientsTableSelected
        {
            get => _patientsTableSelected;
            set
            {
                _patientsTableSelected = value;
                if (value && AllTablesSelected) AllTablesSelected = false;
                OnPropertyChanged(nameof(PatientsTableSelected));
                UpdateComboBoxVisibility();
            }
        }

        public bool OrdersTableSelected
        {
            get => _ordersTableSelected;
            set
            {
                _ordersTableSelected = value;
                if (value && AllTablesSelected) AllTablesSelected = false;
                OnPropertyChanged(nameof(OrdersTableSelected));
                UpdateComboBoxVisibility();
            }
        }

        public bool ServicesTableSelected
        {
            get => _servicesTableSelected;
            set
            {
                _servicesTableSelected = value;
                if (value && AllTablesSelected) AllTablesSelected = false;
                OnPropertyChanged(nameof(ServicesTableSelected));
                UpdateComboBoxVisibility();
            }
        }

        public bool UsersTableSelected
        {
            get => _usersTableSelected;
            set
            {
                _usersTableSelected = value;
                if (value && AllTablesSelected) AllTablesSelected = false;
                OnPropertyChanged(nameof(UsersTableSelected));
                UpdateComboBoxVisibility();
            }
        }

        public List<Pacient> PatientRecords
        {
            get => _patientRecords;
            set { _patientRecords = value; OnPropertyChanged(nameof(PatientRecords)); }
        }

        public List<Order> OrderRecords
        {
            get => _orderRecords;
            set { _orderRecords = value; OnPropertyChanged(nameof(OrderRecords)); }
        }

        public List<Service> ServiceRecords
        {
            get => _serviceRecords;
            set { _serviceRecords = value; OnPropertyChanged(nameof(ServiceRecords)); }
        }

        public List<User> UserRecords
        {
            get => _userRecords;
            set { _userRecords = value; OnPropertyChanged(nameof(UserRecords)); }
        }

        public Pacient SelectedPatientRecord
        {
            get => _selectedPatientRecord;
            set { _selectedPatientRecord = value; OnPropertyChanged(nameof(SelectedPatientRecord)); }
        }

        public Order SelectedOrderRecord
        {
            get => _selectedOrderRecord;
            set { _selectedOrderRecord = value; OnPropertyChanged(nameof(SelectedOrderRecord)); }
        }

        public Service SelectedServiceRecord
        {
            get => _selectedServiceRecord;
            set { _selectedServiceRecord = value; OnPropertyChanged(nameof(SelectedServiceRecord)); }
        }

        public User SelectedUserRecord
        {
            get => _selectedUserRecord;
            set { _selectedUserRecord = value; OnPropertyChanged(nameof(SelectedUserRecord)); }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(nameof(StartDate)); }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(nameof(EndDate)); }
        }

        public string SuggestedBarcode
        {
            get => _suggestedBarcode;
            set { _suggestedBarcode = value; OnPropertyChanged(nameof(SuggestedBarcode)); }
        }

        public List<Analyzer> Analyzers
        {
            get => _analyzers;
            set { _analyzers = value; OnPropertyChanged(nameof(Analyzers)); }
        }

        public Visibility AdminButtonsVisibility
        {
            get => _adminButtonsVisibility;
            set { _adminButtonsVisibility = value; OnPropertyChanged(nameof(AdminButtonsVisibility)); }
        }

        public Visibility IsTableFormatVisible
        {
            get => _isTableFormatVisible;
            set { _isTableFormatVisible = value; OnPropertyChanged(nameof(IsTableFormatVisible)); }
        }

        public Visibility PatientsRecordsComboVisible
        {
            get => PatientsTableSelected && !AllTablesSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility OrdersRecordsComboVisible
        {
            get => OrdersTableSelected && !AllTablesSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility ServicesRecordsComboVisible
        {
            get => ServicesTableSelected && !AllTablesSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility UsersRecordsComboVisible
        {
            get => UsersTableSelected && !AllTablesSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainPage(string role)
        {
            InitializeComponent();
            _currentRole = role;
            DataContext = this;
            SetupUIForRole();
            GenerateBarcodesForNewOrders();
            LoadAnalyzers();
            LoadReportRecords();
        }

        private void SetupUIForRole()
        {
            LabTab.Visibility = Visibility.Collapsed;
            ResearcherTab.Visibility = Visibility.Collapsed;
            AdminTab.Visibility = Visibility.Collapsed;
            AccountantTab.Visibility = Visibility.Collapsed;
            ReportsTab.Visibility = Visibility.Collapsed;
            AdminButtonsPanel.Visibility = Visibility.Collapsed;

            switch (_currentRole)
            {
                case "Лаборант":
                    LabTab.Visibility = Visibility.Visible;
                    MainTabControl.SelectedItem = LabTab;
                    UpdateSuggestedBarcode();
                    break;
                case "Лаборант-Исследователь":
                    ResearcherTab.Visibility = Visibility.Visible;
                    MainTabControl.SelectedItem = ResearcherTab;
                    LoadServiceProvidedData();
                    break;
                case "Лаборант-Администратор":
                    AdminTab.Visibility = Visibility.Visible;
                    ReportsTab.Visibility = Visibility.Visible;
                    AdminButtonsPanel.Visibility = Visibility.Visible;
                    MainTabControl.SelectedItem = AdminTab;
                    LoadAdminData();
                    break;
                case "Бухгалтер":
                    AccountantTab.Visibility = Visibility.Visible;
                    MainTabControl.SelectedItem = AccountantTab;
                    LoadAccountantData();
                    break;
            }

            MainTabControl.SelectionChanged += (s, e) =>
            {
                AdminButtonsVisibility = MainTabControl.SelectedItem == ReportsTab ? Visibility.Collapsed : Visibility.Visible;
            };
        }

        private void UpdateComboBoxVisibility()
        {
            OnPropertyChanged(nameof(PatientsRecordsComboVisible));
            OnPropertyChanged(nameof(OrdersRecordsComboVisible));
            OnPropertyChanged(nameof(ServicesRecordsComboVisible));
            OnPropertyChanged(nameof(UsersRecordsComboVisible));
        }

        private void GenerateBarcodesForNewOrders()
        {
            using (var context = new HospitalBaseEntities())
            {
                var ordersWithoutBarcode = context.Order
                    .Where(o => !o.BarCode.HasValue && o.Pacient_Id != 0 && o.Service_Id != 0)
                    .Take(100)
                    .ToList();

                if (ordersWithoutBarcode.Any())
                {
                    foreach (var order in ordersWithoutBarcode)
                    {
                        int newId = context.Order.Any() ? context.Order.Max(o => o.Order_Id) + 1 : 1;
                        string datePart = DateTime.Now.ToString("yyyyMMdd");
                        string randomPart = _random.Next(100000, 999999).ToString();
                        string barcodeString = $"{newId}{datePart}{randomPart}";
                        if (int.TryParse(barcodeString, out int barcodeValue))
                        {
                            order.BarCode = barcodeValue;
                            string outputPath = Path.Combine("Barcodes", $"Barcode_{barcodeValue}.pdf");
                            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                            BarcodeGenerator.GenerateBarcodePdf(barcodeString, outputPath);
                        }
                    }
                    context.SaveChanges();
                }
            }
        }

        private void UpdateSuggestedBarcode()
        {
            using (var context = new HospitalBaseEntities())
            {
                int lastBarcode = context.Order.Any() ? context.Order.Max(o => o.BarCode ?? 0) + 1 : 1;
                SuggestedBarcode = lastBarcode.ToString();
            }
        }

        private void LoadAnalyzers()
        {
            using (var context = new HospitalBaseEntities())
            {
                Analyzers = context.Analyzer.ToList();
            }
        }

        private void LoadServiceProvidedData()
        {
            using (var context = new HospitalBaseEntities())
            {
                var serviceProvided = context.Service_Provided
                    .Include("Service")
                    .Include("User")
                    .Include("Analyzer")
                    .Where(sp => sp.Service_Id != 0 && sp.User_Id != 0)
                    .ToList();
                ServiceProvidedGrid.ItemsSource = serviceProvided;
            }
        }

        private void LoadAdminData()
        {
            using (var context = new HospitalBaseEntities())
            {
                AdminPatientsGrid.ItemsSource = context.Pacient
                    .Include("Insurance_Company")
                    .Where(p => p.Full_Name != null && p.Full_Name.Trim() != "" && p.Policy != null && p.Policy.Trim() != "")
                    .ToList();

                AdminOrdersGrid.ItemsSource = context.Order
                    .Include("Pacient")
                    .Include("Service")
                    .Where(o => o.Pacient_Id != 0 && o.Service_Id != 0)
                    .ToList();

                ServicesGrid.ItemsSource = context.Service
                    .Where(s => s.Title != null && s.Title.Trim() != "")
                    .ToList();

                UsersGrid.ItemsSource = context.User
                    .Include("Role")
                    .Where(u => u.Full_Name != null && u.Full_Name.Trim() != "" && u.Login != null && u.Login.Trim() != "")
                    .ToList();
            }
        }

        private void LoadAccountantData()
        {
            using (var context = new HospitalBaseEntities())
            {
                InsuranceCompaniesGrid.ItemsSource = context.Insurance_Company
                    .Where(ic => ic.Title != null && ic.Title.Trim() != "")
                    .ToList();

                AccountantPatientsGrid.ItemsSource = context.Pacient
                    .Include("Insurance_Company")
                    .Where(p => p.Full_Name != null && p.Full_Name.Trim() != "" && p.Policy != null && p.Policy.Trim() != "")
                    .ToList();
            }
        }

        private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ScanButton_Click(sender, e);
            }
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            string barcodeText = BarcodeInput.Text.Trim();
            if (string.IsNullOrEmpty(barcodeText) || !int.TryParse(barcodeText, out int barcode))
            {
                MessageBox.Show("Некорректный штрих-код!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new HospitalBaseEntities())
            {
                var orders = context.Order
                    .Include("Pacient")
                    .Include("Service")
                    .Where(o => o.BarCode == barcode)
                    .ToList();
                OrdersGrid.ItemsSource = orders;
                PatientsGrid.ItemsSource = orders.Select(o => o.Pacient).Distinct().ToList();

                if (!orders.Any())
                {
                    MessageBox.Show("Заказ с таким штрих-кодом не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            var receiveWindow = new Window
            {
                Title = "Получение биоматериалов",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var barcodeTextBox = new TextBox { Width = 200, Margin = new Thickness(0, 0, 0, 10) };
            var scanButton = new Button
            {
                Content = "Сканировать",
                Width = 100,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)), // Зеленый (#28A745)
                Foreground = System.Windows.Media.Brushes.White
            };
            var barcodeImage = new System.Windows.Controls.Image
            {
                Width = 200,
                Height = 50,
                Margin = new Thickness(0, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };

            stackPanel.Children.Add(barcodeTextBox);
            stackPanel.Children.Add(scanButton);
            stackPanel.Children.Add(barcodeImage);
            receiveWindow.Content = stackPanel;

            scanButton.Click += (s, args) =>
            {
                string barcodeText = barcodeTextBox.Text.Trim();
                if (string.IsNullOrEmpty(barcodeText) || !int.TryParse(barcodeText, out int barcode))
                {
                    MessageBox.Show("Некорректный штрих-код!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var context = new HospitalBaseEntities())
                {
                    var orders = context.Order
                        .Include("Pacient")
                        .Include("Service")
                        .Where(o => o.BarCode == barcode)
                        .ToList();
                    OrdersGrid.ItemsSource = orders;
                    PatientsGrid.ItemsSource = orders.Select(o => o.Pacient).Distinct().ToList();

                    if (orders.Any())
                    {
                        BarcodeInput.Text = barcodeText;
                        string pdfPath = Path.Combine("Barcodes", $"Barcode_{barcode}.pdf");
                        if (File.Exists(pdfPath))
                        {
                            barcodeImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/barcode_placeholder.png"));
                            barcodeImage.Visibility = Visibility.Visible;
                        }
                        receiveWindow.Close();
                    }
                    else
                    {
                        MessageBox.Show("Заказ с таким штрих-кодом не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };

            barcodeTextBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    scanButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            };

            receiveWindow.ShowDialog();
        }

        private void LoadReportRecords()
        {
            using (var context = new HospitalBaseEntities())
            {
                PatientRecords = context.Pacient
                    .Where(p => p.Full_Name != null && p.Full_Name.Trim() != "")
                    .ToList();
                PatientRecords.Insert(0, null); // Пустое значение для "Все записи"

                OrderRecords = context.Order
                    .Where(o => o.Pacient_Id != 0 && o.Service_Id != 0)
                    .ToList();
                OrderRecords.Insert(0, null);

                ServiceRecords = context.Service
                    .Where(s => s.Title != null && s.Title.Trim() != "")
                    .ToList();
                ServiceRecords.Insert(0, null);

                UserRecords = context.User
                    .Where(u => u.Full_Name != null && u.Full_Name.Trim() != "")
                    .ToList();
                UserRecords.Insert(0, null);
            }
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            var selectedTables = new List<string>();
            if (AllTablesSelected || PatientsTableSelected) selectedTables.Add("Patients");
            if (AllTablesSelected || OrdersTableSelected) selectedTables.Add("Orders");
            if (AllTablesSelected || ServicesTableSelected) selectedTables.Add("Services");
            if (AllTablesSelected || UsersTableSelected) selectedTables.Add("Users");

            if (!selectedTables.Any())
            {
                MessageBox.Show("Выберите хотя бы одну таблицу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedRecordIds = new Dictionary<string, List<int>>();
            if (SelectedPatientRecord != null && PatientsTableSelected && !AllTablesSelected)
                selectedRecordIds["Patients"] = new List<int> { SelectedPatientRecord.Pacient_Id };
            if (SelectedOrderRecord != null && OrdersTableSelected && !AllTablesSelected)
                selectedRecordIds["Orders"] = new List<int> { SelectedOrderRecord.Order_Id };
            if (SelectedServiceRecord != null && ServicesTableSelected && !AllTablesSelected)
                selectedRecordIds["Services"] = new List<int> { SelectedServiceRecord.Service_Id };
            if (SelectedUserRecord != null && UsersTableSelected && !AllTablesSelected)
                selectedRecordIds["Users"] = new List<int> { SelectedUserRecord.User_Id };

            var saveFileDialog = new SaveFileDialog
            {
                Filter = SelectedFormat == "Excel" ? "Excel Files (*.xlsx)|*.xlsx" :
                        SelectedFormat == "PDF" ? "PDF Files (*.pdf)|*.pdf" : "Word Files (*.docx)|*.docx",
                FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = SelectedFormat == "Excel" ? ".xlsx" :
                            SelectedFormat == "PDF" ? ".pdf" : ".docx",
                AddExtension = true
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (SelectedFormat == "Excel")
                    {
                        ExportExcel.GenerateExcelReport(selectedTables, selectedRecordIds, new Dictionary<string, List<string>>(), StartDate, EndDate);
                    }
                    else
                    {
                        ExportWord.GenerateWordReport(selectedTables, IsTableFormat, selectedRecordIds, new Dictionary<string, List<string>>(), StartDate, EndDate, SelectedFormat == "PDF");
                    }
                    MessageBox.Show("Отчет успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Type entityType = GetSelectedGridEntityType();
            if (entityType == null) return;

            var editWindow = new EditWindow(null, entityType) { Owner = Window.GetWindow(this) };
            if (editWindow.ShowDialog() == true)
            {
                LoadDataForCurrentRole();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedGrid = GetSelectedGrid();
            if (selectedGrid?.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для редактирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Type entityType = GetSelectedGridEntityType();
            if (entityType == null) return;

            var editWindow = new EditWindow(selectedGrid.SelectedItem, entityType) { Owner = Window.GetWindow(this) };
            if (editWindow.ShowDialog() == true)
            {
                LoadDataForCurrentRole();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedGrid = GetSelectedGrid();
            if (selectedGrid?.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                using (var context = new HospitalBaseEntities())
                {
                    context.Entry(selectedGrid.SelectedItem).State = EntityState.Deleted;
                    context.SaveChanges();
                }
                LoadDataForCurrentRole();
            }
        }

        private DataGrid GetSelectedGrid()
        {
            if (MainTabControl.SelectedItem == AdminTab)
            {
                if (AdminPatientsGrid.SelectedItem != null) return AdminPatientsGrid;
                if (AdminOrdersGrid.SelectedItem != null) return AdminOrdersGrid;
                if (ServicesGrid.SelectedItem != null) return ServicesGrid;
                if (UsersGrid.SelectedItem != null) return UsersGrid;
            }
            return null;
        }

        private Type GetSelectedGridEntityType()
        {
            var selectedGrid = GetSelectedGrid();
            if (selectedGrid == null) return null;

            if (selectedGrid == AdminPatientsGrid) return typeof(Pacient);
            if (selectedGrid == AdminOrdersGrid) return typeof(Order);
            if (selectedGrid == ServicesGrid) return typeof(Service);
            if (selectedGrid == UsersGrid) return typeof(User);
            return null;
        }

        private void LoadDataForCurrentRole()
        {
            if (_currentRole == "Лаборант-Исследователь")
                LoadServiceProvidedData();
            else if (_currentRole == "Лаборант-Администратор")
                LoadAdminData();
            else if (_currentRole == "Бухгалтер")
                LoadAccountantData();
        }
    }

    public class StatusToTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null)
                return "Неизвестно";

            bool orderStatus = (bool)values[0];
            DateTime? completeTime = values[1] as DateTime?;

            return orderStatus && completeTime.HasValue
                ? $"Проанализировано ({completeTime.Value:dd.MM.yyyy HH:mm})"
                : "В работе";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}