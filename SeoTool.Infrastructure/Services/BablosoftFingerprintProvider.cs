using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;

namespace SeoTool.Infrastructure.Services
{
    public class BablosoftFingerprintProvider : IFingerprintProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string ApiUrl = "http://127.0.0.1:4999/fingerprints/fingerprint";

        public BablosoftFingerprintProvider(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        public async Task<Fingerprint> GetFingerprintAsync()
        {
            try
            {
                // Создаем запрос с заголовком авторизации
                var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");

                // Выполняем запрос
                var response = await _httpClient.SendAsync(request);

                // Проверяем успешность ответа
                response.EnsureSuccessStatusCode();

                // Читаем ответ как строку
                var jsonContent = await response.Content.ReadAsStringAsync();

                // Парсим JSON для извлечения значения отпечатка
                var jsonDocument = JsonDocument.Parse(jsonContent);
                var root = jsonDocument.RootElement;

                string fingerprintValue;

                // Пытаемся получить значение из разных возможных полей
                if (root.TryGetProperty("fingerprint", out var fingerprintElement))
                {
                    fingerprintValue = fingerprintElement.GetString() ?? string.Empty;
                }
                else if (root.TryGetProperty("value", out var valueElement))
                {
                    fingerprintValue = valueElement.GetString() ?? string.Empty;
                }
                else if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.String)
                {
                    fingerprintValue = dataElement.GetString() ?? string.Empty;
                }
                else
                {
                    throw new InvalidOperationException("Unable to extract fingerprint value from API response");
                }

                if (string.IsNullOrWhiteSpace(fingerprintValue))
                {
                    throw new InvalidOperationException("Received empty fingerprint value from API");
                }

                return new Fingerprint(fingerprintValue);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Bablosoft service is not running or unreachable. Please ensure the service is started.", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse API response as JSON: {ex.Message}", ex);
            }
        }
    }
}