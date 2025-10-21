using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;
using DomainProxy = SeoTool.Domain.Entities.Proxy;

namespace SeoTool.Infrastructure.Services
{
    public class PlaywrightBrowserWorker : IBrowserWorker
    {
        public async Task PerformSearchTaskAsync(SearchTask task, DomainProxy proxy, IEnumerable<CookieInfo> cookies, Fingerprint fingerprint)
        {
            IPlaywright? playwright = null;
            IBrowser? browser = null;
            IBrowserContext? context = null;
            IPage? page = null;

            try
            {
                // Инициализируем Playwright
                playwright = await Playwright.CreateAsync();

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
                browser = await playwright.Chromium.LaunchAsync(launchOptions);

                // Создаем новый BrowserContext
                context = await browser.NewContextAsync();

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

                // Применяем отпечаток к странице через AddInitScriptAsync
                if (!string.IsNullOrEmpty(fingerprint.Value))
                {
                    await page.AddInitScriptAsync(fingerprint.Value);
                }

                // Переходим на https://www.bing.com
                await page.GotoAsync("https://www.bing.com");

                // Находим поле ввода по селектору [name="q"] и вводим keyword
                var searchBox = page.Locator("[name=\"q\"]");
                await searchBox.FillAsync(task.Keyword);

                // Нажимаем Enter
                await page.Keyboard.PressAsync("Enter");

                // Ждем загрузки результатов
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Находим ссылку, которая содержит task.Domain, и берем первую
                var targetLink = page.Locator($"a[href*='{task.Domain}']").First;

                // Кликаем по ссылке
                await targetLink.ClickAsync();

                // Ждем случайное время от 30 до 60 секунд
                var randomDelay = Random.Shared.Next(30, 61);
                await Task.Delay(randomDelay * 1000);
            }
            catch (Exception ex)
            {
                // Логируем ошибку (можно заменить на ваш логгер)
                Console.WriteLine($"Error in PerformSearchTaskAsync: {ex.Message}");
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