using Security_Monitor.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Security_Monitor.ViewModels
{
    public class EmployeeEditViewModel : INotifyPropertyChanged
    {
        private string? _selectedPassport;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Список паспортов
        public ObservableCollection<string> Passports { get; set; } = new();

        // Выбранный паспорт
        public string? SelectedPassport
        {
            get => _selectedPassport;
            set
            {
                _selectedPassport = value;                
                OnPropertyChanged();
                LoadPersonData();
            }
        }

        // Поля сотрудника
        private string _surname = "";
        public string Surname { get => _surname; set { _surname = value; OnPropertyChanged(); } }

        private string _name = "";
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string _patronymic = "";
        public string Patronymic { get => _patronymic; set { _patronymic = value; OnPropertyChanged(); } }

        private string _birthdate = "";
        public string Birthdate { get => _birthdate; set { _birthdate = value; OnPropertyChanged(); } }

        private string _department = "";
        public string Department { get => _department; set { _department = value; OnPropertyChanged(); } }

        private string _workshop = "";
        public string Workshop { get => _workshop; set { _workshop = value; OnPropertyChanged(); } }

        private string _jobTitle = "";
        public string JobTitle { get => _jobTitle; set { _jobTitle = value; OnPropertyChanged(); } }

        private string _passCardNumber = "";
        public string PassCardNumber { get => _passCardNumber; set { _passCardNumber = value; OnPropertyChanged(); } }

        private System.Windows.Media.ImageSource? _photo;
        public System.Windows.Media.ImageSource? Photo { get => _photo; set { _photo = value; OnPropertyChanged(); } }
        public EmployeeEditViewModel()
        {
            LoadPassports();
        }

        // Загружаем все паспорта
        private void LoadPassports()
        {
            using var db = new RKNETContext();
            var passports = db.FactoryPerson.Where(c => c.Fake == null)
                .OrderBy(x => x.Passport)
                .Select(x => x.Passport)
                .ToList();

            Passports.Clear();
            foreach (var p in passports)
                Passports.Add(p);
        }

        // Загружаем данные выбранного сотрудника
        private void LoadPersonData()
        {
            if (string.IsNullOrWhiteSpace(SelectedPassport))
            {
                ClearPersonData();
                return;
            }

            using var db = new RKNETContext();

            var person = db.FactoryPerson
                .FirstOrDefault(x => x.Passport == SelectedPassport);

            if (person == null)
            {
                ClearPersonData();
                return;
            }

            Surname = person.Surname;
            Name = person.Name;
            Patronymic = person.Patronymic ?? "";
            Birthdate = person.Birthdate.ToString("dd.MM.yyyy");
            PassCardNumber = person.PassCardNumber ?? "";
            Department = db.FactoryDepartment
                .FirstOrDefault(x => x.Id == person.FactoryDepartment)?.Name ?? "";

            Workshop = db.FactoryWorkshop
                .FirstOrDefault(x => x.Id == person.FactoryWorkshop)?.Name ?? "";

            JobTitle = db.FactoryJobTitle
                .FirstOrDefault(x => x.Id == person.FactoryJobTitle)?.Name ?? "";
            Photo = Global.ConvertBase64ToImage(person.Photo);
        }

        private void ClearPersonData()
        {
            Surname = "";
            Name = "";
            Patronymic = "";
            Birthdate = "";
            Department = "";
            Workshop = "";
            JobTitle = "";
            PassCardNumber = "";
            Photo = null;
        }

        private void OnPropertyChanged([CallerMemberName] string? prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}