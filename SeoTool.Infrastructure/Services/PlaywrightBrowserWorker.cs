using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;
using DomainProxy = SeoTool.Domain.Entities.Proxy;

namespace SeoTool.Infrastructure.Services
{
    public class PlaywrightBrowserWorker : IBrowserWorker
    {
        private readonly ILogger<PlaywrightBrowserWorker> _logger;

        public PlaywrightBrowserWorker(ILogger<PlaywrightBrowserWorker> logger)
        {
            _logger = logger;
        }

        public async Task PerformSearchTaskAsync(SearchTask task, DomainProxy proxy, IEnumerable<CookieInfo> cookies, CancellationToken cancellationToken = default)
        {
            IPlaywright? playwright = null;
            IBrowser? browser = null;
            IBrowserContext? context = null;
            IPage? page = null;

            try
            {
                Console.WriteLine(">>> PlaywrightBrowserWorker.PerformSearchTaskAsync START");
                // Инициализируем Playwright
                Console.WriteLine(">>> PlaywrightBrowserWorker: Creating Playwright instance...");
                _logger.LogInformation("Инициализация Playwright...");
                playwright = await Playwright.CreateAsync();
                cancellationToken.ThrowIfCancellationRequested();

                // Создаем объект BrowserTypeLaunchOptions и устанавливаем данные прокси
                var playwrightProxy = new Microsoft.Playwright.Proxy
                {
                    Server = $"{proxy.Host}:{proxy.Port}"
                };

                // Добавляем аутентификацию только если она указана
                if (!string.IsNullOrEmpty(proxy.Username) && !string.IsNullOrEmpty(proxy.Password))
                {
                    playwrightProxy.Username = proxy.Username;
                    playwrightProxy.Password = proxy.Password;
                }

                var launchOptions = new BrowserTypeLaunchOptions
                {
                    Proxy = playwrightProxy
                };

                // Запускаем браузер с опциями прокси
                _logger.LogInformation("Запускаю браузер Chromium...");
                browser = await playwright.Chromium.LaunchAsync(launchOptions);
                cancellationToken.ThrowIfCancellationRequested();

                // Создаем новый BrowserContext
                context = await browser.NewContextAsync();
                cancellationToken.ThrowIfCancellationRequested();

                // Преобразуем IEnumerable<CookieInfo> в формат Playwright (Cookie[])
                var playwrightCookies = cookies.Select(cookie => new Cookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain,
                    Path = cookie.Path
                }).ToArray();

                // Добавляем куки в контекст
                if (playwrightCookies.Any())
                {
                    await context.AddCookiesAsync(playwrightCookies);
                }

                // Создаем новую страницу
                page = await context.NewPageAsync();
                cancellationToken.ThrowIfCancellationRequested();

                // Временное изменение: переходим на whoer.net для проверки прокси
                _logger.LogInformation("Перехожу на страницу {Url}...", "https://whoer.net");
                await page.GotoAsync("https://whoer.net");
                cancellationToken.ThrowIfCancellationRequested();

                // Ждем загрузки страницы
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                cancellationToken.ThrowIfCancellationRequested();

                // Делаем скриншот страницы
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = "whoer_screenshot.png" });

                // Ждем случайное время от 5 до 10 секунд (для теста прокси)
                var randomDelay = Random.Shared.Next(5, 11);
                await Task.Delay(randomDelay * 1000, cancellationToken);

            }
            catch (Exception ex)
            {
                // Логируем ошибку
                _logger.LogError(ex, "Произошла ошибка в PlaywrightBrowserWorker.");
                throw;
            }
            finally
            {
                // Гарантированно закрываем браузер и очищаем ресурсы
                if (page != null)
                {
                    await page.CloseAsync();
                }

                if (context != null)
                {
                    await context.CloseAsync();
                }

                if (browser != null)
                {
                    await browser.CloseAsync();
                }

                playwright?.Dispose();
            }
        }
    }
}