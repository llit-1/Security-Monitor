using System;
using System.IO;
using System.Text.Json;

namespace Security_Monitor.Services
{
    /// <summary>
    /// Конфигурация приложения, загружается из appsettings.json рядом с exe.
    /// Файл с реальными значениями не коммитится (см. .gitignore),
    /// в репозитории лежит шаблон appsettings.example.json.
    /// </summary>
    public class AppConfig
    {
        private static readonly Lazy<AppConfig> _current = new(Load);
        public static AppConfig Current => _current.Value;

        public NxConfig Nx { get; set; } = new NxConfig();

        private static AppConfig Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Файл конфигурации appsettings.json не найден рядом с приложением. " +
                    "Скопируйте appsettings.example.json в appsettings.json и заполните значения.",
                    path);

            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<AppConfig>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                }) ?? new AppConfig();
        }
    }

    public class NxConfig
    {
        public string Host { get; set; } = "";
        public string Port { get; set; } = "7001";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string CameraId { get; set; } = "";
    }
}
