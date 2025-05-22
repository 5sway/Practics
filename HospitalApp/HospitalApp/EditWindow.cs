using System;
using System.Collections.Generic;
using System.Linq;
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

        public EditWindow(object item, string entityType, HospitalBaseEntities context)
        {
            _item = item;
            _entityType = entityType;
            _context = context;
            _inputControls = new Dictionary<string, Control>();

            Title = item == null ? $"Добавить {_entityType}" : $"Редактировать {_entityType}";
            Width = 400;
            Height = 400;
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

        private void CreatePatientFields(StackPanel panel)
        {
            var pacient = _item as Pacient ?? new Pacient();
            AddTextBox(panel, "Full_Name", "ФИО*", pacient.Full_Name, true);
            AddTextBox(panel, "Birth_Date", "Дата рождения (дд.мм.гггг)", pacient.Birth_Date != default ? pacient.Birth_Date.ToString("dd.MM.yyyy") : "");
            AddTextBox(panel, "Passport", "Паспорт", pacient.Passport);
            AddTextBox(panel, "Phone_Number", "Телефон", pacient.Phone_Number);
            AddTextBox(panel, "Email", "Email", pacient.Email);
            AddTextBox(panel, "Policy", "Полис", pacient.Policy);
            AddTextBox(panel, "Policy_Type", "Тип полиса", pacient.Policy_Type);
            AddComboBox(panel, "Insurance_Company_Id", "Страховая компания", _context.Insurance_Company.ToList(),
                        "Title", "Insurance_Company_Id", pacient.Insurance_Company_Id);
        }

        private void CreateOrderFields(StackPanel panel)
        {
            var order = _item as Order ?? new Order();
            AddTextBox(panel, "Create_Date", "Дата создания (дд.мм.гггг)", order.Create_Date != default ? order.Create_Date.ToString("dd.MM.yyyy") : "");
            AddComboBox(panel, "Pacient_Id", "Пациент*", _context.Pacient.ToList(),
                        "Full_Name", "Pacient_Id", order.Pacient_Id, true);
            AddComboBox(panel, "Service_Id", "Услуга*", _context.Service.ToList(),
                        "Title", "Service_Id", order.Service_Id, true);
            AddComboBox(panel, "Order_Status", "Статус", new List<string> { "В работе", "Выполнен" },
                        null, null, (bool)order.Order_Status ? "Выполнен" : "В работе");
            AddTextBox(panel, "Complete_Time", "Время завершения (дд.мм.гггг чч:мм)",
                       order.Complete_Time.HasValue ? order.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "");
        }

        private void CreateServiceFields(StackPanel panel)
        {
            var service = _item as Service ?? new Service();
            AddTextBox(panel, "Title", "Название*", service.Title, true);
            AddTextBox(panel, "Price", "Цена", service.Price != 0 ? service.Price.ToString() : "");
            AddTextBox(panel, "Deadline", "Срок (дни)", service.Deadline != 0 ? service.Deadline.ToString() : "");
            AddTextBox(panel, "Deviation", "Допуск (%)", service.Deviation != 0 ? service.Deviation.ToString("N2") : "");
        }

        private void CreateUserFields(StackPanel panel)
        {
            var user = _item as User ?? new User();
            AddTextBox(panel, "Full_Name", "ФИО*", user.Full_Name, true);
            AddTextBox(panel, "Login", "Логин*", user.Login, true);
            AddTextBox(panel, "Password", "Пароль*", user.Password, true);
            AddTextBox(panel, "Last_Login_Date", "Последний вход (дд.мм.гггг чч:мм)",
                       user.Last_Login_Date != default ? user.Last_Login_Date.ToString("dd.MM.yyyy HH:mm") : "");
            AddComboBox(panel, "Service_Id", "Услуга", _context.Service.ToList(),
                        "Title", "Service_Id", user.Service_Id);
            AddComboBox(panel, "Insurance_Company_Id", "Страховая компания", _context.Insurance_Company.ToList(),
                        "Title", "Insurance_Company_Id", user.Insurance_Company_Id);
            AddTextBox(panel, "Account", "Счет", user.Account != 0 ? user.Account.ToString() : "");
            AddComboBox(panel, "Role_Id", "Роль", _context.Role.ToList(),
                        "Name", "Role_Id", user.Role_Id, true);
        }

        private void AddTextBox(StackPanel panel, string property, string label, string value, bool isRequired = false)
        {
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 5) });
            var textBox = new TextBox { Text = value ?? "", Margin = new Thickness(0, 0, 0, 10) };
            if (isRequired)
                textBox.Tag = "Required";
            _inputControls[property] = textBox;
            panel.Children.Add(textBox);
        }

        private void AddComboBox<T>(StackPanel panel, string property, string label, List<T> items,
                                    string displayMember, string valueMember, object selectedValue, bool isRequired = false)
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

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}\nВнутренняя ошибка: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            foreach (var control in _inputControls)
            {
                if (control.Value.Tag?.ToString() == "Required")
                {
                    if (control.Value is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        MessageBox.Show($"Поле {control.Key} обязательно для заполнения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    if (control.Value is ComboBox comboBox && (comboBox.SelectedValue == null || (comboBox.SelectedValue is int value && value == 0)))
                    {
                        MessageBox.Show($"Поле {control.Key} обязательно для заполнения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
            return true;
        }

        private void SavePatient()
        {
            var pacient = _item as Pacient ?? new Pacient();
            pacient.Full_Name = (_inputControls["Full_Name"] as TextBox).Text;
            pacient.Birth_Date = DateTime.TryParse((_inputControls["Birth_Date"] as TextBox).Text, out var birthDate) ? birthDate : default;
            pacient.Passport = (_inputControls["Passport"] as TextBox).Text;
            pacient.Phone_Number = (_inputControls["Phone_Number"] as TextBox).Text;
            pacient.Email = (_inputControls["Email"] as TextBox).Text;
            pacient.Policy = (_inputControls["Policy"] as TextBox).Text;
            pacient.Policy_Type = (_inputControls["Policy_Type"] as TextBox).Text;
            pacient.Insurance_Company_Id = (int)((_inputControls["Insurance_Company_Id"] as ComboBox).SelectedValue as int?);

            if (_item == null)
                _context.Pacient.Add(pacient);
        }

        private void SaveOrder()
        {
            var order = _item as Order ?? new Order();
            order.Create_Date = DateTime.TryParse((_inputControls["Create_Date"] as TextBox).Text, out var createDate) ? createDate : default;
            order.Pacient_Id = (_inputControls["Pacient_Id"] as ComboBox).SelectedValue as int? ?? throw new InvalidOperationException("Пациент обязателен");
            order.Service_Id = (_inputControls["Service_Id"] as ComboBox).SelectedValue as int? ?? throw new InvalidOperationException("Услуга обязательна");
            order.Order_Status = (_inputControls["Order_Status"] as ComboBox).SelectedValue?.ToString() == "Выполнен";
            order.Complete_Time = DateTime.TryParse((_inputControls["Complete_Time"] as TextBox).Text, out var completeTime) ? completeTime : (DateTime?)null;

            if (_item == null)
                _context.Order.Add(order);
        }

        private void SaveService()
        {
            var service = _item as Service ?? new Service();
            service.Title = (_inputControls["Title"] as TextBox).Text;
            service.Price = decimal.TryParse((_inputControls["Price"] as TextBox).Text, out var price) ? price : 0;
            service.Deadline = int.TryParse((_inputControls["Deadline"] as TextBox).Text, out var deadline) ? deadline : 0;
            service.Deviation = decimal.TryParse((_inputControls["Deviation"] as TextBox).Text, out var deviation) ? deviation : 0;

            if (_item == null)
                _context.Service.Add(service);
        }

        private void SaveUser()
        {
            var user = _item as User ?? new User();
            user.Full_Name = (_inputControls["Full_Name"] as TextBox).Text;
            user.Login = (_inputControls["Login"] as TextBox).Text;
            user.Password = (_inputControls["Password"] as TextBox).Text;
            user.Last_Login_Date = DateTime.TryParse((_inputControls["Last_Login_Date"] as TextBox).Text, out var lastLogin) ? lastLogin : default;
            user.Service_Id = (int)((_inputControls["Service_Id"] as ComboBox).SelectedValue as int?);
            user.Insurance_Company_Id = (_inputControls["Insurance_Company_Id"] as ComboBox).SelectedValue as int?;
            user.Account = decimal.TryParse((_inputControls["Account"] as TextBox).Text, out var account) ? account : 0;
            user.Role_Id = (_inputControls["Role_Id"] as ComboBox).SelectedValue as int? ?? throw new InvalidOperationException("Роль обязательна");

            if (_item == null)
                _context.User.Add(user);
        }
    }
}