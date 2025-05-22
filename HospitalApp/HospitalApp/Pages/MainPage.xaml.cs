using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;  
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


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
        private string _selectedOrderStatus;
        private Insurance_Company _selectedInsuranceCompany;
        private string _selectedInsuranceCompanyTitle;
        private decimal _billAmount;
        private decimal _totalBillAmount;
        private List<User> _bills;
        private bool _areTableCheckBoxesEnabled = true;
        private bool _isPatientsGridReadOnly = true;
        private bool _isOrdersGridReadOnly = true;
        private bool _isServicesGridReadOnly = true;
        private bool _isUsersGridReadOnly = true;
        private DataGrid _selectedGrid;
        private object _editableItem;
        private List<int?> _scannedBarcodes;
        private bool _isAdminTabSelected;
        private bool _isReportsTabSelected;
        private DateTime? _analysisDate;
        private bool _isEditingOrAdding = false;
        public static readonly List<string> PolicyTypes = new List<string> { "Standard", "Premium", "Basic" };

        // Свойство для определения формата таблицы (табличный/текстовой)
        public bool IsTableFormat
        {
            get => _isTableFormat;
            set { _isTableFormat = value; OnPropertyChanged(nameof(IsTableFormat)); }
        }

        // Свойство для определения видимости опции формата таблицы
        // (доступно только для Word и PDF форматов)
        public bool IsTableFormatVisible
        {
            get => _selectedFormat == "Word" || _selectedFormat == "PDF";
        }

        // Свойство для даты анализа с комбинированием выбранной даты и текущего времени
        public DateTime? AnalysisDate
        {
            get => _analysisDate;
            set
            {
                if (value.HasValue)
                {
                    DateTime currentTime = DateTime.Now;
                    _analysisDate = new DateTime(
                        value.Value.Year,
                        value.Value.Month,
                        value.Value.Day,
                        currentTime.Hour,
                        currentTime.Minute,
                        currentTime.Second);
                }
                else
                {
                    _analysisDate = value;
                }
                OnPropertyChanged(nameof(AnalysisDate));
            }
        }

        // Свойство для выбранного формата отчета (Word/Excel/PDF)
        public string SelectedFormat
        {
            get => _selectedFormat;
            set
            {
                _selectedFormat = value;
                if (value == "Excel")
                    IsTableFormat = false;
                OnPropertyChanged(nameof(SelectedFormat));
                OnPropertyChanged(nameof(IsTableFormatVisible));
            }
        }

        // Свойство для выбора всех таблиц одновременно
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

        // Свойство для выбора таблицы пациентов
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

        // Свойство для выбора таблицы заказов
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

        // Свойство для выбора таблицы услуг
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

        // Свойство для выбора таблицы пользователей
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

        // Свойство для определения доступности чекбоксов таблиц
        public bool AreTableCheckBoxesEnabled
        {
            get => _areTableCheckBoxesEnabled;
            set { _areTableCheckBoxesEnabled = value; OnPropertyChanged(nameof(AreTableCheckBoxesEnabled)); }
        }

        // Свойство для списка записей пациентов
        public List<Pacient> PatientRecords
        {
            get => _patientRecords;
            set { _patientRecords = value; OnPropertyChanged(nameof(PatientRecords)); }
        }

        // Свойство для списка записей заказов
        public List<Order> OrderRecords
        {
            get => _orderRecords;
            set { _orderRecords = value; OnPropertyChanged(nameof(OrderRecords)); }
        }

        // Свойство для списка записей услуг
        public List<Service> ServiceRecords
        {
            get => _serviceRecords;
            set { _serviceRecords = value; OnPropertyChanged(nameof(ServiceRecords)); }
        }

        // Свойство для списка записей пользователей
        public List<User> UserRecords
        {
            get => _userRecords;
            set { _userRecords = value; OnPropertyChanged(nameof(UserRecords)); }
        }

        // Свойство для выбранной записи пациента
        public Pacient SelectedPatientRecord
        {
            get => _selectedPatientRecord;
            set { _selectedPatientRecord = value; OnPropertyChanged(nameof(SelectedPatientRecord)); }
        }

        // Свойство для выбранной записи заказа
        public Order SelectedOrderRecord
        {
            get => _selectedOrderRecord;
            set { _selectedOrderRecord = value; OnPropertyChanged(nameof(SelectedOrderRecord)); }
        }

        // Свойство для выбранной записи услуги
        public Service SelectedServiceRecord
        {
            get => _selectedServiceRecord;
            set { _selectedServiceRecord = value; OnPropertyChanged(nameof(SelectedServiceRecord)); }
        }

        // Свойство для выбранной записи пользователя
        public User SelectedUserRecord
        {
            get => _selectedUserRecord;
            set { _selectedUserRecord = value; OnPropertyChanged(nameof(SelectedUserRecord)); }
        }

        // Свойство для начальной даты периода отчетов
        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(nameof(StartDate)); }
        }

        // Свойство для конечной даты периода отчетов
        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(nameof(EndDate)); }
        }

        // Свойство для предлагаемого штрих-кода
        public string SuggestedBarcode
        {
            get => _suggestedBarcode;
            set { _suggestedBarcode = value; OnPropertyChanged(nameof(SuggestedBarcode)); }
        }

        // Свойство для списка анализаторов
        public List<Analyzer> Analyzers
        {
            get => _analyzers;
            set { _analyzers = value; OnPropertyChanged(nameof(Analyzers)); }
        }

        // Свойство для видимости кнопок администратора
        public Visibility AdminButtonsVisibility
        {
            get => _adminButtonsVisibility;
            set { _adminButtonsVisibility = value; OnPropertyChanged(nameof(AdminButtonsVisibility)); }
        }

        // Свойство для видимости комбобокса записей пациентов
        public Visibility PatientsRecordsComboVisible
        {
            get => (PatientsTableSelected && !AllTablesSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Свойство для видимости комбобокса записей заказов
        public Visibility OrdersRecordsComboVisible
        {
            get => (OrdersTableSelected && !AllTablesSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Свойство для видимости комбобокса записей услуг
        public Visibility ServicesRecordsComboVisible
        {
            get => (ServicesTableSelected && !AllTablesSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Свойство для видимости комбобокса записей пользователей
        public Visibility UsersRecordsComboVisible
        {
            get => (UsersTableSelected && !AllTablesSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Свойство для выбранной оказанной услуги
        public Service_Provided SelectedServiceProvided
        {
            get => _selectedServiceProvided;
            set { _selectedServiceProvided = value; OnPropertyChanged(nameof(SelectedServiceProvided)); }
        }

        // Свойство для выбранного статуса заказа
        public string SelectedOrderStatus
        {
            get => _selectedOrderStatus;
            set { _selectedOrderStatus = value; OnPropertyChanged(nameof(SelectedOrderStatus)); }
        }

        // Свойство для выбранной страховой компании
        public Insurance_Company SelectedInsuranceCompany
        {
            get => _selectedInsuranceCompany;
            set
            {
                _selectedInsuranceCompany = value;
                SelectedInsuranceCompanyTitle = value?.Title;
                InsuranceCompanyTextBox.IsEnabled = value != null;
                OnPropertyChanged(nameof(SelectedInsuranceCompany));
            }
        }

        // Свойство для названия выбранной страховой компании
        public string SelectedInsuranceCompanyTitle
        {
            get => _selectedInsuranceCompanyTitle;
            set { _selectedInsuranceCompanyTitle = value; OnPropertyChanged(nameof(SelectedInsuranceCompanyTitle)); }
        }

        // Свойство для суммы счета
        public decimal BillAmount
        {
            get => _billAmount;
            set { _billAmount = value; OnPropertyChanged(nameof(BillAmount)); }
        }

        // Свойство для общей суммы счетов
        public decimal TotalBillAmount
        {
            get => _totalBillAmount;
            set { _totalBillAmount = value; OnPropertyChanged(nameof(TotalBillAmount)); }
        }

        // Свойство для списка счетов
        public List<User> Bills
        {
            get => _bills;
            set { _bills = value; OnPropertyChanged(nameof(Bills)); }
        }

        // Свойство для определения режима "только чтение" таблицы пациентов
        public bool IsPatientsGridReadOnly
        {
            get => _isPatientsGridReadOnly;
            set { _isPatientsGridReadOnly = value; OnPropertyChanged(nameof(IsPatientsGridReadOnly)); }
        }

        // Свойство для определения режима "только чтение" таблицы заказов
        public bool IsOrdersGridReadOnly
        {
            get => _isOrdersGridReadOnly;
            set { _isOrdersGridReadOnly = value; OnPropertyChanged(nameof(IsOrdersGridReadOnly)); }
        }

        // Свойство для определения режима "только чтение" таблицы услуг
        public bool IsServicesGridReadOnly
        {
            get => _isServicesGridReadOnly;
            set { _isServicesGridReadOnly = value; OnPropertyChanged(nameof(IsServicesGridReadOnly)); }
        }

        // Свойство для определения режима "только чтение" таблицы пользователей
        public bool IsUsersGridReadOnly
        {
            get => _isUsersGridReadOnly;
            set { _isUsersGridReadOnly = value; OnPropertyChanged(nameof(IsUsersGridReadOnly)); }
        }

        // Свойство для определения выбранной вкладки администратора
        public bool IsAdminTabSelected
        {
            get => _isAdminTabSelected;
            set { _isAdminTabSelected = value; OnPropertyChanged(nameof(IsAdminTabSelected)); }
        }

        // Свойство для определения выбранной вкладки отчетов
        public bool IsReportsTabSelected
        {
            get => _isReportsTabSelected;
            set { _isReportsTabSelected = value; OnPropertyChanged(nameof(IsReportsTabSelected)); }
        }

        // Событие изменения свойств (реализация INotifyPropertyChanged)
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для вызова события изменения свойства
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Свойство для текста кнопки редактирования ("Изменить"/"Сохранить")
        public string EditButtonContent
        {
            get => _isEditingOrAdding ? "Сохранить" : "Изменить";
            set => OnPropertyChanged(nameof(EditButtonContent));
        }

        // Конструктор главной страницы
        // Инициализирует компоненты и настраивает интерфейс в зависимости от роли пользователя
        public MainPage(string role)
        {
            InitializeComponent();
            _currentRole = role;
            _scannedBarcodes = new List<int?>();
            DataContext = this;
            SetupUIForRole();

            OrdersGrid.ItemsSource = new List<Order>();
            PatientsGrid.ItemsSource = new List<Pacient>();

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

        // Метод для настройки интерфейса в зависимости от роли пользователя
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
                    UpdateLabTables();
                    BarcodeInput.Text = "";
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

        // Метод для настройки выпадающих списков в колонках DataGrid
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

                var insuranceColumn = AdminPatientsGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Страховая компания") as DataGridComboBoxColumn;
                if (insuranceColumn != null)
                    insuranceColumn.ItemsSource = insuranceCompanies;

                var userInsuranceColumn = UsersGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Страховая компания") as DataGridComboBoxColumn;
                if (userInsuranceColumn != null)
                    userInsuranceColumn.ItemsSource = insuranceCompanies;

                var userServiceColumn = UsersGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Услуга") as DataGridComboBoxColumn;
                if (userServiceColumn != null)
                    userServiceColumn.ItemsSource = services;
            }
        }

        // Обработчик нажатия клавиш в таблице счетов (удаление по Delete)
        private void BillsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && BillsGrid.SelectedItem != null)
            {
                var selectedItemType = BillsGrid.SelectedItem.GetType();
                var userProperty = selectedItemType.GetProperty("User");
                var user = userProperty?.GetValue(BillsGrid.SelectedItem) as User;

                if (user == null)
                    return;

                if (MessageBox.Show("Вы уверены, что хотите удалить этот счет?", "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    using (var context = new HospitalBaseEntities())
                    {
                        var userToUpdate = context.User.FirstOrDefault(u => u.User_Id == user.User_Id);
                        if (userToUpdate != null)
                        {
                            var deletedAmount = userToUpdate.Account ?? 0;
                            userToUpdate.Insurance_Company_Id = null;
                            userToUpdate.Account = null;
                            context.SaveChanges();
                            TotalBillAmount -= deletedAmount;
                            LoadBills();
                        }
                    }
                }
                e.Handled = true;
            }
        }

        // Обработчик нажатия клавиш в таблицах лаборанта (удаление по Delete)
        private void LabDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && sender is DataGrid grid && grid.SelectedItem != null)
            {
                if (MessageBox.Show("Удалить запись из таблицы?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (grid == PatientsGrid)
                    {
                        var patient = grid.SelectedItem as Pacient;
                        var ordersToRemove = (OrdersGrid.ItemsSource as IList)?.Cast<Order>()
                            .Where(o => o.Pacient_Id == patient.Pacient_Id).ToList();

                        if (ordersToRemove != null)
                        {
                            foreach (var order in ordersToRemove)
                            {
                                (OrdersGrid.ItemsSource as IList)?.Remove(order);
                                _scannedBarcodes.Remove(order.BarCode);
                            }
                        }
                        (PatientsGrid.ItemsSource as IList)?.Remove(patient);
                    }
                    else if (grid == OrdersGrid)
                    {
                        var order = grid.SelectedItem as Order;
                        _scannedBarcodes.Remove(order.BarCode);
                        (OrdersGrid.ItemsSource as IList)?.Remove(order);

                        var patientOrders = (OrdersGrid.ItemsSource as IList)?.Cast<Order>()
                            .Where(o => o.Pacient_Id == order.Pacient_Id).ToList();

                        if (patientOrders == null || !patientOrders.Any())
                        {
                            var patientToRemove = (PatientsGrid.ItemsSource as IList)?.Cast<Pacient>()
                                .FirstOrDefault(p => p.Pacient_Id == order.Pacient_Id);
                            if (patientToRemove != null)
                                (PatientsGrid.ItemsSource as IList)?.Remove(patientToRemove);
                        }
                    }

                    OrdersGrid.ItemsSource = OrdersGrid.ItemsSource;
                    PatientsGrid.ItemsSource = PatientsGrid.ItemsSource;
                }
                e.Handled = true;
            }
        }

        // Обработчик события выбора всех таблиц
        private void AllTablesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateIndividualCheckBoxes();
        }

        // Обработчик события снятия выбора всех таблиц
        private void AllTablesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateIndividualCheckBoxes();
        }

        // Обработчик события снятия выбора отдельной таблицы
        private void TableCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!PatientsTableSelected || !OrdersTableSelected || !ServicesTableSelected || !UsersTableSelected)
            {
                AllTablesSelected = false;
            }
        }

        // Метод для обновления состояния чекбоксов отдельных таблиц
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

        // Метод для генерации QR-кодов для новых заказов
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
                    var existingBarcodes = context.Order
                        .Where(o => o.BarCode.HasValue)
                        .Select(o => o.BarCode.Value)
                        .ToList();

                    foreach (var order in ordersWithoutBarcode)
                    {
                        try
                        {
                            int newBarcode;
                            do
                            {
                                newBarcode = _random.Next(100000, 999999);
                            }
                            while (existingBarcodes.Contains(newBarcode));

                            order.BarCode = newBarcode;
                            order.Order_Status = false;
                            order.Create_Date = DateTime.Now;

                            string outputPath = Path.Combine("Barcodes", $"Barcode_{order.BarCode}.png");
                            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                            GenerateBarcode(newBarcode.ToString(), outputPath);
                            Debug.WriteLine($"Штрих-код сгенерирован: {outputPath}, код: {newBarcode}");
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

        // Метод для генерации штрих-кода в формате PNG
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
                using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
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

        // Метод для загрузки анализаторов из базы данных
        private void LoadAnalyzers()
        {
            using (var context = new HospitalBaseEntities())
            {
                Analyzers = context.Analyzer.ToList();
            }
        }

        // Метод для загрузки данных об оказанных услугах
        private void LoadServiceProvidedData()
                {
            using (var context = new HospitalBaseEntities())
            {
                var serviceProvided = context.Service_Provided
                    .Include(sp => sp.Service)
                    .Include(sp => sp.User)
                    .Where(sp => sp.Service_Id != 0 && sp.User_Id != 0)
                    .ToList();

                var orders = context.Order
                    .Include(o => o.Pacient)
                    .Include(o => o.Service)
                    .Where(o => o.Pacient_Id != 0 && o.Service_Id != 0)
                    .ToList();

                var displayList = new List<dynamic>();
                foreach (var sp in serviceProvided)
                {
                    var order = orders.FirstOrDefault(o => o.Service_Id == sp.Service_Id);
                    displayList.Add(new
                    {
                        ServiceProvided = sp,
                        Order = order,
                        Pacient = order?.Pacient,
                        Service = sp.Service,
                        User = sp.User,
                        OrderStatus = order?.Order_Status == true ? "Выполнен" : "В работе",
                        CompleteTime = order?.Complete_Time
                    });
                }

                ServiceProvidedGrid.ItemsSource = displayList;
            }
        }

        // Метод для загрузки административных данных (пациенты, заказы, услуги, пользователи)
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
                    .Include(u => u.Service)
                    .Include(u => u.Insurance_Company)
                    .Where(u => u.Full_Name != null && u.Full_Name.Trim() != "")
                    .ToList();
            }
        }

        // Метод для загрузки данных для бухгалтера (страховые компании)
        private void LoadAccountantData()
        {
            using (var context = new HospitalBaseEntities())
            {
                InsuranceCompaniesGrid.ItemsSource = context.Insurance_Company
                    .Where(ic => ic.Title != null && ic.Title.Trim() != "")
                    .ToList();
            }
        }

        // Метод для обработки нажатия кнопки выставления счета
        private void IssueBillButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedInsuranceCompany == null)
            {
                MessageBox.Show("Выберите страховую компанию!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    var currentUser = context.User
                        .Include(u => u.Service)
                        .FirstOrDefault(u => u.User_Id == UserData.CurrentUserId);

                    if (currentUser == null)
                    {
                        MessageBox.Show("Не удалось идентифицировать текущего пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (BillsGrid.SelectedItem != null)
                    {
                        var selectedItem = BillsGrid.SelectedItem;
                        var userProperty = selectedItem.GetType().GetProperty("User");
                        var user = userProperty?.GetValue(selectedItem) as User;

                        if (user != null)
                        {
                            var userToUpdate = context.User.FirstOrDefault(u => u.User_Id == user.User_Id);
                            if (userToUpdate != null)
                            {
                                var previousAmount = userToUpdate.Account ?? 0;
                                userToUpdate.Account = BillAmount;
                                userToUpdate.Insurance_Company_Id = SelectedInsuranceCompany.Insurance_Company_Id;
                                context.SaveChanges();
                                TotalBillAmount = TotalBillAmount - previousAmount + BillAmount;
                            }
                        }
                    }
                    else
                    {
                        currentUser.Insurance_Company_Id = SelectedInsuranceCompany.Insurance_Company_Id;
                        currentUser.Account = BillAmount;
                        context.SaveChanges();
                        TotalBillAmount += BillAmount;
                    }

                    LoadBills();

                    ResetBillForm();

                    MessageBox.Show("Счет успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении счета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetBillForm()
        {
            BillAmount = 0;
            SelectedInsuranceCompany = null;
            BillsGrid.SelectedItem = null;
            InsuranceCompanyTextBox.IsEnabled = false;
        }

        // Метод для загрузки счетов из базы данных
        private void LoadBills()
        {
            using (var context = new HospitalBaseEntities())
            {
                var billData = context.User
                    .Include(u => u.Insurance_Company)
                    .Where(u => u.Account.HasValue && u.Account > 0 && u.Insurance_Company_Id.HasValue)
                    .ToList()
                    .Select(u => new
                    {
                        User = u,
                        Insurance_Company = u.Insurance_Company,
                        Account = u.Account
                    })
                    .ToList();

                BillsGrid.ItemsSource = billData;
                TotalBillAmount = billData.Sum(b => b.Account ?? 0);
            }
        }

        // Метод для обработки ввода штрих-кода
        private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ScanButton_Click(sender, e);
            }
        }

        // Метод для обработки нажатия кнопки сканирования
        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            string barcodeText = BarcodeInput.Text.Trim();
            if (string.IsNullOrEmpty(barcodeText))
            {
                MessageBox.Show("Введите штрих-код!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(barcodeText, out int barcode))
            {
                MessageBox.Show("Штрих-код должен быть числом!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                BarcodeInput.Text = "";
                return;
            }

            using (var context = new HospitalBaseEntities())
            {
                var order = context.Order
                    .Include(o => o.Pacient)
                    .Include(o => o.Service)
                    .FirstOrDefault(o => o.BarCode == barcode && o.Order_Status == false);

                if (order == null)
                {
                    MessageBox.Show("Заказ не найден или уже выполнен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    BarcodeInput.Text = "";
                    return;
                }

                if (_scannedBarcodes.Contains(barcode))
                {
                    MessageBox.Show("Этот штрих-код уже был отсканирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    BarcodeInput.Text = "";
                    return;
                }

                _scannedBarcodes.Add(barcode);
                UpdateLabTables();
                BarcodeInput.Text = "";
                MessageBox.Show($"Штрих-код {barcode} успешно отсканирован", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Метод для обновления таблиц лаборанта
        private void UpdateLabTables()
        {
            if (_scannedBarcodes == null || !_scannedBarcodes.Any())
            {
                OrdersGrid.ItemsSource = new List<Order>();
                PatientsGrid.ItemsSource = new List<Pacient>();
                return;
            }

            using (var context = new HospitalBaseEntities())
            {
                var orders = context.Order
                    .Include(o => o.Pacient)
                    .Include(o => o.Service)
                    .Where(o => o.Order_Status == false &&
                               o.BarCode.HasValue &&
                               _scannedBarcodes.Contains(o.BarCode.Value))
                    .ToList();

                OrdersGrid.ItemsSource = orders;
                PatientsGrid.ItemsSource = orders.Select(o => o.Pacient).Distinct().ToList();
            }
        }

        // Метод для обработки нажатия кнопки получения биоматериалов
        private void ReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new HospitalBaseEntities())
            {
                var unscannedOrder = context.Order
                    .Where(o => o.BarCode.HasValue && o.Order_Status == false && !_scannedBarcodes.Contains(o.BarCode.Value))
                    .OrderBy(o => o.Order_Id)
                    .FirstOrDefault();

                if (unscannedOrder == null)
                {
                    MessageBox.Show("Нет доступных штрих-кодов для сканирования!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            var receiveWindow = new Window
            {
                Title = "Получение биоматериалов",
                MinWidth = 300,
                MaxWidth = 400,
                MinHeight = 250,
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

            string tempImagePath = Path.Combine(Path.GetTempPath(), $"temp_barcode_{Guid.NewGuid()}.png");

            try
            {
                using (var context = new HospitalBaseEntities())
                {
                    var unscannedOrder = context.Order
                        .Where(o => o.BarCode.HasValue && o.Order_Status == false && !_scannedBarcodes.Contains(o.BarCode))
                        .OrderBy(o => o.Order_Id)
                        .FirstOrDefault();

                    if (unscannedOrder == null)
                    {
                        MessageBox.Show("Все штрих-коды уже отсканированы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        receiveWindow.Close();
                        return;
                    }

                    string barcodeString = unscannedOrder.BarCode.ToString();
                    GenerateBarcode(barcodeString, tempImagePath);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(tempImagePath);
                    bitmap.EndInit();
                    barcodeImage.Source = bitmap;

                    File.Delete(tempImagePath);

                    scanButton.Click += (s, args) =>
                    {
                        string barcodeText = barcodeTextBox.Text.Trim();
                        if (string.IsNullOrEmpty(barcodeText) || !int.TryParse(barcodeText, out int barcode))
                        {
                            MessageBox.Show("Некорректный штрих-код!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            barcodeTextBox.Text = "";
                            return;
                        }

                        using (var scanContext = new HospitalBaseEntities())
                        {
                            var order = scanContext.Order
                                .FirstOrDefault(o => o.BarCode == barcode && o.Order_Status == false);

                            if (order != null)
                            {
                                if (_scannedBarcodes.Contains(barcode))
                                {
                                    MessageBox.Show($"Штрих-код {barcodeText} уже был отсканирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    barcodeTextBox.Text = "";
                                    return;
                                }

                                _scannedBarcodes.Add(barcode);
                                order.Order_Status = false;
                                scanContext.SaveChanges();
                                UpdateLabTables();
                                barcodeTextBox.Text = "";
                                MessageBox.Show($"Штрих-код успешно отсканирован: {barcodeText}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                receiveWindow.Close();
                            }
                            else
                            {
                                MessageBox.Show("Заказ с таким штрих-кодом не найден или уже завершен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                barcodeTextBox.Text = "";
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
                }

                receiveWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации штрих-кода: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                try { if (File.Exists(tempImagePath)) File.Delete(tempImagePath); } catch { }
                receiveWindow.Close();
            }
        }

        // Метод для обработки изменения выбранной оказанной услуги
        private void ServiceProvidedGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServiceProvidedGrid.SelectedItem != null)
            {
                var selected = ServiceProvidedGrid.SelectedItem.GetType().GetProperty("ServiceProvided").GetValue(ServiceProvidedGrid.SelectedItem) as Service_Provided;
                var order = ServiceProvidedGrid.SelectedItem.GetType().GetProperty("Order").GetValue(ServiceProvidedGrid.SelectedItem) as Order;
                SelectedServiceProvided = selected;
                SelectedOrderStatus = order?.Order_Status == true ? "Выполнен" : "В работе";
            }
            else
            {
                SelectedServiceProvided = null;
                SelectedOrderStatus = null;
            }
        }

        // Метод для обработки нажатия кнопки анализа
        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedServiceProvided == null)
            {
                MessageBox.Show("Выберите запись для обновления статуса!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(SelectedOrderStatus))
            {
                MessageBox.Show("Выберите статус!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AnalysisDate == null)
            {
                MessageBox.Show("Укажите дату анализа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new HospitalBaseEntities())
            {
                var order = context.Order
                    .FirstOrDefault(o => o.Service_Id == SelectedServiceProvided.Service_Id);
                if (order != null)
                {
                    bool newStatus = SelectedOrderStatus == "Выполнен";
                    if (order.Order_Status != newStatus)
                    {
                        order.Order_Status = newStatus;
                        DateTime currentTime = DateTime.Now;
                        order.Complete_Time = new DateTime(
                            AnalysisDate.Value.Year,
                            AnalysisDate.Value.Month,
                            AnalysisDate.Value.Day,
                            currentTime.Hour,
                            currentTime.Minute,
                            currentTime.Second);
                        context.SaveChanges();
                        LoadServiceProvidedData();
                        if (_currentRole == "Лаборант")
                            UpdateLabTables();
                        MessageBox.Show("Статус успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Статус не изменился.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Заказ не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Метод для обработки изменения выбранной страховой компании
        private void InsuranceCompaniesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InsuranceCompaniesGrid.SelectedItem is Insurance_Company selected)
            {
                SelectedInsuranceCompany = selected;
                InsuranceCompanyTextBox.Text = selected.Title;
                InsuranceCompanyTextBox.IsEnabled = true;
                BillAmount = 0;
                BillsGrid.SelectedItem = null;
            }
        }

        // Метод для обработки изменения выбранного счета
        private void BillsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BillsGrid.SelectedItem != null)
            {
                var selectedItem = BillsGrid.SelectedItem;
                var userProperty = selectedItem.GetType().GetProperty("User");
                var user = userProperty?.GetValue(selectedItem) as User;

                if (user != null)
                {
                    using (var context = new HospitalBaseEntities())
                    {
                        var fullUser = context.User
                            .Include(u => u.Insurance_Company)
                            .FirstOrDefault(u => u.User_Id == user.User_Id);

                        if (fullUser != null)
                        {
                            SelectedInsuranceCompany = fullUser.Insurance_Company;
                            BillAmount = fullUser.Account ?? 0;
                        }
                    }
                }
            }
        }

        // Метод для загрузки записей отчетов
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

        // Метод для обработки изменения формата отчета
        private void ReportFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportFormatCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                SelectedFormat = selectedItem.Content.ToString();
            }
        }

        // Метод для генерации отчета
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

        // Метод для обработки нажатия кнопки удаления
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGrid == null || _selectedGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HospitalBaseEntities())
                    {
                        var selectedItem = _selectedGrid.SelectedItem;

                        if (selectedItem is Pacient patient)
                        {
                            if (patient.Pacient_Id == 0) // Новая, несохраненная запись
                            {
                                ((IList)_selectedGrid.ItemsSource).Remove(selectedItem);
                            }
                            else
                            {
                                var dbPatient = context.Pacient.Find(patient.Pacient_Id);
                                if (dbPatient != null)
                                {
                                    context.Pacient.Remove(dbPatient);
                                    context.SaveChanges();
                                }
                            }
                        }
                        else if (selectedItem is Order order)
                        {
                            if (order.Order_Id == 0)
                            {
                                ((IList)_selectedGrid.ItemsSource).Remove(selectedItem);
                            }
                            else
                            {
                                var dbOrder = context.Order.Find(order.Order_Id);
                                if (dbOrder != null)
                                {
                                    context.Order.Remove(dbOrder);
                                    context.SaveChanges();
                                }
                            }
                        }
                        else if (selectedItem is Service service)
                        {
                            if (service.Service_Id == 0)
                            {
                                ((IList)_selectedGrid.ItemsSource).Remove(selectedItem);
                            }
                            else
                            {
                                var dbService = context.Service.Find(service.Service_Id);
                                if (dbService != null)
                                {
                                    context.Service.Remove(dbService);
                                    context.SaveChanges();
                                }
                            }
                        }
                        else if (selectedItem is User user)
                        {
                            if (user.User_Id == 0)
                            {
                                ((IList)_selectedGrid.ItemsSource).Remove(selectedItem);
                            }
                            else
                            {
                                var dbUser = context.User.Find(user.User_Id);
                                if (dbUser != null)
                                {
                                    context.User.Remove(dbUser);
                                    context.SaveChanges();
                                }
                            }
                        }

                        LoadAdminData();
                        ResetEditMode();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Метод для обработки завершения редактирования ячейки DataGrid
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && _editableItem != null)
            {
                // Валидация полей
                if (_editableItem is Pacient validatedPatient)
                {
                    if (!string.IsNullOrEmpty(validatedPatient.Policy) && !Regex.IsMatch(validatedPatient.Policy, @"^\d{1,10}$"))
                    {
                        MessageBox.Show("Полис должен содержать только цифры и быть не длиннее 10 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                        return;
                    }
                    if (!string.IsNullOrEmpty(validatedPatient.Phone_Number) && !Regex.IsMatch(validatedPatient.Phone_Number, @"^\d{1,10}$"))
                    {
                        MessageBox.Show("Телефон должен содержать только цифры и быть не длиннее 10 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                        return;
                    }
                    if (!string.IsNullOrEmpty(validatedPatient.Policy_Type) && !MainPage.PolicyTypes.Contains(validatedPatient.Policy_Type))
                    {
                        MessageBox.Show("Тип полиса должен быть Standard, Premium или Basic!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                        return;
                    }
                }
                else if (_editableItem is Service service)
                {
                    if (service.Deviation > 100m)
                    {
                        MessageBox.Show("Допуск не может превышать 100%!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                        return;
                    }
                }

                using (var context = new HospitalBaseEntities())
                {
                    try
                    {
                        object entityToUpdate = null;
                        if (_selectedGrid == AdminPatientsGrid)
                        {
                            var editedPatient = _editableItem as Pacient;
                            entityToUpdate = context.Pacient.Find(editedPatient.Pacient_Id) ?? editedPatient;
                            if (context.Entry(entityToUpdate).State == EntityState.Detached)
                            {
                                context.Pacient.Add((Pacient)entityToUpdate);
                            }
                            else
                            {
                                context.Entry(entityToUpdate).State = EntityState.Modified;
                            }
                            if (editedPatient.Insurance_Company_Id != 0)
                            {
                                var insuranceCompany = context.Insurance_Company.Local.FirstOrDefault(ic => ic.Insurance_Company_Id == editedPatient.Insurance_Company_Id);
                                if (insuranceCompany == null)
                                {
                                    insuranceCompany = context.Insurance_Company.Find(editedPatient.Insurance_Company_Id);
                                    if (insuranceCompany == null)
                                    {
                                        insuranceCompany = new Insurance_Company
                                        {
                                            Insurance_Company_Id = editedPatient.Insurance_Company_Id,
                                            Title = "Новая страховая компания"
                                        };
                                        context.Insurance_Company.Add(insuranceCompany);
                                    }
                                }
                            }
                        }
                        else if (_selectedGrid == AdminOrdersGrid)
                        {
                            var order = _editableItem as Order;
                            entityToUpdate = context.Order.Find(order.Order_Id) ?? order;
                            context.Order.Attach((Order)entityToUpdate);
                            context.Entry(entityToUpdate).CurrentValues.SetValues(order);
                        }
                        else if (_selectedGrid == ServicesGrid)
                        {
                            var service = _editableItem as Service;
                            entityToUpdate = context.Service.Find(service.Service_Id) ?? service;
                            context.Service.Attach((Service)entityToUpdate);
                            context.Entry(entityToUpdate).CurrentValues.SetValues(service);
                        }
                        else if (_selectedGrid == UsersGrid)
                        {
                            var user = _editableItem as User;
                            entityToUpdate = context.User.Find(user.User_Id) ?? user;
                            context.User.Attach((User)entityToUpdate);
                            context.Entry(entityToUpdate).CurrentValues.SetValues(user);
                        }

                        if (context.Entry(entityToUpdate).State == EntityState.Detached && _selectedGrid != AdminPatientsGrid)
                        {
                            if (_selectedGrid == AdminOrdersGrid) context.Order.Add((Order)entityToUpdate);
                            else if (_selectedGrid == ServicesGrid) context.Service.Add((Service)entityToUpdate);
                            else if (_selectedGrid == UsersGrid) context.User.Add((User)entityToUpdate);
                        }
                        else if (_selectedGrid != AdminPatientsGrid)
                        {
                            context.Entry(entityToUpdate).State = EntityState.Modified;
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

        // Метод для сброса режима редактирования
        private void ResetEditMode()
        {
            IsPatientsGridReadOnly = true;
            IsOrdersGridReadOnly = true;
            IsServicesGridReadOnly = true;
            IsUsersGridReadOnly = true;
            _editableItem = null;
            _selectedGrid = null;
        }

        // Метод для обработки нажатия кнопки добавления
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGrid == null)
            {
                MessageBox.Show("Выберите таблицу для добавления записи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string entityType = GetEntityType(_selectedGrid);
            if (entityType == null)
            {
                MessageBox.Show("Неизвестный тип таблицы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new HospitalBaseEntities())
            {
                var editWindow = new EditWindow(null, entityType, context);
                if (editWindow.ShowDialog() == true)
                {
                    LoadAdminData();
                    MessageBox.Show("Запись успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Метод для обработки нажатия кнопки редактирования
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGrid == null || _selectedGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для редактирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string entityType = GetEntityType(_selectedGrid);
            if (entityType == null)
            {
                MessageBox.Show("Неизвестный тип таблицы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new HospitalBaseEntities())
            {
                var item = _selectedGrid.SelectedItem;
                var editWindow = new EditWindow(item, entityType, context);
                if (editWindow.ShowDialog() == true)
                {
                    LoadAdminData();
                    MessageBox.Show("Запись успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Метод для обработки изменения выбранного элемента DataGrid
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedGrid = sender as DataGrid;
            _editableItem = _selectedGrid?.SelectedItem;
        }

        // Метод для получения типа сущности по DataGrid
        private string GetEntityType(DataGrid grid)
        {
            if (grid == AdminPatientsGrid)
                return "Pacient";
            if (grid == AdminOrdersGrid)
                return "Order";
            if (grid == ServicesGrid)
                return "Service";
            if (grid == UsersGrid)
                return "User";
            return null;
        }

        // Метод для обработки нажатия клавиш в DataGrid
        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && sender is DataGrid grid && grid.SelectedItem != null && _currentRole == "Лаборант-Администратор")
            {
                _selectedGrid = grid;
                DeleteButton_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}