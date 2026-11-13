using System.Configuration;
using System.Data;
using System.Windows;

namespace Security_Monitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            // Закрываем сессию на сервере NX, чтобы она не висела до таймаута.
            try
            {
                Security_Monitor.Services.NxApiService
                    .ShutdownAsync()
                    .Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // При выходе ошибки logout не критичны — сессия истечёт по таймауту.
            }

            base.OnExit(e);
        }
    }

}
