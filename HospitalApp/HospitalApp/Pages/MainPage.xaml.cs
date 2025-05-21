using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using BarcodeStandard;
using SkiaSharp;
using System.Globalization;

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
        private Service_Provided _selectedServiceProvided;
        private DateTime? _selectedDateProvided;
        private Insurance_Company _selectedInsuranceCompany;
        private string _selectedInsuranceCompanyTitle;
        private decimal _billAmount;
        private List<User> _bills;
        private Dictionary<int, DateTime?> _billDates = new Dictionary<int, DateTime?>();
        private bool _areTableCheckBoxesEnabled = true;
        private List<Pacient> _availablePatients;
        private Pacient _selectedPatient;
        private bool _isPatientsGridReadOnly = true;
        private bool _isOrdersGridReadOnly = true;
        private bool _isServicesGridReadOnly = true;
        private bool _isUsersGridReadOnly = true;
        private DataGrid _selectedGrid;
        private object _editableItem;
        private List<int?> _scannedBarcodes;
        private bool _isAdminTabSelected;
        private bool _isReportsTabSelected;

        public bool IsTableFormat
        {
            get => _isTableFormat;
            set { _isTableFormat = value; OnPropertyChanged(nameof(IsTableFormat)); }
        }

        public bool IsTableFormatVisible
        {
            get => _selectedFormat == "Word" || _selectedFormat == "PDF";
        }

        public string SelectedFormat
        {
            get => _selectedFormat;
            set
            {
                _selectedFormat = value;
                // Устанавливаем IsTableFormat по умолчанию для Excel
                if (value == "Excel")
                    IsTableFormat = false;
                OnPropertyChanged(nameof(SelectedFormat));
                OnPropertyChanged(nameof(IsTableFormatVisible));
            }
        }

        public bool AllTablesSelected
        {
            get => _allTablesSelected;
            set
            {
                _allTablesSelected = value;
                OnPropertyChanged(nameof(AllTablesSelected));
                UpdateIndividualCheckBoxes();
            }
        }

        public bool PatientsTableSelected
        {
            get => _patientsTableSelected;
            set
            {
                _patientsTableSelected = value;
                OnPropertyChanged(nameof(PatientsTableSelected));
                OnPropertyChanged(nameof(PatientsRecordsComboVisible));
            }
        }

        public bool OrdersTableSelected
        {
            get => _ordersTableSelected;
            set
            {
                _ordersTableSelected = value;
                OnPropertyChanged(nameof(OrdersTableSelected));
                OnPropertyChanged(nameof(OrdersRecordsComboVisible));
            }
        }

        public bool ServicesTableSelected
        {
            get => _servicesTableSelected;
            set
            {
                _servicesTableSelected = value;
                OnPropertyChanged(nameof(ServicesTableSelected));
                OnPropertyChanged(nameof(ServicesRecordsComboVisible));
            }
        }

        public bool UsersTableSelected
        {
            get => _usersTableSelected;
            set
            {
                _usersTableSelected = value;
                OnPropertyChanged(nameof(UsersTableSelected));
                OnPropertyChanged(nameof(UsersRecordsComboVisible));
            }
        }

        public bool AreTableCheckBoxesEnabled
        {
            get => _areTableCheckBoxesEnabled;
            set { _areTableCheckBoxesEnabled = value; OnPropertyChanged(nameof(AreTableCheckBoxesEnabled)); }
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

        public Visibility PatientsRecordsComboVisible
        {
            get => (PatientsTableSelected && !AllTablesSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility OrdersRecordsComboVisible
        {
            get => (OrdersTableSelected && !AllTablesSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility ServicesRecordsComboVisible
        {
            get => (ServicesTableSelected && !AllTablesSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility UsersRecordsComboVisible
        {
            get => (UsersTableSelected && !AllTablesSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        public Service_Provided SelectedServiceProvided
        {
            get => _selectedServiceProvided;
            set { _selectedServiceProvided = value; OnPropertyChanged(nameof(SelectedServiceProvided)); }
        }

        public DateTime? SelectedDateProvided
        {
            get => _selectedDateProvided;
            set { _selectedDateProvided = value; OnPropertyChanged(nameof(SelectedDateProvided)); }
        }

        public Insurance_Company SelectedInsuranceCompany
        {
            get => _selectedInsuranceCompany;
            set
            {
                _selectedInsuranceCompany = value;
                SelectedInsuranceCompanyTitle = value?.Title;
                LoadAvailablePatients();
                PatientsComboBox.IsEnabled = value != null;
                OnPropertyChanged(nameof(SelectedInsuranceCompany));
            }
        }

        public string SelectedInsuranceCompanyTitle
        {
            get => _selectedInsuranceCompanyTitle;
            set { _selectedInsuranceCompanyTitle = value; OnPropertyChanged(nameof(SelectedInsuranceCompanyTitle)); }
        }

        public decimal BillAmount
        {
            get => _billAmount;
            set { _billAmount = value; OnPropertyChanged(nameof(BillAmount)); }
        }

        public List<User> Bills
        {
            get => _bills;
            set { _bills = value; OnPropertyChanged(nameof(Bills)); }
        }

        public List<Pacient> AvailablePatients
        {
            get => _availablePatients;
            set { _availablePatients = value; OnPropertyChanged(nameof(AvailablePatients)); }
        }

        public Pacient SelectedPatient
        {
            get => _selectedPatient;
            set { _selectedPatient = value; OnPropertyChanged(nameof(SelectedPatient)); }
        }

        public bool IsPatientsGridReadOnly
        {
            get => _isPatientsGridReadOnly;
            set { _isPatientsGridReadOnly = value; OnPropertyChanged(nameof(IsPatientsGridReadOnly)); }
        }

        public bool IsOrdersGridReadOnly
        {
            get => _isOrdersGridReadOnly;
            set { _isOrdersGridReadOnly = value; OnPropertyChanged(nameof(IsOrdersGridReadOnly)); }
        }

        public bool IsServicesGridReadOnly
        {
            get => _isServicesGridReadOnly;
            set { _isServicesGridReadOnly = value; OnPropertyChanged(nameof(IsServicesGridReadOnly)); }
        }

        public bool IsUsersGridReadOnly
        {
            get => _isUsersGridReadOnly;
            set { _isUsersGridReadOnly = value; OnPropertyChanged(nameof(IsUsersGridReadOnly)); }
        }

        public bool IsAdminTabSelected
        {
            get => _isAdminTabSelected;
            set { _isAdminTabSelected = value; OnPropertyChanged(nameof(IsAdminTabSelected)); }
        }

        public bool IsReportsTabSelected
        {
            get => _isReportsTabSelected;
            set { _isReportsTabSelected = value; OnPropertyChanged(nameof(IsReportsTabSelected)); }
        }

        public List<string> StatusOptions { get; } = new List<string> { "В работе", "Готово" };

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainPage(string role)
        {
            InitializeComponent();
            _currentRole = role;
            _scannedBarcodes = new List<int?>();
            DataContext = this;
            SetupUIForRole();
            if (_currentRole == "Лаборант")
            {
                BarcodeInput.Text = "";
            }
            if (_currentRole != "Лаборант-Исследователь")
            {
                GenerateQRCodesForNewOrders();
            }
            LoadAnalyzers();
            LoadReportRecords();
            if (_currentRole == "Бухгалтер")
            {
                LoadBills();
            }
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
                    SetupComboBoxColumns();
                    break;
                case "Бухгалтер":
                    AccountantTab.Visibility = Visibility.Visible;
                    AdminButtonsPanel.Visibility = Visibility.Collapsed;
                    MainTabControl.SelectedItem = AccountantTab;
                    LoadAccountantData();
                    break;
            }

            MainTabControl.SelectionChanged += (s, e) =>
            {
                IsAdminTabSelected = MainTabControl.SelectedItem == AdminTab;
                IsReportsTabSelected = MainTabControl.SelectedItem == ReportsTab;
                AdminButtonsVisibility = _currentRole == "Лаборант-Администратор" ? Visibility.Visible : Visibility.Collapsed;
            };

            ReportFormatCombo.SelectedItem = ReportFormatCombo.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == _selectedFormat);

            if (_currentRole == "Бухгалтер")
            {
                BillsGrid.PreviewKeyDown += BillsGrid_PreviewKeyDown;
            }
        }

        private void SetupComboBoxColumns()
        {
            using (var context = new HospitalBaseEntities())
            {
                var patients = context.Pacient.ToList();
                var services = context.Service.ToList();
                var roles = context.Role.ToList();
                var insuranceCompanies = context.Insurance_Company.ToList();

                var patientColumn = AdminOrdersGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Пациент") as DataGridComboBoxColumn;
                if (patientColumn != null)
                    patientColumn.ItemsSource = patients;

                var serviceColumn = AdminOrdersGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Услуга") as DataGridComboBoxColumn;
                if (serviceColumn != null)
                    serviceColumn.ItemsSource = services;

                var roleColumn = UsersGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Роль") as DataGridComboBoxColumn;
                if (roleColumn != null)
                    roleColumn.ItemsSource = roles;

                var insuranceColumn = UsersGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Страх. компания") as DataGridComboBoxColumn;
                if (insuranceColumn != null)
                    insuranceColumn.ItemsSource = insuranceCompanies;

                var statusColumn = AdminOrdersGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Статус") as DataGridComboBoxColumn;
                if (statusColumn != null)
                    statusColumn.ItemsSource = StatusOptions;
            }
        }

        private void BillsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && BillsGrid.SelectedItem is User selectedBill)
            {
                if (MessageBox.Show("Вы уверены, что хотите удалить этот счет?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    using (var context = new HospitalBaseEntities())
                    {
                        var billToDelete = context.User.FirstOrDefault(u => u.User_Id == selectedBill.User_Id);
                        if (billToDelete != null)
                        {
                            context.User.Remove(billToDelete);
                            context.SaveChanges();
                            LoadBills();
                        }
                    }
                }
                e.Handled = true;
            }
        }

        private void LabDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && sender is DataGrid grid && grid.SelectedItem != null)
            {
                if (MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    using (var context = new HospitalBaseEntities())
                    {
                        try
                        {
                            if (grid == PatientsGrid)
                            {
                                var patient = grid.SelectedItem as Pacient;
                                var dbPatient = context.Pacient.FirstOrDefault(p => p.Pacient_Id == patient.Pacient_Id);
                                if (dbPatient != null)
                                    context.Pacient.Remove(dbPatient);
                            }
                            else if (grid == OrdersGrid)
                            {
                                var order = grid.SelectedItem as Order;
                                var dbOrder = context.Order.FirstOrDefault(o => o.Order_Id == order.Order_Id);
                                if (dbOrder != null)
                                    context.Order.Remove(dbOrder);
                            }
                            context.SaveChanges();
                            UpdateLabTables();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                e.Handled = true;
            }
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && sender is DataGrid grid && grid.SelectedItem != null)
            {
                if (MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    using (var context = new HospitalBaseEntities())
                    {
                        try
                        {
                            if (grid == AdminPatientsGrid)
                            {
                                var patient = grid.SelectedItem as Pacient;
                                var dbPatient = context.Pacient.FirstOrDefault(p => p.Pacient_Id == patient.Pacient_Id);
                                if (dbPatient != null)
                                    context.Pacient.Remove(dbPatient);
                            }
                            else if (grid == AdminOrdersGrid)
                            {
                                var order = grid.SelectedItem as Order;
                                var dbOrder = context.Order.FirstOrDefault(o => o.Order_Id == order.Order_Id);
                                if (dbOrder != null)
                                    context.Order.Remove(dbOrder);
                            }
                            else if (grid == ServicesGrid)
                            {
                                var service = grid.SelectedItem as Service;
                                var dbService = context.Service.FirstOrDefault(s => s.Service_Id == service.Service_Id);
                                if (dbService != null)
                                    context.Service.Remove(dbService);
                            }
                            else if (grid == UsersGrid)
                            {
                                var user = grid.SelectedItem as User;
                                var dbUser = context.User.FirstOrDefault(u => u.User_Id == user.User_Id);
                                if (dbUser != null)
                                    context.User.Remove(dbUser);
                            }
                            context.SaveChanges();
                            LoadAdminData();
                            ResetEditMode();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                e.Handled = true;
            }
        }

        private void AllTablesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateIndividualCheckBoxes();
        }

        private void AllTablesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateIndividualCheckBoxes();
        }

        private void TableCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!PatientsTableSelected || !OrdersTableSelected || !ServicesTableSelected || !UsersTableSelected)
            {
                AllTablesSelected = false;
            }
        }

        private void UpdateIndividualCheckBoxes()
        {
            if (AllTablesSelected)
            {
                PatientsTableSelected = true;
                OrdersTableSelected = true;
                ServicesTableSelected = true;
                UsersTableSelected = true;
                AreTableCheckBoxesEnabled = true;
                SelectedPatientRecord = null;
                SelectedOrderRecord = null;
                SelectedServiceRecord = null;
                SelectedUserRecord = null;
            }
            else
            {
                AreTableCheckBoxesEnabled = true;
            }
            OnPropertyChanged(nameof(PatientsRecordsComboVisible));
            OnPropertyChanged(nameof(OrdersRecordsComboVisible));
            OnPropertyChanged(nameof(ServicesRecordsComboVisible));
            OnPropertyChanged(nameof(UsersRecordsComboVisible));
        }

        private void GenerateQRCodesForNewOrders()
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
                        try
                        {
                            int newId = context.Order.Any() ? context.Order.Max(o => o.Order_Id) + 1 : 1;
                            string barcodeString = newId.ToString();
                            int barcodeValue;

                            while (context.Order.Any(o => o.BarCode == newId))
                            {
                                newId++;
                                barcodeString = newId.ToString();
                            }

                            if (int.TryParse(barcodeString, out barcodeValue))
                            {
                                order.BarCode = barcodeValue;
                                string outputPath = Path.Combine("Barcodes", $"Barcode_{order.BarCode}.png");
                                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                                GenerateBarcode(barcodeString, outputPath);
                                Debug.WriteLine($"Штрих-код сгенерирован: {outputPath}, код: {barcodeString}");
                            }
                            else
                            {
                                Debug.WriteLine($"Ошибка: Невозможно преобразовать штрих-код в число: {barcodeString}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка при генерации штрих-кода для заказа {order.Order_Id}: {ex.Message}");
                        }
                    }
                    context.SaveChanges();
                }
            }
        }

        private void GenerateBarcode(string barcodeText, string outputPath)
        {
            try
            {
                var barcode = new BarcodeStandard.Barcode
                {
                    EncodedType = BarcodeStandard.Type.Code128,
                    RawData = barcodeText,
                    IncludeLabel = true
                };

                using (var image = barcode.Encode(BarcodeStandard.Type.Code128, barcodeText))
                using (var bitmap = SKBitmap.FromImage(image))
                using (var data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = new FileStream(outputPath, FileMode.Create))
                {
                    data.SaveTo(stream);
                }
                Debug.WriteLine($"Штрих-код успешно сохранен: {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении штрих-кода: {ex.Message}");
                throw;
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
                    .Include(sp => sp.Service)
                    .Include(sp => sp.User)
                    .Include(sp => sp.Analyzer)
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
                    .Include(p => p.Insurance_Company)
                    .Where(p => p.Full_Name != null && p.Full_Name.Trim() != "")
                    .ToList();

                AdminOrdersGrid.ItemsSource = context.Order
                    .Include(o => o.Pacient)
                    .Include(o => o.Service)
                    .Where(o => o.Pacient_Id != 0 && o.Service_Id != 0)
                    .ToList();

                ServicesGrid.ItemsSource = context.Service
                    .Where(s => s.Title != null && s.Title.Trim() != "")
                    .ToList();

                UsersGrid.ItemsSource = context.User
                    .Include(u => u.Role)
                    .Include(u => u.Insurance_Company)
                    .Where(u => u.Full_Name != null && u.Full_Name.Trim() != "")
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
            }
        }

        private void LoadBills()
        {
            using (var context = new HospitalBaseEntities())
            {
                Bills = context.User
                    .Include(u => u.Insurance_Company)
                    .Where(u => u.Account.HasValue && u.Account > 0 && u.Insurance_Company_Id.HasValue)
                    .ToList();
                BillsGrid.ItemsSource = Bills;
            }
        }

        private void LoadAvailablePatients()
        {
            using (var context = new HospitalBaseEntities())
            {
                if (SelectedInsuranceCompany != null)
                {
                    AvailablePatients = context.Pacient
                        .Include(p => p.Insurance_Company)
                        .Where(p => p.Insurance_Company_Id == SelectedInsuranceCompany.Insurance_Company_Id && p.Full_Name != null && p.Full_Name.Trim() != "")
                        .ToList();
                }
                else
                {
                    AvailablePatients = new List<Pacient>();
                }
                SelectedPatient = null;
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
                    .Include(o => o.Pacient)
                    .Include(o => o.Service)
                    .Where(o => o.BarCode == barcode)
                    .ToList();

                if (orders.Any())
                {
                    if (!_scannedBarcodes.Contains(barcode))
                    {
                        _scannedBarcodes.Add(barcode);
                    }
                    UpdateLabTables();
                    BarcodeInput.Text = "";
                    MessageBox.Show($"Штрих-код успешно отсканирован: {barcodeText}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Заказ с таким штрих-кодом не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateLabTables()
        {
            using (var context = new HospitalBaseEntities())
            {
                var orders = context.Order
                    .Include(o => o.Pacient)
                    .Include(o => o.Service)
                    .Where(o => _scannedBarcodes.Contains(o.BarCode))
                    .ToList();

                var currentOrders = OrdersGrid.ItemsSource as List<Order> ?? new List<Order>();
                var updatedOrders = currentOrders
                    .Where(o => _scannedBarcodes.Contains(o.BarCode))
                    .ToList();
                updatedOrders.AddRange(orders.Where(o => !updatedOrders.Any(existing => existing.Order_Id == o.Order_Id)));

                OrdersGrid.ItemsSource = updatedOrders;
                PatientsGrid.ItemsSource = updatedOrders.Select(o => o.Pacient).Distinct().ToList();
            }
        }

        private bool HasUnscannedBarcodes()
        {
            using (var context = new HospitalBaseEntities())
            {
                return context.Order
                    .Any(o => o.BarCode.HasValue && !_scannedBarcodes.Contains(o.BarCode));
            }
        }

        private void ReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!HasUnscannedBarcodes())
            {
                MessageBox.Show("Нет доступных штрих-кодов для сканирования!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var receiveWindow = new Window
            {
                Title = "Получение биоматериалов",
                MinWidth = 300,
                MaxWidth = 400,
                MinHeight = 200,
                MaxHeight = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var barcodeTextBox = new TextBox
            {
                Width = 200,
                Margin = new Thickness(0, 0, 0, 10),
                Text = ""
            };
            var scanButton = new Button
            {
                Content = "Сканировать",
                Width = 100,
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Foreground = Brushes.White
            };
            var barcodeImage = new System.Windows.Controls.Image
            {
                Width = 300,
                Height = 100,
                Margin = new Thickness(0, 10, 0, 10),
                Visibility = Visibility.Visible
            };

            stackPanel.Children.Add(barcodeTextBox);
            stackPanel.Children.Add(scanButton);
            stackPanel.Children.Add(barcodeImage);
            receiveWindow.Content = stackPanel;

            string barcodeString = "";
            try
            {
                using (var context = new HospitalBaseEntities())
                {
                    var unscannedOrder = context.Order
                        .Where(o => o.BarCode.HasValue && !_scannedBarcodes.Contains(o.BarCode))
                        .OrderBy(o => o.Order_Id)
                        .FirstOrDefault();

                    if (unscannedOrder != null)
                    {
                        barcodeString = unscannedOrder.BarCode.ToString();
                        string imagePath = Path.Combine("Barcodes", $"Barcode_{barcodeString}.png");
                        Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                        GenerateBarcode(barcodeString, imagePath);
                        barcodeImage.Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                        Debug.WriteLine($"Штрих-код отображен из базы данных: {imagePath}, код: {barcodeString}");
                    }
                    else
                    {
                        Debug.WriteLine("Не найдено неотсканированных штрих-кодов в базе данных.");
                        barcodeImage.Source = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при генерации штрих-кода: {ex.Message}");
                MessageBox.Show($"Ошибка при генерации штрих-кода: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                receiveWindow.Close();
                return;
            }

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
                        .Include(o => o.Pacient)
                        .Include(o => o.Service)
                        .Where(o => o.BarCode == barcode)
                        .ToList();
                    if (orders.Any())
                    {
                        if (!_scannedBarcodes.Contains(barcode))
                        {
                            _scannedBarcodes.Add(barcode);
                        }
                        UpdateLabTables();
                        BarcodeInput.Text = "";
                        MessageBox.Show($"Штрих-код успешно отсканирован: {barcodeText}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void DataGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                switch (_currentRole)
                {
                    case "Лаборант":
                        LabTabScrollViewer.CanContentScroll = false;
                        break;
                    case "Лаборант-Исследователь":
                        ResearcherTabScrollViewer.CanContentScroll = false;
                        break;
                    case "Лаборант-Администратор":
                        AdminTabScrollViewer.CanContentScroll = false;
                        break;
                }
                grid.Focus();
            }
        }

        private void DataGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is DataGrid)
            {
                switch (_currentRole)
                {
                    case "Лаборант":
                        LabTabScrollViewer.CanContentScroll = true;
                        break;
                    case "Лаборант-Исследователь":
                        ResearcherTabScrollViewer.CanContentScroll = true;
                        break;
                    case "Лаборант-Администратор":
                        AdminTabScrollViewer.CanContentScroll = true;
                        break;
                }
            }
        }

        private void ServiceProvidedGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServiceProvidedGrid.SelectedItem is Service_Provided selected)
            {
                SelectedServiceProvided = selected;
                SelectedDateProvided = selected.Date_Provided;
            }
            else
            {
                SelectedServiceProvided = null;
                SelectedDateProvided = null;
            }
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedServiceProvided == null)
            {
                MessageBox.Show("Выберите запись для анализа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SelectedDateProvided == null)
            {
                MessageBox.Show("Выберите дату предоставления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new HospitalBaseEntities())
            {
                var serviceProvided = context.Service_Provided
                    .FirstOrDefault(sp => sp.Service_Provided_Id == SelectedServiceProvided.Service_Provided_Id);
                if (serviceProvided != null)
                {
                    serviceProvided.Date_Provided = SelectedDateProvided;
                    context.SaveChanges();
                    LoadServiceProvidedData();
                    MessageBox.Show("Дата успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void InsuranceCompaniesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InsuranceCompaniesGrid.SelectedItem is Insurance_Company selected)
            {
                SelectedInsuranceCompany = selected;
                InsuranceCompanyTextBox.IsEnabled = true;
            }
            else
            {
                SelectedInsuranceCompany = null;
                InsuranceCompanyTextBox.IsEnabled = false;
                PatientsComboBox.IsEnabled = false;
            }
        }

        private void IssueBillButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedInsuranceCompany == null)
            {
                MessageBox.Show("Выберите страховую компанию!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SelectedPatient == null)
            {
                MessageBox.Show("Выберите пациента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (BillAmount <= 0)
            {
                MessageBox.Show("Введите корректную сумму счета!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new HospitalBaseEntities())
            {
                try
                {
                    var user = context.User
                        .FirstOrDefault(u => u.Insurance_Company_Id == SelectedInsuranceCompany.Insurance_Company_Id && u.Full_Name.Contains("Счет для"));

                    if (user == null)
                    {
                        user = new User
                        {
                            Full_Name = $"Счет для {SelectedInsuranceCompany.Title} (Пациент: {SelectedPatient.Full_Name})",
                            Login = $"bill_{SelectedInsuranceCompany.Insurance_Company_Id}_{DateTime.Now.Ticks}",
                            Insurance_Company_Id = SelectedInsuranceCompany.Insurance_Company_Id,
                            Account = BillAmount,
                            Role_Id = context.Role.FirstOrDefault(r => r.Name == "Бухгалтер")?.Role_Id ?? 1,
                            Service_Id = context.Service.FirstOrDefault()?.Service_Id ?? 1,
                            Last_Login_Date = DateTime.Now,
                            Password = "default_password"
                        };
                        context.User.Add(user);
                    }
                    else
                    {
                        user.Account = (user.Account ?? 0) + BillAmount;
                    }

                    context.SaveChanges();

                    if (_billDates.ContainsKey(user.User_Id))
                    {
                        _billDates[user.User_Id] = null;
                    }
                    else
                    {
                        _billDates.Add(user.User_Id, null);
                    }

                    LoadBills();
                    LoadAdminData();
                    BillAmount = 0;
                    SelectedPatient = null;
                    MessageBox.Show("Счет успешно выставлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (DbEntityValidationException ex)
                {
                    var errors = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");
                    MessageBox.Show($"Ошибка валидации: {string.Join("; ", errors)}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при выставлении счета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadReportRecords()
        {
            using (var context = new HospitalBaseEntities())
            {
                PatientRecords = context.Pacient
                    .Where(p => p.Full_Name != null && p.Full_Name.Trim() != "")
                    .ToList();
                PatientRecords.Insert(0, null);

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

        private void ReportFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportFormatCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                SelectedFormat = selectedItem.Content.ToString();
            }
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTables = new List<string>();
                if (AllTablesSelected || PatientsTableSelected) selectedTables.Add("Patients");
                if (AllTablesSelected || OrdersTableSelected) selectedTables.Add("Orders");
                if (AllTablesSelected || ServicesTableSelected) selectedTables.Add("Services");
                if (AllTablesSelected || UsersTableSelected) selectedTables.Add("Users");

                if (!selectedTables.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну таблицу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                if (StartDate > EndDate)
                {
                    MessageBox.Show("Дата начала не может быть позже даты окончания!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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
                        string filePath = saveFileDialog.FileName;
                        if (SelectedFormat == "Excel")
                        {
                            ExportExcel.GenerateExcelReport(selectedTables, selectedRecordIds, StartDate, EndDate, filePath);
                        }
                        else
                        {
                            ExportWord.GenerateWordReport(selectedTables, IsTableFormat, selectedRecordIds, StartDate, EndDate, SelectedFormat == "PDF", filePath);
                        }
                        Process.Start(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при создании или открытии отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedGrid = sender as DataGrid;
            ResetEditMode();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGrid == null)
            {
                MessageBox.Show("Выберите таблицу для добавления записи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new HospitalBaseEntities())
            {
                object newItem = null;
                if (_selectedGrid == AdminPatientsGrid)
                {
                    newItem = new Pacient { Full_Name = "Новый пациент", Policy = "Не указано" };
                    var patients = (AdminPatientsGrid.ItemsSource as List<Pacient>) ?? new List<Pacient>();
                    patients.Add((Pacient)newItem);
                    AdminPatientsGrid.ItemsSource = patients;
                }
                else if (_selectedGrid == AdminOrdersGrid)
                {
                    newItem = new Order { Create_Date = DateTime.Now, Order_Status = false, Pacient_Id = context.Pacient.FirstOrDefault()?.Pacient_Id ?? 0, Service_Id = context.Service.FirstOrDefault()?.Service_Id ?? 0 };
                    var orders = (AdminOrdersGrid.ItemsSource as List<Order>) ?? new List<Order>();
                    orders.Add((Order)newItem);
                    AdminOrdersGrid.ItemsSource = orders;
                }
                else if (_selectedGrid == ServicesGrid)
                {
                    newItem = new Service { Title = "Новая услуга", Price = 0, Deadline = 1 };
                    var services = (ServicesGrid.ItemsSource as List<Service>) ?? new List<Service>();
                    services.Add((Service)newItem);
                    ServicesGrid.ItemsSource = services;
                }
                else if (_selectedGrid == UsersGrid)
                {
                    newItem = new User { Full_Name = "Новый пользователь", Login = "new_user", Role_Id = context.Role.FirstOrDefault()?.Role_Id ?? 1 };
                    var users = (UsersGrid.ItemsSource as List<User>) ?? new List<User>();
                    users.Add((User)newItem);
                    UsersGrid.ItemsSource = users;
                }

                _editableItem = newItem;
                SetGridEditMode(_selectedGrid, newItem);
                _selectedGrid.ScrollIntoView(newItem);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGrid == null || _selectedGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для редактирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _editableItem = _selectedGrid.SelectedItem;
            SetGridEditMode(_selectedGrid, _editableItem);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGrid == null || _selectedGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                using (var context = new HospitalBaseEntities())
                {
                    context.Entry(_selectedGrid.SelectedItem).State = EntityState.Deleted;
                    context.SaveChanges();
                }
                LoadAdminData();
                ResetEditMode();
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && _editableItem != null)
            {
                using (var context = new HospitalBaseEntities())
                {
                    try
                    {
                        if (_selectedGrid == AdminPatientsGrid)
                        {
                            var patient = _editableItem as Pacient;
                            if (context.Pacient.Any(p => p.Pacient_Id == patient.Pacient_Id))
                            {
                                context.Entry(patient).State = EntityState.Modified;
                            }
                            else
                            {
                                context.Pacient.Add(patient);
                            }
                        }
                        else if (_selectedGrid == AdminOrdersGrid)
                        {
                            var order = _editableItem as Order;
                            if (context.Order.Any(o => o.Order_Id == order.Order_Id))
                            {
                                context.Entry(order).State = EntityState.Modified;
                            }
                            else
                            {
                                context.Order.Add(order);
                            }
                        }
                        else if (_selectedGrid == ServicesGrid)
                        {
                            var service = _editableItem as Service;
                            if (context.Service.Any(s => s.Service_Id == service.Service_Id))
                            {
                                context.Entry(service).State = EntityState.Modified;
                            }
                            else
                            {
                                context.Service.Add(service);
                            }
                        }
                        else if (_selectedGrid == UsersGrid)
                        {
                            var user = _editableItem as User;
                            if (context.User.Any(u => u.User_Id == user.User_Id))
                            {
                                context.Entry(user).State = EntityState.Modified;
                            }
                            else
                            {
                                context.User.Add(user);
                            }
                        }

                        context.SaveChanges();
                        LoadAdminData();
                        ResetEditMode();
                    }
                    catch (DbEntityValidationException ex)
                    {
                        var errors = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");
                        MessageBox.Show($"Ошибка валидации: {string.Join("; ", errors)}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                }
            }
        }

        private void SetGridEditMode(DataGrid grid, object item)
        {
            ResetEditMode();
            if (grid == AdminPatientsGrid)
            {
                IsPatientsGridReadOnly = false;
                AdminPatientsGrid.SelectedItem = item;
            }
            else if (grid == AdminOrdersGrid)
            {
                IsOrdersGridReadOnly = false;
                AdminOrdersGrid.SelectedItem = item;
            }
            else if (grid == ServicesGrid)
            {
                IsServicesGridReadOnly = false;
                ServicesGrid.SelectedItem = item;
            }
            else if (grid == UsersGrid)
            {
                IsUsersGridReadOnly = false;
                UsersGrid.SelectedItem = item;
            }
        }

        private void ResetEditMode()
        {
            IsPatientsGridReadOnly = true;
            IsOrdersGridReadOnly = true;
            IsServicesGridReadOnly = true;
            IsUsersGridReadOnly = true;
            _editableItem = null;
        }
    }
}