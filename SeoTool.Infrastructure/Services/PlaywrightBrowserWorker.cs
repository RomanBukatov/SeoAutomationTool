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

        public async Task PerformSearchTaskAsync(SearchTask task, DomainProxy proxy, IEnumerable<CookieInfo> cookies, Fingerprint fingerprint, CancellationToken cancellationToken = default)
        {
            IPlaywright? playwright = null;
            IBrowser? browser = null;
            IBrowserContext? context = null;
            IPage? page = null;

            playwright = await Playwright.CreateAsync();

            try
            {
                _logger.LogInformation("Инициализация Playwright...");
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
                    Proxy = playwrightProxy,
                    Headless = false
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
                    foreach (var cookie in playwrightCookies)
                    {
                        try
                        {
                            await context.AddCookiesAsync(new[] { cookie });
                        }
                        catch (PlaywrightException ex)
                        {
                            _logger.LogWarning(ex, "Не удалось добавить одну из кук. Возможно, она некорректна. Пропускаю. Имя: {CookieName}", cookie.Name);
                            // Просто игнорируем ошибку и идем к следующей куке
                        }
                    }
                }

                // Создаем новую страницу
                page = await context.NewPageAsync();
                await page.AddInitScriptAsync(fingerprint.Value);
                cancellationToken.ThrowIfCancellationRequested();

                // Формируем поисковый URL вручную
                var searchQuery = System.Net.WebUtility.UrlEncode(task.Keyword);
                var searchUrl = $"https://www.bing.com/search?q={searchQuery}";

                _logger.LogInformation("Перехожу напрямую по поисковому URL: {SearchUrl}", searchUrl);

                // Переходим напрямую на страницу с результатами поиска
                await page.GotoAsync(searchUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                cancellationToken.ThrowIfCancellationRequested();

                // Принимаем куки Bing, если есть кнопка
                try
                {
                    var acceptButton = page.Locator("button:has-text('Accept'), button:has-text('Принять'), button[id*='accept'], button[class*='accept']").First;
                    await acceptButton.ClickAsync();
                }
                catch
                {
                }

                // Извлекаем чистое имя домена, например, "youtube" из "youtube.com"
                var domainNameOnly = task.Domain.Split('.')[0];

                // Ищем ссылку <a>, внутри которой есть текст с чистым именем домена (без учета регистра)
                var link = page.Locator($"a:has-text('{domainNameOnly}')").First;

                // Сначала скроллим до элемента, чтобы он стал видимым
                await link.ScrollIntoViewIfNeededAsync();

                // Ждем, пока он точно будет готов к клику
                await link.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                // Кликаем
                await link.ClickAsync();
                cancellationToken.ThrowIfCancellationRequested();

                // Ждем на странице случайное время от 30 до 60 секунд
                var randomDelay = Random.Shared.Next(30, 61);
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

                if (playwright != null)
                {
                    playwright.Dispose();
                }
            }
        }
    }
}