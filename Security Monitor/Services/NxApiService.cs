using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Security_Monitor.Services
{
    public class NxApiService
    {
        private readonly string _host = "10.140.0.26";
        private readonly string _port = "7001";
        private readonly string _username = "videoproh";
        private readonly string _password = "238CondomNX169!";
        private string? _token;
        private readonly HttpClient _client;

        public NxApiService()
        {
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
            if (_token == null)
                _token = await LoginAndGetTokenAsync();

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
                    $"https://{_host}:{_port}/rest/v2/devices/{cameraId}/image" +
                    $"?resolution=1920x1080&timestampUs={tsUs}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _token);
                request.Headers.Accept.ParseAdd("image/*");

                try
                {
                    using var resp = await _client.SendAsync(request);
                    if (!resp.IsSuccessStatusCode)
                        continue;

                    if (resp.Content.Headers.ContentType?.MediaType?.StartsWith("image/") != true)
                        continue;

                    var imgBytes = await resp.Content.ReadAsByteArrayAsync();
                    return BytesToBitmapImage(imgBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Ошибка при загрузке кадра NX: {ex.Message}");
                }
            }

            return null;
        }

        private async Task<string> LoginAndGetTokenAsync()
        {
            var url = $"https://{_host}:{_port}/rest/v2/login/sessions";

            var payload = new
            {
                username = _username,
                password = _password
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

            return parsed.Token;
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