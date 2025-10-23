using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;
using SeoTool.Infrastructure.Services;

namespace SeoTool.Core
{
    public class AutomationService
    {
        private readonly IProxyProvider _proxyProvider;
        private readonly ICookieProvider _cookieProvider;
        private readonly IBrowserWorker _browserWorker;
        private readonly HttpClient _httpClient;

        public AutomationService(
            IProxyProvider proxyProvider,
            ICookieProvider cookieProvider,
            IBrowserWorker browserWorker,
            HttpClient httpClient)
        {
            _proxyProvider = proxyProvider ?? throw new ArgumentNullException(nameof(proxyProvider));
            _cookieProvider = cookieProvider ?? throw new ArgumentNullException(nameof(cookieProvider));
            _browserWorker = browserWorker ?? throw new ArgumentNullException(nameof(browserWorker));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task StartAutomationAsync(SearchTask task, string proxyPath, string cookiesPath, string usedCookiesFolderPath, string fingerprintApiKey, CancellationToken cancellationToken = default, params string[] additionalPaths)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (string.IsNullOrWhiteSpace(proxyPath))
                throw new ArgumentNullException(nameof(proxyPath));

            if (string.IsNullOrWhiteSpace(cookiesPath))
                throw new ArgumentNullException(nameof(cookiesPath));

            if (string.IsNullOrWhiteSpace(fingerprintApiKey))
                throw new ArgumentNullException(nameof(fingerprintApiKey));

            try
            {
                Console.WriteLine($"Starting automation for task: {task.Domain} - {task.Keyword}");

                // Поочередно вызываем все провайдеры для получения данных
                Console.WriteLine("Getting proxy...");
                var proxy = await _proxyProvider.GetNextProxyAsync(proxyPath);

                Console.WriteLine("Getting cookies...");
                var cookies = await _cookieProvider.GetNextCookiesAsync(cookiesPath, usedCookiesFolderPath);

                Console.WriteLine("Getting fingerprint...");
                // Create fingerprint provider with the provided API key
                var fingerprintProvider = new FingerprintSwitcherProvider(_httpClient, fingerprintApiKey);
                var fingerprint = await fingerprintProvider.GetFingerprintAsync();

                // Вызываем браузерный воркер с полученными данными
                Console.WriteLine("Executing search task...");
                await _browserWorker.PerformSearchTaskAsync(task, proxy, cookies, fingerprint, cancellationToken);

                Console.WriteLine($"Automation completed successfully for task: {task.Domain} - {task.Keyword}");
            }
            catch (Exception ex)
            {
                // Логируем ошибки в консоль
                Console.WriteLine($"Error during automation for task {task.Domain} - {task.Keyword}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Пробрасываем исключение дальше для возможности обработки на более высоком уровне
            }
        }
    }
}