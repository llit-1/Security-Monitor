using Microsoft.EntityFrameworkCore;
using Security_Monitor.Models;
using Security_Monitor.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Security_Monitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _surname;
        private string _name;
        private string _patronymic;
        private string _department;
        private string _workshop;
        private string _jobTitle;
        private string _reader;
        private string _time;
        private BitmapImage _photo; 
        private int _age;

        public string Surname { get => _surname; set { _surname = value; OnPropertyChanged(); } }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string Patronymic { get => _patronymic; set { _patronymic = value; OnPropertyChanged(); } }
        public string Department { get => _department; set { _department = value; OnPropertyChanged(); } }
        public string Workshop { get => _workshop; set { _workshop = value; OnPropertyChanged(); } }
        public string JobTitle { get => _jobTitle; set { _jobTitle = value; OnPropertyChanged(); } }
        public string Reader { get => _reader; set { _reader = value; OnPropertyChanged(); } }
        public string Time { get => _time; set { _time = value; OnPropertyChanged(); } }
        public BitmapImage Photo { get => _photo; set { _photo = value; OnPropertyChanged(); } }
        public BitmapImage PhotoNX { get => _photo; set { _photo = value; OnPropertyChanged(); } }
        public int Age { get => _age; set { _age = value; OnPropertyChanged(); } }

        public async void LoadPerson(int id, string reader, string time)
        {
            using var db = new RKNETContext();
            var person = await db.FactoryPerson.FirstOrDefaultAsync(p => p.Id == id);
            if (person == null) return;

            Surname = person.Surname;
            Name = person.Name;
            Patronymic = person.Patronymic ?? "";
            Age = DateTime.Now.Year - person.Birthdate.Year -
                  (DateTime.Now < person.Birthdate.AddYears(DateTime.Now.Year - person.Birthdate.Year) ? 1 : 0);

            Department = (await db.FactoryDepartment.FindAsync(person.FactoryDepartment))?.Name ?? "-";
            Workshop = (await db.FactoryWorkshop.FindAsync(person.FactoryWorkshop))?.Name ?? "-";
            JobTitle = (await db.FactoryJobTitle.FindAsync(person.FactoryJobTitle))?.Name ?? "-";
            Reader = reader;
            Time= time;
            // ✅ Обработка base64 фото
            Photo = Global.ConvertBase64ToImage(person.Photo); 
            try
            {
                DateTime dt = DateTime.Today.Add(TimeSpan.Parse(time)).AddSeconds(-0.5);
                var nx = new NxApiService();
                PhotoNX = await nx.GetImageAsync(dt, "2da4a772-4c24-ed11-a888-9bd9c6760d9a");
            }
            catch (Exception)
            {

            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}