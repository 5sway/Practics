using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HospitalApp
{
    public class EditWindow : Window
    {
        private readonly object _item;
        private readonly string _entityType;
        private readonly HospitalBaseEntities _context;
        private readonly Dictionary<string, Control> _inputControls;

        // Конструктор окна редактирования
        public EditWindow(object item, string entityType, HospitalBaseEntities context)
        {
            _item = item;
            _entityType = entityType;
            _context = context;
            _inputControls = new Dictionary<string, Control>();

            Title = item == null ? $"Добавить {_entityType}" : $"Редактировать {_entityType}";
            Width = 400;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            if (_entityType == "Pacient")
                CreatePatientFields(stackPanel);
            else if (_entityType == "Order")
                CreateOrderFields(stackPanel);
            else if (_entityType == "Service")
                CreateServiceFields(stackPanel);
            else if (_entityType == "User")
                CreateUserFields(stackPanel);

            var saveButton = new Button
            {
                Content = "Сохранить",
                Width = 100,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Foreground = Brushes.White
            };
            saveButton.Click += SaveButton_Click;
            stackPanel.Children.Add(saveButton);

            Content = new ScrollViewer { Content = stackPanel };
        }
        
        // Методы создания полей для разных типов сущностей:
        private void CreatePatientFields(StackPanel panel)
        {
            var pacient = _item as Pacient ?? new Pacient();
            AddTextBox(panel, "Full_Name", "ФИО*", pacient.Full_Name, true);
            AddDatePicker(panel, "Birth_Date", "Дата рождения", pacient.Birth_Date != default ? pacient.Birth_Date : (DateTime?)null);
            AddTextBox(panel, "Passport", "Паспорт", pacient.Passport);
            AddTextBox(panel, "Phone_Number", "Телефон", pacient.Phone_Number, false, true);
            AddTextBox(panel, "Email", "Email", pacient.Email);
            AddTextBox(panel, "Policy", "Полис", pacient.Policy, false, true);
            AddComboBox(panel, "Policy_Type", "Тип полиса*", new List<string> { "Standard", "Premium", "Basic" },
                        null, null, pacient.Policy_Type, true);
            AddComboBox(panel, "Insurance_Company_Id", "Страховая компания", _context.Insurance_Company.ToList(),
                        "Title", "Insurance_Company_Id", pacient.Insurance_Company_Id);
        }
        private void CreateOrderFields(StackPanel panel)
        {
            var order = _item as Order ?? new Order();
            AddDatePicker(panel, "Create_Date", "Дата создания", order.Create_Date != default ? order.Create_Date : (DateTime?)null);
            AddComboBox(panel, "Pacient_Id", "Пациент*", _context.Pacient.ToList(),
                        "Full_Name", "Pacient_Id", order.Pacient_Id, true);
            AddComboBox(panel, "Service_Id", "Услуга*", _context.Service.ToList(),
                        "Title", "Service_Id", order.Service_Id, true);
            AddComboBox(panel, "Order_Status", "Статус", new List<string> { "В работе", "Выполнен" },
                        null, null, (bool)order.Order_Status ? "Выполнен" : "В работе");
            AddDatePicker(panel, "Complete_Time", "Время завершения", order.Complete_Time);
        }
        private void CreateServiceFields(StackPanel panel)
        {
            var service = _item as Service ?? new Service();
            AddTextBox(panel, "Title", "Название*", service.Title, true);
            AddTextBox(panel, "Price", "Цена", service.Price != 0 ? service.Price.ToString() : "");
            AddTextBox(panel, "Deadline", "Срок (дни)", service.Deadline != 0 ? service.Deadline.ToString() : "");
            AddTextBox(panel, "Deviation", "Допуск (%)", service.Deviation != 0 ? (service.Deviation * 100).ToString("N2") : "", false, false, true);
        }
        private void CreateUserFields(StackPanel panel)
        {
            var user = _item as User ?? new User();
            AddTextBox(panel, "Full_Name", "ФИО*", user.Full_Name, true);
            AddTextBox(panel, "Login", "Логин*", user.Login, true);
            AddTextBox(panel, "Password", "Пароль*", user.Password, true);
            AddDatePicker(panel, "Last_Login_Date", "Последний вход", user.Last_Login_Date != default ? user.Last_Login_Date : (DateTime?)null);
            AddComboBox(panel, "Service_Id", "Услуга", _context.Service.ToList(),
                        "Title", "Service_Id", user.Service_Id);
            AddComboBox(panel, "Insurance_Company_Id", "Страховая компания", _context.Insurance_Company.ToList(),
                        "Title", "Insurance_Company_Id", user.Insurance_Company_Id);
            AddTextBox(panel, "Account", "Счет", user.Account != 0 ? user.Account.ToString() : "");
            AddComboBox(panel, "Role_Id", "Роль*", _context.Role.ToList(),
                        "Name", "Role_Id", user.Role_Id, true);
        }

        // Методы добавления элементов управления:
        private void AddTextBox(StackPanel panel, string property, string label, string value, bool isRequired = false, bool isNumeric = false, bool isPercent = false)
        {
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 5) });
            var textBox = new TextBox { Text = value ?? "", Margin = new Thickness(0, 0, 0, 10) };
            if (isRequired)
                textBox.Tag = "Required";
            if (isNumeric)
                textBox.Tag = (textBox.Tag?.ToString() + "|Numeric") ?? "Numeric";
            if (isPercent)
                textBox.Tag = (textBox.Tag?.ToString() + "|Percent") ?? "Percent";
            _inputControls[property] = textBox;
            panel.Children.Add(textBox);
        }
        private void AddDatePicker(StackPanel panel, string property, string label, DateTime? value)
        {
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 5) });
            var datePicker = new DatePicker { SelectedDate = value, Margin = new Thickness(0, 0, 0, 10) };
            _inputControls[property] = datePicker;
            panel.Children.Add(datePicker);
        }
        private void AddComboBox<T>(StackPanel panel, string property, string label, List<T> items, string displayMember, string valueMember, object selectedValue, bool isRequired = false)
        {
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 5) });
            var comboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            if (isRequired)
                comboBox.Tag = "Required";
            comboBox.ItemsSource = items;
            if (displayMember != null)
                comboBox.DisplayMemberPath = displayMember;
            if (valueMember != null)
                comboBox.SelectedValuePath = valueMember;
            comboBox.SelectedValue = selectedValue;
            _inputControls[property] = comboBox;
            panel.Children.Add(comboBox);
        }

        // Валидация введенных данных
        private bool ValidateInputs()
        {
            foreach (var control in _inputControls)
            {
                string tag = control.Value.Tag?.ToString();
                bool isRequired = tag?.Contains("Required") == true;
                bool isNumeric = tag?.Contains("Numeric") == true;
                bool isPercent = tag?.Contains("Percent") == true;

                if (isRequired)
                {
                    if (control.Value is TextBox textbox && string.IsNullOrWhiteSpace(textbox.Text))
                    {
                        MessageBox.Show($"Поле {control.Key} обязательно для заполнения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    if (control.Value is ComboBox comboBox && comboBox.SelectedValue == null)
                    {
                        MessageBox.Show($"Поле {control.Key} обязательно для заполнения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    if (control.Value is DatePicker datePicker && datePicker.SelectedDate == null)
                    {
                        MessageBox.Show($"Поле {control.Key} обязательно для заполнения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }

                if (control.Value is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    if (isNumeric)
                    {
                        if (!Regex.IsMatch(textBox.Text, @"^\d{1,10}$"))
                        {
                            MessageBox.Show($"Поле {control.Key} должно содержать только цифры и быть не длиннее 10 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                    if (isPercent)
                    {
                        if (!decimal.TryParse(textBox.Text, out decimal percent) || percent < 0 || percent > 100)
                        {
                            MessageBox.Show($"Поле {control.Key} должно быть числом от 0 до 100!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        // Обработчик сохранения данных
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInputs())
                    return;

                if (_entityType == "Pacient")
                    SavePatient();
                else if (_entityType == "Order")
                    SaveOrder();
                else if (_entityType == "Service")
                    SaveService();
                else if (_entityType == "User")
                    SaveUser();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}\nВнутренняя ошибка: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Методы сохранения для каждого типа сущности:
        private void SavePatient()
        {
            var pacient = _item as Pacient ?? new Pacient();
            if (_item == null)
            {
                _context.Pacient.Add(pacient);
            }
            else
            {
                var existingPacient = _context.Pacient.Find(pacient.Pacient_Id);
                if (existingPacient == null)
                {
                    _context.Pacient.Add(pacient);
                }
                else
                {
                    existingPacient.Full_Name = (_inputControls["Full_Name"] as TextBox).Text;
                    existingPacient.Birth_Date = (_inputControls["Birth_Date"] as DatePicker).SelectedDate ?? default;
                    existingPacient.Passport = (_inputControls["Passport"] as TextBox).Text;
                    existingPacient.Phone_Number = (_inputControls["Phone_Number"] as TextBox).Text;
                    existingPacient.Email = (_inputControls["Email"] as TextBox).Text;
                    existingPacient.Policy = (_inputControls["Policy"] as TextBox).Text;
                    existingPacient.Policy_Type = (_inputControls["Policy_Type"] as ComboBox).SelectedValue?.ToString();
                    existingPacient.Insurance_Company_Id = (int?)((_inputControls["Insurance_Company_Id"] as ComboBox).SelectedValue) ?? 0;
                }
            }
            if (pacient.Insurance_Company_Id != 0)
            {
                var insuranceCompany = _context.Insurance_Company.Find(pacient.Insurance_Company_Id);
                if (insuranceCompany == null)
                {
                    insuranceCompany = new Insurance_Company
                    {
                        Insurance_Company_Id = pacient.Insurance_Company_Id,
                        Title = "Новая страховая компания"
                    };
                    _context.Insurance_Company.Add(insuranceCompany);
                }
            }

            _context.SaveChanges();
        }
        private void SaveOrder()
        {
            var order = _item as Order ?? new Order();
            if (_item == null)
            {
                _context.Order.Add(order);
            }
            else
            {
                var existingOrder = _context.Order.Find(order.Order_Id);
                if (existingOrder == null)
                {
                    _context.Order.Add(order);
                }
                else
                {
                    existingOrder.Create_Date = (_inputControls["Create_Date"] as DatePicker).SelectedDate ?? default;
                    existingOrder.Pacient_Id = (int?)((_inputControls["Pacient_Id"] as ComboBox).SelectedValue) ?? throw new InvalidOperationException("Пациент обязателен");
                    existingOrder.Service_Id = (int?)((_inputControls["Service_Id"] as ComboBox).SelectedValue) ?? throw new InvalidOperationException("Услуга обязательна");
                    existingOrder.Order_Status = (_inputControls["Order_Status"] as ComboBox).SelectedValue?.ToString() == "Выполнен";
                    existingOrder.Complete_Time = (_inputControls["Complete_Time"] as DatePicker).SelectedDate;
                }
            }

            _context.SaveChanges();
        }
        private void SaveService()
        {
            var service = _item as Service ?? new Service();
            if (_item == null)
            {
                _context.Service.Add(service);
            }
            else
            {
                var existingService = _context.Service.Find(service.Service_Id);
                if (existingService == null)
                {
                    _context.Service.Add(service);
                }
                else
                {
                    existingService.Title = (_inputControls["Title"] as TextBox).Text;
                    existingService.Price = decimal.TryParse((_inputControls["Price"] as TextBox).Text, out var price) ? price : 0;
                    existingService.Deadline = int.TryParse((_inputControls["Deadline"] as TextBox).Text, out var deadline) ? deadline : 0;
                    existingService.Deviation = decimal.TryParse((_inputControls["Deviation"] as TextBox).Text, out var deviation) ? deviation / 100 : 0;
                }
            }

            _context.SaveChanges();
        }
        private void SaveUser()
        {
            var user = _item as User ?? new User();
            if (_item == null)
            {
                _context.User.Add(user);
            }
            else
            {
                var existingUser = _context.User.Find(user.User_Id);
                if (existingUser == null)
                {
                    _context.User.Add(user);
                }
                else
                {
                    existingUser.Full_Name = (_inputControls["Full_Name"] as TextBox).Text;
                    existingUser.Login = (_inputControls["Login"] as TextBox).Text;
                    existingUser.Password = (_inputControls["Password"] as TextBox).Text;
                    existingUser.Last_Login_Date = (_inputControls["Last_Login_Date"] as DatePicker).SelectedDate ?? default;
                    existingUser.Service_Id = (int?)((_inputControls["Service_Id"] as ComboBox).SelectedValue) ?? 0;
                    existingUser.Insurance_Company_Id = (int?)((_inputControls["Insurance_Company_Id"] as ComboBox).SelectedValue) ?? 0;
                    existingUser.Account = decimal.TryParse((_inputControls["Account"] as TextBox).Text, out var account) ? account : 0;
                    existingUser.Role_Id = (int?)((_inputControls["Role_Id"] as ComboBox).SelectedValue) ?? throw new InvalidOperationException("Роль обязательна");
                }
            }

            _context.SaveChanges();
        }
    }
}