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

        public async Task PerformSearchTaskAsync(SearchTask task, DomainProxy proxy, IEnumerable<CookieInfo> cookies, Fingerprint fingerprint, Action<string> logAction, CancellationToken cancellationToken = default)
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
                _logger.LogInformation("Запускаю браузер Chrome...");
                logAction("Запускаю браузер Chrome...");
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
                logAction($"Перехожу по поисковому URL: {searchUrl}");

                // Переходим напрямую на страницу с результатами поиска
                await page.GotoAsync(searchUrl, new() { WaitUntil = WaitUntilState.Load, Timeout = 60000 });
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

                // Пытаемся найти и нажать кнопку принятия куки
                try
                {
                    var acceptButton = page.Locator("#bnp_btn_accept").First;
                    if (await acceptButton.IsVisibleAsync())
                    {
                        await acceptButton.ClickAsync();
                        _logger.LogInformation("Баннер с куки принят.");
                    }
                }
                catch (Exception) { /* Игнорируем, если кнопки нет */ }

                _logger.LogInformation("Начинаю финальный поиск ссылки на домен: {Domain}", task.Domain);

                // Извлекаем чистое имя домена без "www." и протокола
                var cleanDomain = task.Domain.Replace("https://", "").Replace("http://", "").Replace("www.", "");

                // Ищем ссылку <a>, у которой href содержит чистое имя домена (без учета регистра)
                // Это наш основной, самый надежный локатор
                var linkLocator = page.Locator($"a[href*='{cleanDomain}' i]");

                try
                {
                    // Пытаемся кликнуть по первой найденной ссылке с таймаутом
                    await linkLocator.First.ClickAsync(new() { Timeout = 30000 });
                    _logger.LogInformation("Клик по ссылке с href*='{Domain}' успешно выполнен!", cleanDomain);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Не удалось найти ссылку по href. Пробую запасной вариант: поиск по видимому тексту.");

                    // ЗАПАСНОЙ ВАРИАНТ: Если по href не нашли, ищем по тексту
                    var textLocator = page.Locator($"a:has-text('{cleanDomain}')");
                    try
                    {
                        await textLocator.First.ClickAsync(new() { Timeout = 15000 });
                        _logger.LogInformation("Клик по ссылке с :has-text='{Domain}' успешно выполнен!", cleanDomain);
                    }
                    catch (Exception finalEx)
                    {
                        await page.ScreenshotAsync(new() { Path = "final_fail_screenshot.png", FullPage = true });
                        throw new Exception($"ФИНАЛЬНЫЙ ПРОВАЛ: Не удалось найти ссылку ни по href, ни по тексту. Скриншот сохранен.", finalEx);
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();

                // Ждем на странице случайное время от 30 до 60 секунд
                var randomDelay = Random.Shared.Next(30, 61);
                await Task.Delay(randomDelay * 1000, cancellationToken);
                logAction("Ожидание на странице завершено. Сессия успешна.");

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