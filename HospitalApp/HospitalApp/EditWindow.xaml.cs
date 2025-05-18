using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace HospitalApp
{
    public partial class EditWindow : Window
    {
        private readonly object _entity;
        private readonly Type _entityType;
        private readonly Dictionary<string, Control> _controls = new Dictionary<string, Control>();
        private readonly HospitalBaseEntities _context = new HospitalBaseEntities();
        private readonly Random _random = new Random();

        public EditWindow(object entity, Type entityType)
        {
            InitializeComponent();
            _entity = entity;
            _entityType = entityType;
            Title = entity == null ? $"Добавление {GetEntityName()}" : $"Редактирование {GetEntityName()}";
            GenerateFields();
        }

        private string GetEntityName()
        {
            switch (_entityType.Name)
            {
                case "Pacient": return "Пациента";
                case "Service": return "Услуги";
                case "User": return "Пользователя";
                case "Order": return "Заказа";
                case "Service_Provided": return "Предоставленной услуги";
                default: return "Записи";
            }
        }

        private void GenerateFields()
        {
            var properties = _entityType.GetProperties()
                .Where(p => p.Name != "Insurance_Company" && p.Name != "Pacient" && p.Name != "Service" && p.Name != "Role" && p.Name != "Analyzer")
                .OrderBy(p => GetFieldOrder(p.Name));

            foreach (var prop in properties)
            {
                if (_entityType == typeof(Order) && prop.Name == "BarCode") continue; // Штрих-код не редактируется

                var label = new TextBlock
                {
                    Text = GetFriendlyName(prop.Name) + (IsRequired(prop.Name) ? " *" : ""),
                    Margin = new Thickness(0, 5, 0, 2)
                };
                FieldsPanel.Children.Add(label);

                Control control = null;
                if (_entityType == typeof(Pacient) && prop.Name == "Policy_Type")
                {
                    var comboBox = new ComboBox
                    {
                        ItemsSource = new List<string> { "ОМС", "ДМС" },
                        Margin = new Thickness(0, 0, 0, 10),
                        SelectedItem = _entity != null ? prop.GetValue(_entity)?.ToString() : null
                    };
                    control = comboBox;
                }
                else if (prop.PropertyType == typeof(bool?))
                {
                    var comboBox = new ComboBox
                    {
                        ItemsSource = new List<string> { "В работе", "Готово" },
                        Margin = new Thickness(0, 0, 0, 10),
                        SelectedIndex = _entity != null && (bool?)prop.GetValue(_entity) == true ? 1 : 0
                    };
                    control = comboBox;
                }
                else if (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime))
                {
                    var datePicker = new DatePicker
                    {
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    if (_entity != null && prop.GetValue(_entity) != null)
                        datePicker.SelectedDate = (DateTime?)prop.GetValue(_entity);
                    else if (prop.Name != "Birth_Date" && prop.Name != "Last_Login_Date")
                        datePicker.SelectedDate = DateTime.Now;
                    control = datePicker;
                }
                else if (prop.Name.EndsWith("_Id") && prop.PropertyType == typeof(int))
                {
                    var comboBox = new ComboBox
                    {
                        Margin = new Thickness(0, 0, 0, 10),
                        DisplayMemberPath = GetDisplayMemberPath(prop.Name),
                        SelectedValuePath = GetSelectedValuePath(prop.Name)
                    };
                    LoadComboBoxData(comboBox, prop.Name);
                    if (_entity != null)
                        comboBox.SelectedValue = prop.GetValue(_entity);
                    control = comboBox;
                }
                else if (prop.PropertyType == typeof(decimal?) || prop.PropertyType == typeof(decimal))
                {
                    var textBox = new TextBox
                    {
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    if (_entity != null && prop.GetValue(_entity) != null)
                        textBox.Text = ((decimal)prop.GetValue(_entity)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    control = textBox;
                }
                else
                {
                    var textBox = new TextBox
                    {
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    if (_entity != null)
                        textBox.Text = prop.GetValue(_entity)?.ToString();
                    control = textBox;
                }

                _controls[prop.Name] = control;
                FieldsPanel.Children.Add(control);
            }
        }

        private int GetFieldOrder(string propName)
        {
            var order = new Dictionary<string, int>
            {
                { "Full_Name", 1 }, { "Birth_Date", 2 }, { "Passport", 3 }, { "Phone_Number", 4 },
                { "Email", 5 }, { "Policy", 6 }, { "Policy_Type", 7 }, { "Insurance_Company_Id", 8 },
                { "Order_Id", 1 }, { "Pacient_Id", 2 }, { "Service_Id", 3 }, { "Create_Date", 4 },
                { "Order_Status", 5 }, { "Complete_Time", 6 }, { "BarCode", 7 }, { "Title", 1 },
                { "Price", 2 }, { "Deadline", 3 }, { "Deviation", 4 }, { "Login", 1 }, { "Password", 2 },
                { "Role_Id", 3 }, { "Last_Login_Date", 4 }, { "User_Id", 1 }, { "Service_Provided_Id", 2 },
                { "Receipt_Date", 3 }, { "Execution_Date", 4 }, { "Analyzer_Id", 5 }
            };
            return order.ContainsKey(propName) ? order[propName] : 100;
        }

        private bool IsRequired(string propName)
        {
            if (_entityType == typeof(Pacient))
                return propName == "Full_Name" || propName == "Birth_Date" || propName == "Policy" || propName == "Policy_Type" || propName == "Insurance_Company_Id";
            if (_entityType == typeof(Service))
                return propName == "Title" || propName == "Price";
            if (_entityType == typeof(User))
                return propName == "Full_Name" || propName == "Login" || propName == "Role_Id";
            if (_entityType == typeof(Order))
                return propName == "Pacient_Id" || propName == "Service_Id";
            if (_entityType == typeof(Service_Provided))
                return propName == "User_Id" || propName == "Service_Id" || propName == "Analyzer_Id";
            return false;
        }

        private string GetFriendlyName(string propName)
        {
            switch (propName)
            {
                case "Pacient_Id": return "Пациент";
                case "Full_Name": return "ФИО";
                case "Birth_Date": return "Дата рождения";
                case "Passport": return "Паспорт";
                case "Phone_Number": return "Телефон";
                case "Email": return "E-mail";
                case "Policy": return "Полис";
                case "Policy_Type": return "Тип полиса";
                case "Insurance_Company_Id": return "Страховая компания";
                case "Service_Id": return "Услуга";
                case "Title": return "Название";
                case "Price": return "Цена";
                case "Deadline": return "Срок (дни)";
                case "Deviation": return "Допуск";
                case "User_Id": return "Пользователь";
                case "Role_Id": return "Роль";
                case "Login": return "Логин";
                case "Password": return "Пароль";
                case "Last_Login_Date": return "Последний вход";
                case "Order_Id": return "ID Заказа";
                case "Create_Date": return "Дата создания";
                case "Order_Status": return "Статус";
                case "Complete_Time": return "Дата завершения";
                case "BarCode": return "Штрих-код";
                case "Service_Provided_Id": return "ID Услуги";
                case "Receipt_Date": return "Дата начала";
                case "Execution_Date": return "Дата окончания";
                case "Analyzer_Id": return "Анализатор";
                default: return propName;
            }
        }

        private string GetDisplayMemberPath(string propName)
        {
            switch (propName)
            {
                case "Insurance_Company_Id": return "Title";
                case "Pacient_Id": return "Full_Name";
                case "Service_Id": return "Title";
                case "Role_Id": return "Name";
                case "User_Id": return "Full_Name";
                case "Analyzer_Id": return "Analyzer_Id";
                default: return "Id";
            }
        }

        private string GetSelectedValuePath(string propName)
        {
            switch (propName)
            {
                case "Insurance_Company_Id": return "Insurance_Company_Id";
                case "Pacient_Id": return "Pacient_Id";
                case "Service_Id": return "Service_Id";
                case "Role_Id": return "Role_Id";
                case "User_Id": return "User_Id";
                case "Analyzer_Id": return "Analyzer_Id";
                default: return "Id";
            }
        }

        private void LoadComboBoxData(ComboBox comboBox, string propName)
        {
            try
            {
                switch (propName)
                {
                    case "Insurance_Company_Id":
                        comboBox.ItemsSource = _context.Insurance_Company
                            .Where(ic => ic.Title != null && ic.Title.Trim() != "")
                            .ToList();
                        break;
                    case "Pacient_Id":
                        comboBox.ItemsSource = _context.Pacient
                            .Where(p => p.Full_Name != null && p.Full_Name.Trim() != "")
                            .ToList();
                        break;
                    case "Service_Id":
                        comboBox.ItemsSource = _context.Service
                            .Where(s => s.Title != null && s.Title.Trim() != "")
                            .ToList();
                        break;
                    case "Role_Id":
                        comboBox.ItemsSource = _context.Role.ToList();
                        break;
                    case "User_Id":
                        comboBox.ItemsSource = _context.User
                            .Where(u => u.Full_Name != null && u.Full_Name.Trim() != "")
                            .ToList();
                        break;
                    case "Analyzer_Id":
                        comboBox.ItemsSource = _context.Analyzer.ToList();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateFields(out string errorMessage)
        {
            errorMessage = "";
            foreach (var prop in _entityType.GetProperties())
            {
                if (!_controls.ContainsKey(prop.Name))
                    continue;

                var control = _controls[prop.Name];
                if (IsRequired(prop.Name))
                {
                    if (control is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        errorMessage = $"Поле '{GetFriendlyName(prop.Name)}' обязательно";
                        return false;
                    }
                    if (control is ComboBox comboBox && comboBox.SelectedItem == null)
                    {
                        errorMessage = $"Поле '{GetFriendlyName(prop.Name)}' обязательно";
                        return false;
                    }
                    if (control is DatePicker datePicker && !datePicker.SelectedDate.HasValue)
                    {
                        errorMessage = $"Поле '{GetFriendlyName(prop.Name)}' обязательно";
                        return false;
                    }
                }

                if (_entityType == typeof(Pacient))
                {
                    if (prop.Name == "Phone_Number" && control is TextBox phoneTextBox && !string.IsNullOrWhiteSpace(phoneTextBox.Text))
                    {
                        if (!Regex.IsMatch(phoneTextBox.Text, @"^\+?\d{10,15}$"))
                        {
                            errorMessage = "Некорректный формат телефона (пример: +79991234567)";
                            return false;
                        }
                    }
                    if (prop.Name == "Email" && control is TextBox emailTextBox && !string.IsNullOrWhiteSpace(emailTextBox.Text))
                    {
                        if (!Regex.IsMatch(emailTextBox.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            errorMessage = "Некорректный формат E-mail";
                            return false;
                        }
                    }
                    if (prop.Name == "Passport" && control is TextBox passportTextBox && !string.IsNullOrWhiteSpace(passportTextBox.Text))
                    {
                        if (!Regex.IsMatch(passportTextBox.Text, @"^\d{4}\s?\d{6}$"))
                        {
                            errorMessage = "Некорректный формат паспорта (пример: 1234 567890)";
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateFields(out string errorMessage))
                {
                    ValidationMessage.Text = errorMessage;
                    ValidationMessage.Visibility = Visibility.Visible;
                    return;
                }

                object entity;
                if (_entity == null)
                {
                    entity = Activator.CreateInstance(_entityType);
                    _context.Set(_entityType).Add(entity);
                }
                else
                {
                    entity = _entity;
                    _context.Entry(entity).State = EntityState.Modified;
                }

                // Rest of your property setting code...

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ValidationMessage.Text = $"Ошибка сохранения: {ex.Message}";
                ValidationMessage.Visibility = Visibility.Visible;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}