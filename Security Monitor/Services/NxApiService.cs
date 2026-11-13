using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Security_Monitor.Services
{
    public class NxApiService
    {
        // Единственный экземпляр на всё приложение: токен переиспользуется,
        // на сервере NX открывается одна сессия вместо сессии на каждое событие.
        private static readonly Lazy<NxApiService> _shared = new(() => new NxApiService());
        public static NxApiService Shared => _shared.Value;

        private readonly NxConfig _config;

        private string? _token;
        private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
        private readonly SemaphoreSlim _loginLock = new(1, 1);
        private readonly HttpClient _client;

        private NxApiService()
        {
            _config = AppConfig.Current.Nx;

            if (string.IsNullOrWhiteSpace(_config.Host) ||
                string.IsNullOrWhiteSpace(_config.Username) ||
                string.IsNullOrWhiteSpace(_config.Password))
                throw new InvalidOperationException(
                    "В appsettings.json не заполнены Nx:Host / Nx:Username / Nx:Password.");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            _client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<BitmapImage?> GetImageAsync(DateTime date, string cameraId)
        {
            // До двух попыток: при 401 (протухший токен) перелогиниваемся и повторяем.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string token;
                try
                {
                    token = await EnsureTokenAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Ошибка логина в NX: {ex.Message}");
                    return null;
                }

                var (image, unauthorized) = await TryGetImageAsync(date, cameraId, token);

                if (image != null)
                    return image;

                if (!unauthorized)
                    return null; // ошибка не связана с авторизацией — повторять бессмысленно

                InvalidateToken(); // 401/403 → токен недействителен, следующая попытка перелогинится
            }

            return null;
        }

        private async Task<(BitmapImage? image, bool unauthorized)> TryGetImageAsync(
            DateTime date, string cameraId, string token)
        {
            long ToMicroseconds(DateTime dt) =>
                (long)(dt.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds * 1000;

            long[] tsList =
            {
                ToMicroseconds(date - TimeSpan.FromMilliseconds(100)),
                ToMicroseconds(date),
                ToMicroseconds(date - TimeSpan.FromMilliseconds(700))
            };

            foreach (var tsUs in tsList)
            {
                var url =
                    $"https://{_config.Host}:{_config.Port}/rest/v2/devices/{cameraId}/image" +
                    $"?resolution=1920x1080&timestampUs={tsUs}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.ParseAdd("image/*");

                try
                {
                    using var resp = await _client.SendAsync(request);

                    if (resp.StatusCode == HttpStatusCode.Unauthorized ||
                        resp.StatusCode == HttpStatusCode.Forbidden)
                        return (null, true);

                    if (!resp.IsSuccessStatusCode)
                        continue;

                    if (resp.Content.Headers.ContentType?.MediaType?.StartsWith("image/") != true)
                        continue;

                    var imgBytes = await resp.Content.ReadAsByteArrayAsync();
                    return (BytesToBitmapImage(imgBytes), false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Ошибка при загрузке кадра NX: {ex.Message}");
                }
            }

            return (null, false);
        }

        private async Task<string> EnsureTokenAsync()
        {
            if (_token != null && DateTimeOffset.UtcNow < _tokenExpiresAt)
                return _token;

            // Защита от параллельных логинов при одновременных событиях.
            await _loginLock.WaitAsync();
            try
            {
                if (_token != null && DateTimeOffset.UtcNow < _tokenExpiresAt)
                    return _token;

                await LoginAsync();
                return _token!;
            }
            finally
            {
                _loginLock.Release();
            }
        }

        private void InvalidateToken()
        {
            _token = null;
            _tokenExpiresAt = DateTimeOffset.MinValue;
        }

        private async Task LoginAsync()
        {
            var url = $"https://{_config.Host}:{_config.Port}/rest/v2/login/sessions";

            var payload = new
            {
                username = _config.Username,
                password = _config.Password
            };

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var resp = await _client.PostAsync(url, content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"HTTP {resp.StatusCode}: {body}");

            var parsed = JsonSerializer.Deserialize<LoginResponse>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (parsed?.Token == null)
                throw new Exception("NX не вернул токен. Ответ: " + body);

            _token = parsed.Token;

            // Запас 60 секунд, чтобы не упереться в истечение токена на середине запроса.
            var expiresInS = parsed.ExpiresInS ?? 3600;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(expiresInS - 60, 30));
        }

        public async Task LogoutAsync()
        {
            var token = Interlocked.Exchange(ref _token, null);
            _tokenExpiresAt = DateTimeOffset.MinValue;

            if (token == null)
                return;

            try
            {
                var url = $"https://{_config.Host}:{_config.Port}/rest/v2/login/sessions/{token}";
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
                using var resp = await _client.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Ошибка при закрытии сессии NX: {ex.Message}");
            }
        }

        /// <summary>Закрывает сессию NX, если сервис вообще использовался. Вызывать при выходе из приложения.</summary>
        public static async Task ShutdownAsync()
        {
            if (_shared.IsValueCreated)
                await _shared.Value.LogoutAsync();
        }

        private static BitmapImage BytesToBitmapImage(byte[] bytes)
        {
            var img = new BitmapImage();
            using var ms = new MemoryStream(bytes);
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            return img;
        }

        private class LoginResponse
        {
            public string? Token { get; set; }
            public string? Username { get; set; }
            public int? AgeS { get; set; }
            public int? ExpiresInS { get; set; }
        }
    }
}
